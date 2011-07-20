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

		private WebSocket client = null;

		public SocketIOClient(Uri socketIOUri) {
			var sessionId = this.GetSessionId(socketIOUri);
			var url = String.Format("ws://{0}:{1}/{2}/{3}/websocket/{4}", socketIOUri.Host, socketIOUri.Port, nameSpace, protocolVersion, sessionId);

			this.client = new WebSocket(url);
			this.client.OnOpen += (sender, message) => {
				if (this.OnOpen != null) {
					this.OnOpen(sender, message);
				}
			};

			this.client.OnClose += (sender, message) => {
				if (this.OnClose != null) {
					this.OnClose(sender, message);
				}
			};

			this.client.OnError += (sender, message) => {
				if (this.OnError != null) {
					this.OnError(sender, message);
				}
			};

			this.client.OnMessage += (sender, message) => {
				Console.WriteLine(message);
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
		}

		public void Connect() {
			this.client.Connect();
		}

		public void Close() {
			if (this.client == null) {
				return;
			}
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
			var packet = this.EncodePacket(message);
			this.client.Send(packet);
		}

		private String GetSessionId(Uri uri) {
			using (var wc = new WebClient()) {
				var handshakeUrl = String.Format("http://{0}:{1}/{2}/{3}", uri.Host, uri.Port, nameSpace, protocolVersion);
				var response = wc.DownloadString(handshakeUrl);
				var sessionId = Regex.Split(response, ":")[0];
				return sessionId;
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
}