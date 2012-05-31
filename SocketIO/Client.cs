using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SocketIOClient.Eventing;
using SocketIOClient.Messages;
using WebSocket4Net;

namespace SocketIOClient
{
	/// <summary>
	/// Class to emulate socket.io javascript client capabilities for .net classes
	/// </summary>
	/// <exception cref="ArgumentException" for wss or https urls />
	public class Client : IDisposable, SocketIOClient.IClient
	{
		private Timer socketHeartBeatTimer; // HeartBeat timer 
		private Task dequeuOutBoundMsgTask;
		private BlockingCollection<string> outboundQueue;
		private int retryConnectionCount = 0;
		private readonly static object padLock = new object(); // alow one connection attempt at a time

		protected Uri uri;
		protected WebSocket wsClient;
		protected RegistrationManager registrationManager;  // allow registration of dynamic events (event names) for client actions
		protected WebSocketVersion socketVersion = WebSocketVersion.Rfc6455;

		// Events
		/// <summary>
		/// Opened event comes from the underlying websocket client connection being opened.  This is not the same as socket.io returning the 'connect' event
		/// </summary>
		public event EventHandler Opened;
		public event EventHandler<MessageEventArgs> Message;
		public event EventHandler ConnectionRetryAttempt;
		public event EventHandler HeartBeatTimerEvent;
		/// <summary>
		/// <para>The underlying websocket connection has closed (unexpectedly)</para>
		/// <para>The Socket.IO service may have closed the connection due to a heartbeat timeout, or the connection was just broken</para>
		/// <para>Call the client.Connect() method to re-establish the connection</para>
		/// </summary>
		public event EventHandler SocketConnectionClosed;
		public event EventHandler<ErrorEventArgs> Error;

		public ManualResetEvent MessageQueueEmptyEvent = new ManualResetEvent(true);
		public ManualResetEvent ConnectionOpenEvent = new ManualResetEvent(false);

		private int retryConnectionAttempts = 3;
		/// <summary>
		/// Number of reconnection attempts before raising SocketConnectionClosed event - (default = 3)
		/// </summary>
		public int RetryConnectionAttempts
		{
			get { return this.retryConnectionAttempts; }
			set { this.retryConnectionAttempts = value; }
		}

		/// <summary>
		/// Value of the last error message text  
		/// </summary>
		public string LastErrorMessage = "";

		/// <summary>
		/// Represents the initial handshake parameters received from the socket.io service (SID, HeartbeatTimeout etc)
		/// </summary>
		public SocketIOHandshake HandShake { get; private set; }

		/// <summary>
		/// Returns boolean of ReadyState == WebSocketState.Open
		/// </summary>
		public bool IsConnected
		{
			get
			{
				return this.ReadyState == WebSocketState.Open;
			}
		}

		/// <summary>
		/// Connection state of websocket client: None, Connecting, Open, Closing, Closed
		/// </summary>
		public WebSocketState ReadyState
		{
			get
			{
				if (this.wsClient != null)
					return this.wsClient.State;
				else
					return WebSocketState.None;
			}
		}

		// Constructors
		public Client(string url)
			: this(url, WebSocketVersion.Rfc6455)
		{
		}

