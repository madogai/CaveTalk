namespace SocketIO {

	using System;
	using System.Net;
	using System.Text.RegularExpressions;
	using WebSocketSharp;

	public sealed class SocketIOClient : IDisposable {
		private const String nameSpace = "socket.io";
		private const String protocolVersion = "1";

		public event Action<Object, EventArgs> OnOpen;
		public event Action<Object, String> OnMessage;
		public event Action<Object, String> OnError;
		public event Action<Object, EventArgs> OnClose;

		public Boolean IsConnect {
			get {
				if (this.client == null) {
					return false;
				}
				return this.client.ReadyState == WsState.OPEN;
			}
		}

		private WebSocket client = null;
		private readonly Uri socketIOUri;

		public SocketIOClient(Uri socketIOUri) {
			this.socketIOUri = socketIOUri;
			this.client = this.CreateWebSocketClient();
		}

		public void Connect() {
			this.client.Connect();
		}

		public void Close() {
			if (this.client == null) {
				return;
			}
			this.client.Send("0::");
			this.client.Close();
			this.client = this.CreateWebSocketClient();
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

		private String GetSessionId(Uri uri) {
			try {
				using (var wc = new WebClient()) {
					var handshakeUrl = String.Format("http://{0}:{1}/{2}/{3}", uri.Host, uri.Port, nameSpace, protocolVersion);
					var response = wc.DownloadString(handshakeUrl);
					var sessionId = Regex.Split(response, ":")[0];
					return sessionId;
				}
			} catch (WebException e) {
				throw new SocketIOException("SessionIdを取得できません。", e);
			}
		}

		private String EncodePacket(String message) {
			var id = String.Empty;
			var endpoint = String.Empty;
			return String.Format("3:{0}:{1}:{2}", id, endpoint, message);
		}

		~SocketIOClient() {
			this.Dispose();
		}

		private WebSocket CreateWebSocketClient() {
			var sessionId = this.GetSessionId(this.socketIOUri);
			var url = String.Format("ws://{0}:{1}/{2}/{3}/websocket/{4}", this.socketIOUri.Host, this.socketIOUri.Port, nameSpace, protocolVersion, sessionId);
			var client = new WebSocket(url);
			client.OnOpen += (sender, message) => {
				if (this.OnOpen != null) {
					this.OnOpen(sender, message);
				}
			};

			client.OnClose += (sender, message) => {
				if (this.OnClose != null) {
					this.OnClose(sender, message);
				}
			};

			client.OnError += (sender, message) => {
				if (this.OnError != null) {
					this.OnError(sender, message);
				}
			};

			client.OnMessage += (sender, message) => {
				try {
					var status = (Status)Int32.Parse(message.Substring(0, 1));
					switch (status) {
						case Status.Disconnect:
							break;
						case Status.Connect:
							break;
						case Status.Heartbeat:
							client.Send("2::");
							break;
						case Status.Message:
							if (this.OnMessage != null) {
								message = Regex.Replace(message, @"^3:[^:]*?:[^:]*?:", String.Empty);
								this.OnMessage(sender, message);
							}
							break;
						case Status.JSONMessage:
							if (this.OnMessage != null) {
								this.OnMessage(sender, message);
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