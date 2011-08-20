namespace SocketIO {

	using System;
	using System.Linq;
	using System.Net;
	using System.Text.RegularExpressions;
	using WebSocketSharp;
	using System.Diagnostics;
	using System.Threading;

	public class SocketIOClient : ISocketIOClient {
		private const String nameSpace = "socket.io";
		private const String protocolVersion = "1";

		private static ITransport CreateWebSocketClient(Uri sessionUrl, String sessionId) {
			var webSocketUrl = String.Format("ws://{0}:{1}/{2}/{3}/websocket/{4}", sessionUrl.Host, sessionUrl.Port, nameSpace, protocolVersion, sessionId);
			return new WebSocket(webSocketUrl);
		}

		public event Action<Object, EventArgs> OnOpen;
		public event Action<Object, String> OnMessage;
		public event Action<Object, String> OnError;
		public event Action<Object, Reason> OnClose;

		// 可能ならばclientの作成はコンストラクタのみにとどめたいのですが、
		// 今のところ再接続を行うためにclientのインスタンスを再度生成しないといけないのでクラス変数を用意します。
		private Uri socketIOUri;
		private Func<Uri, String, ITransport> clientBuilder;

		public Boolean IsConnect {
			get {
				if (this.client == null) {
					return false;
				}
				return this.client.IsConnect;
			}
		}
		public String SessionId { get; private set; }

		private ITransport client = null;
		private Int32 timeout;

		public SocketIOClient(Uri socketIOUri)
			: this(socketIOUri, CreateWebSocketClient) {
		}

		private SocketIOClient(Uri socketIOUri, Func<Uri, String, ITransport> clientBuilder) {
			this.socketIOUri = socketIOUri;
			this.clientBuilder = clientBuilder;
		}

		~SocketIOClient() {
			this.Dispose();
		}

		public void Connect() {
			var handshakeInfo = this.GetHandshakeInfo(this.socketIOUri);
			var client = clientBuilder(this.socketIOUri, handshakeInfo.SessionId);
			this.client = client;

			client = this.SetupClientEvent(client);

			this.SessionId = handshakeInfo.SessionId;
			this.timeout = handshakeInfo.Timeout;

			client.Connect();
		}

		public void Close() {
			if (this.client == null) {
				return;
			}
			this.client.Send("0");
			this.client.Close();
		}

		public void Dispose() {
			if (this.client == null) {
				return;
			}
			this.client.Dispose();
			this.client = null;
		}

		public void Send(String message) {
			if (this.client == null) {
				throw new SocketIOException("サーバに接続されていません。");
			}
			var packet = this.EncodePacket(message);
			this.client.Send(packet);
		}

		private ITransport SetupClientEvent(ITransport client) {
			var stopwatch = new Stopwatch();
			var timer = new Timer(state => {
				var sw = (Stopwatch)state;
				if (sw.ElapsedMilliseconds > this.timeout) {
					stopwatch.Stop();
					client.Close();
				}
			}, stopwatch, Timeout.Infinite, Timeout.Infinite);

			client.OnOpen += (sender, e) => {
				stopwatch.Restart();
				timer.Change(this.timeout, this.timeout);
				if (this.OnOpen != null) {
					this.OnOpen(this, e);
				}
			};

			client.OnClose += (sender, e) => {
				var isTimeout = stopwatch.ElapsedMilliseconds > this.timeout;

				stopwatch.Reset();
				timer.Change(Timeout.Infinite, Timeout.Infinite);
				if (this.OnClose != null) {
					var reason = new Reason(isTimeout);
					this.OnClose(this, reason);
				}
			};

			client.OnError += (sender, message) => {
				Debug.WriteLine(message);
				if (this.OnError != null) {
					this.OnError(this, message);
				}
			};

			client.OnMessage += (sender, message) => {
				Debug.WriteLine(message);
				try {
					var status = (Status)Int32.Parse(message.Substring(0, 1));
					switch (status) {
						case Status.Disconnect:
							break;
						case Status.Connect:
							break;
						case Status.Heartbeat:
							stopwatch.Restart();
							client.Send("2::");
							break;
						case Status.Message:
							if (this.OnMessage != null) {
								message = Regex.Replace(message, @"^3:[^:]*?:[^:]*?:", String.Empty);
								this.OnMessage(this, message);
							}
							break;
						case Status.JSONMessage:
							if (this.OnMessage != null) {
								this.OnMessage(this, message);
							}
							break;
						case Status.Event:
							break;
						case Status.ACK:
							break;
						case Status.Error:
							break;
						case Status.Noop:
							break;
						default:
							throw new SocketIOException("SocketIOの型と一致しないメッセージを受信しました。");
					}
				} catch (System.FormatException) {
					throw new SocketIOException("SocketIOの型と一致しないメッセージを受信しました。");
				}
			};
			return client;
		}

		private String EncodePacket(String message) {
			var id = String.Empty;
			var endpoint = String.Empty;
			return String.Format("3:{0}:{1}:{2}", id, endpoint, message);
		}

		private HandshakeInfo GetHandshakeInfo(Uri uri) {
			try {
				using (var wc = new WebClient()) {
					var handshakeUrl = String.Format("http://{0}:{1}/{2}/{3}", uri.Host, uri.Port, nameSpace, protocolVersion);
					var response = wc.DownloadString(handshakeUrl);
					var infos = Regex.Split(response, ":");
					var sessionId = infos.ElementAtOrDefault(0);
					var heartbeatText = infos.ElementAtOrDefault(1);
					var heartbeat = String.IsNullOrEmpty(heartbeatText) == false ? (Int32.Parse(heartbeatText) * 1000) : 0;
					var timoutText = infos.ElementAtOrDefault(2);
					var timeout = String.IsNullOrEmpty(timoutText) == false ? (Int32.Parse(timoutText) * 1000) : (25 * 1000);
					return new HandshakeInfo {
						SessionId = sessionId,
						Heartbeat = heartbeat,
						Timeout = timeout,
					};
				}
			} catch (WebException e) {
				throw new SocketIOException("SessionIdを取得できません。", e);
			}
		}

		private sealed class HandshakeInfo {
			public String SessionId { get; set; }
			public Int32 Heartbeat { get; set; }
			public Int32 Timeout { get; set; }
		}

		private enum Status {
			Disconnect = 0,
			Connect,
			Heartbeat,
			Message,
			JSONMessage,
			Event,
			ACK,
			Error,
			Noop,
		}
	}

	[Serializable]
	public sealed class SocketIOException : Exception {

		public SocketIOException()
			: base() {
		}

		public SocketIOException(String message)
			: base(message) {
		}

		public SocketIOException(String message, Exception innerException)
			: base(message, innerException) {
		}
	}
}