		public Client(string url, WebSocketVersion socketVersion)
		{
			this.uri = new Uri(url);

			this.socketVersion = socketVersion;

			this.registrationManager = new RegistrationManager();
			this.outboundQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
			this.dequeuOutBoundMsgTask = Task.Factory.StartNew(() => dequeuOutboundMessages(), TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Initiate the connection with Socket.IO service
		/// </summary>
		public void Connect()
		{
			lock (padLock)
			{
				if (!(this.ReadyState == WebSocketState.Connecting || this.ReadyState == WebSocketState.Open))
				{
					try
					{
						this.HandShake = this.requestHandshake(uri);// perform an initial HTTP request as a new, non-handshaken connection

						if (this.HandShake == null || string.IsNullOrWhiteSpace(this.HandShake.SID) || this.HandShake.HadError)
						{
							this.LastErrorMessage = string.Format("Error initializing handshake with {0}", uri.ToString());
							this.OnErrorEvent(this, new ErrorEventArgs(this.LastErrorMessage, new Exception()));
						}
						else
						{
							string wsScheme = (uri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws");
							this.wsClient = new WebSocket(
								string.Format("{0}://{1}:{2}/socket.io/1/websocket/{3}", wsScheme, uri.Host, uri.Port, this.HandShake.SID),
								string.Empty,
								this.socketVersion);

							this.wsClient.Opened += this.wsClient_OpenEvent;
							this.wsClient.MessageReceived += this.wsClient_MessageReceived;
							this.wsClient.Error += this.wsClient_Error;

							this.wsClient.Closed += wsClient_Closed;

							this.wsClient.Open();
						}
					}
					catch (Exception ex)
					{
						Trace.WriteLine(string.Format("Connect threw an exception...{0}", ex.Message));
						this.OnErrorEvent(this, new ErrorEventArgs("SocketIO.Client.Connect threw an exception", ex));
					}
				}
			}
		}
		public IEndPointClient Connect(string endPoint)
		{
			EndPointClient nsClient = new EndPointClient(this, endPoint);
			this.Connect();
			this.Send(new ConnectMessage(endPoint));
			return nsClient;
		}

		protected void ReConnect()
		{
			if (this.ConnectionRetryAttempt != null)
			{
				try { this.ConnectionRetryAttempt(this, EventArgs.Empty); }
				catch (Exception ex) { Trace.WriteLine(ex); }
			}
			Trace.WriteLine(string.Format("Attempting to reconnect: {0}", this.retryConnectionCount));

			this.closeHeartBeatTimer(); // stop the heartbeat time
			this.closeWebSocketClient();// stop websocket

			this.Connect();

			bool connected = this.ConnectionOpenEvent.WaitOne(4000);
			Trace.WriteLine(string.Format("Retry-Connection successful: {0}", connected));
			if (connected) {
				this.retryConnectionCount = 0;
			} else {
				this.wsClient_Closed(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// <para>Asynchronously calls the action delegate on event message notification</para>
		/// <para>Mimicks the Socket.IO client 'socket.on('name',function(data){});' pattern</para>
		/// <para>Reserved socket.io event names available: connect, disconnect, open, close, error, retry, reconnect  </para>
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="action"></param>
		/// <example>
		/// client.On("testme", (data) =>
		///    {
		///        Debug.WriteLine(data.ToJson());
		///    });
		/// </example>
		public virtual void On(
			string eventName,
			Action<IMessage> action)
		{
			this.registrationManager.AddOnEvent(eventName, action);
		}
		public virtual void On(
			string eventName,
			string endPoint,
			Action<IMessage> action)
		{
			this.registrationManager.AddOnEvent(eventName, endPoint, action);
		}
		/// <summary>
		/// <para>Asynchronously sends payload using eventName</para>
		/// <para>payload must a string or Json Serializable</para>
		/// <para>Mimicks Socket.IO client 'socket.emit('name',payload);' pattern</para>
		/// <para>Do not use the reserved socket.io event names: connect, disconnect, open, close, error, retry, reconnect</para>
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="payload">must be a string or a Json Serializable object</param>
		/// <remarks>ArgumentOutOfRangeException will be thrown on reserved event names</remarks>
		public void Emit(string eventName, dynamic payload, string endPoint = "", Action<dynamic> callback = null)
		{

			string lceventName = eventName.ToLower();
			IMessage msg = null;
			switch (lceventName)
			{
				case "message":
					if (payload is string)
						msg = new TextMessage() { MessageText = payload };
					else
						msg = new JSONMessage(payload);
					this.Send(msg);
					break;
				case "connect":
				case "disconnect":
				case "open":
				case "close":
				case "error":
				case "retry":
				case "reconnect":
					throw new System.ArgumentOutOfRangeException(eventName, "Event name is reserved by socket.io, and cannot be used by clients or servers with this message type");
				default:
					if (!string.IsNullOrWhiteSpace(endPoint) && !endPoint.StartsWith("/"))
						endPoint = "/" + endPoint;
					msg = new EventMessage(eventName, payload, endPoint, callback);
					if (callback != null)
						this.registrationManager.AddCallBack(msg);

					this.Send(msg);
					break;
			}
		}

		/// <summary>
		/// <para>Asynchronously sends payload using eventName</para>
		/// <para>payload must a string or Json Serializable</para>
		/// <para>Mimicks Socket.IO client 'socket.emit('name',payload);' pattern</para>
		/// <para>Do not use the reserved socket.io event names: connect, disconnect, open, close, error, retry, reconnect</para>
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="payload">must be a string or a Json Serializable object</param>
		public void Emit(string eventName, dynamic payload)
		{
			this.Emit(eventName, payload, string.Empty, null);
		}

		/// <summary>
		/// Queue outbound message
		/// </summary>
		/// <param name="msg"></param>
		public void Send(IMessage msg)
		{
			this.MessageQueueEmptyEvent.Reset();
			if (this.outboundQueue != null)
				this.outboundQueue.Add(msg.Encoded);
		}

		private void Send(string rawEncodedMessageText)
		{
			this.MessageQueueEmptyEvent.Reset();
			if (this.outboundQueue != null)
				this.outboundQueue.Add(rawEncodedMessageText);
		}

		/// <summary>
		/// if a registerd event name is found, don't raise the more generic Message event
		/// </summary>
		/// <param name="msg"></param>
		protected void OnMessageEvent(IMessage msg)
		{
			bool skip = false;
			if (!string.IsNullOrEmpty(msg.Event))
				skip = this.registrationManager.InvokeOnEvent(msg); // 

			var handler = this.Message;
			if (handler != null && !skip)
				handler(this, new MessageEventArgs(msg));
		}

		public void Close()
		{
			this.retryConnectionCount = 0; // reset for next connection cycle
			// stop the heartbeat time
			this.closeHeartBeatTimer();

			// stop outbound messages
			this.closeOutboundQueue();

			this.closeWebSocketClient();

			if (this.registrationManager != null)
			{
				this.registrationManager.Dispose();
				this.registrationManager = null;
			}

		}

		protected void closeHeartBeatTimer()
		{
			// stop the heartbeat timer
			if (this.socketHeartBeatTimer != null)
			{
				this.socketHeartBeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
				this.socketHeartBeatTimer.Dispose();
				this.socketHeartBeatTimer = null;
			}
		}
		protected void closeOutboundQueue()
		{
			// stop outbound messages
			if (this.outboundQueue != null)
			{
				this.outboundQueue.CompleteAdding(); // stop adding any more items;
				this.dequeuOutBoundMsgTask.Wait(700); // wait for dequeue thread to stop
				this.outboundQueue.Dispose();
				this.outboundQueue = null;
			}
		}
		protected void closeWebSocketClient()
		{
			if (this.wsClient != null)
			{
				// unwire events
				this.wsClient.Closed -= this.wsClient_Closed;
				this.wsClient.MessageReceived -= wsClient_MessageReceived;
				this.wsClient.Error -= wsClient_Error;
				this.wsClient.Opened -= this.wsClient_OpenEvent;

				if (this.wsClient.State == WebSocketState.Connecting || this.wsClient.State == WebSocketState.Open)
					this.wsClient.Close();
				this.wsClient = null;
			}
		}

		// websocket client events - open, messages, errors, closing
		private void wsClient_OpenEvent(object sender, EventArgs e)
		{
			this.socketHeartBeatTimer = new Timer(OnHeartBeatTimerCallback, new object(), HandShake.HeartbeatInterval, HandShake.HeartbeatInterval);
			this.ConnectionOpenEvent.Set();

			this.OnMessageEvent(new EventMessage() { Event = "open" });
			if (this.Opened != null)
			{
				try { this.Opened(this, EventArgs.Empty); }
				catch (Exception ex) { Trace.WriteLine(ex); }
			}

		}

		/// <summary>
		/// Raw websocket messages from server - convert to message types and call subscribers of events and/or callbacks
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void wsClient_MessageReceived(object sender, MessageReceivedEventArgs e)
		{
			Trace.WriteLine(string.Format("webSocket_OnMessage: {0}", e.Message));

			IMessage iMsg = SocketIOClient.Messages.Message.Factory(e.Message);
			switch (iMsg.MessageType)
			{
				case SocketIOMessageTypes.Disconnect:
					this.OnMessageEvent(iMsg);
					if (string.IsNullOrWhiteSpace(iMsg.Endpoint)) // Disconnect the whole socket
						this.Close();
					break;
				case SocketIOMessageTypes.Heartbeat:
					this.OnHeartBeatTimerCallback(null);
					break;
				case SocketIOMessageTypes.Connect:
				case SocketIOMessageTypes.Message:
				case SocketIOMessageTypes.JSONMessage:
				case SocketIOMessageTypes.Event:
				case SocketIOMessageTypes.Error:
					this.OnMessageEvent(iMsg);
					break;
				case SocketIOMessageTypes.ACK:
					this.registrationManager.InvokeCallBack(iMsg.AckId, iMsg.Json);
					break;
				default:
					Trace.WriteLine("unknown wsClient message Received...");
					break;
			}
		}

		/// <summary>
		/// websocket has closed unexpectedly - retry connection
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void wsClient_Closed(object sender, EventArgs e)
		{
			if (retryConnectionCount < this.RetryConnectionAttempts)
			{
				retryConnectionCount++;
				this.ConnectionOpenEvent.Reset();
				this.ReConnect();
			}
			else
			{
				this.Close();
				if (this.SocketConnectionClosed != null)
				{
					try { this.SocketConnectionClosed(this, EventArgs.Empty); }
					catch { }
				}
			}
		}

		private void wsClient_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
		{
			this.OnErrorEvent(sender, new ErrorEventArgs("SocketClient error", e.Exception));
		}

		protected void OnErrorEvent(object sender, ErrorEventArgs e)
		{
			this.LastErrorMessage = e.Message;
			if (this.Error != null)
			{
				try { this.Error.Invoke(this, e); }
				catch { }
			}
			Trace.WriteLine(string.Format("Error Event: {0}\r\n\t{1}", e.Message, e.Exception));
		}

		// Housekeeping
		protected void OnHeartBeatTimerCallback(object state)
		{
			if (this.ReadyState == WebSocketState.Open)
			{
				IMessage msg = new Heartbeat();
				try
				{
					if (this.outboundQueue != null && !this.outboundQueue.IsAddingCompleted)
					{
						this.outboundQueue.Add(msg.Encoded);
						if (this.HeartBeatTimerEvent != null)
						{
							this.HeartBeatTimerEvent.BeginInvoke(this, EventArgs.Empty, EndAsyncEvent, null);
						}
					}
				}
				catch(Exception ex)
				{
					// 
					Trace.WriteLine(string.Format("OnHeartBeatTimerCallback Error Event: {0}\r\n\t{1}", ex.Message, ex.InnerException));
				}
			}
		}
		private void EndAsyncEvent(IAsyncResult result)
		{
			var ar = (System.Runtime.Remoting.Messaging.AsyncResult)result;
			var invokedMethod = (EventHandler)ar.AsyncDelegate;

			try
			{
				invokedMethod.EndInvoke(result);
			}
			catch
			{
				// Handle any exceptions that were thrown by the invoked method
				Trace.WriteLine("An event listener went kaboom!");
			}
		}
		/// <summary>
		/// While connection is open, dequeue and send messages to the socket server
		/// </summary>
		protected void dequeuOutboundMessages()
		{
			while (this.outboundQueue != null && !this.outboundQueue.IsAddingCompleted)
			{
				if (this.ReadyState == WebSocketState.Open)
				{
					string msgString;
					if (this.outboundQueue.TryTake(out msgString, 500))
					{
						try
						{
							Trace.WriteLine(string.Format("webSocket_Send: {0}", msgString));
							this.wsClient.Send(msgString);
						}
						catch { }
					}
					else
						this.MessageQueueEmptyEvent.Set();
				}
				else
				{
					this.ConnectionOpenEvent.WaitOne(2000); // wait for connection event
				}
			}
		}

		/// <summary>
		/// <para>Client performs an initial HTTP POST to obtain a SessionId (sid) assigned to a client, followed
		///  by the heartbeat timeout, connection closing timeout, and the list of supported transports.</para>
		/// <para>The tansport and sid are required as part of the ws: transport connection</para>
		/// </summary>
		/// <param name="uri">http://localhost:3000</param>
		/// <returns>Handshake object with sid value</returns>
		/// <example>DownloadString: 13052140081337757257:15:25:websocket,htmlfile,xhr-polling,jsonp-polling</example>
		protected SocketIOHandshake requestHandshake(Uri uri)
		{
			string value = string.Empty;
			string errorText = string.Empty;
			SocketIOHandshake handshake = null;

			using (WebClient client = new WebClient())
			{ 
				try
				{
					value = client.DownloadString(string.Format("{0}://{1}:{2}/socket.io/1/",uri.Scheme, uri.Host, uri.Port));
					// 13052140081337757257:15:25:websocket,htmlfile,xhr-polling,jsonp-polling
					if (string.IsNullOrEmpty(value))
						errorText = "Did not receive handshake string from server";
				}
				catch (Exception ex)
				{
					errorText = string.Format("Error getting handsake from Socket.IO host instance: {0}", ex.Message);
					//this.OnErrorEvent(this, new ErrorEventArgs(errMsg));
				}
			}
			if (string.IsNullOrEmpty(errorText))
				handshake = SocketIOHandshake.LoadFromString(value);
			else
			{
				handshake = new SocketIOHandshake();
				handshake.ErrorMessage = errorText;
			}

			return handshake;
		}

		public void Dispose()
		{
			this.Close();
			this.MessageQueueEmptyEvent.Dispose();
			this.ConnectionOpenEvent.Dispose();
		}
	}


}
