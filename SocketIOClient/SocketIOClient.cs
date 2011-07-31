namespace SocketIO {

	using System;
	using System.Net;
	using System.Text.RegularExpressions;
	using WebSocketSharp;
	using System.Diagnostics;

	public class SocketIOClient : ISocketIOClient {
		private const String nameSpace = "socket.io";
		private const String protocolVersion = "1";

		private static ITransport CreateWebSocketClient(Uri sessionUrl, String sessionId) {
			var webSocketUrl = String.Format("ws://{0}:{1}/{2}/{3}/websocket/{4}", sessionUrl.Host, sessionUrl.Port, nameSpace, protocolVersion, sessionId);
			return new WebSocket(webSocketUrl);
		}

		private static String GetSessionId(Uri uri) {
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

		public event Action<Object, EventArgs> OnOpen;
		public event Action<Object, String> OnMessage;
		public event Action<Object, String> OnError;
		public event Action<Object, EventArgs> OnClose;

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

		private ITransport client = null;

		public SocketIOClient(Uri socketIOUri)
			: this(socketIOUri, CreateWebSocketClient) {
		}

		public SocketIOClient(Uri socketIOUri, Func<Uri, String, ITransport> clientBuilder)
			: this(socketIOUri, clientBuilder, GetSessionId(socketIOUri)) {
		}

		private SocketIOClient(Uri socketIOUri, Func<Uri, String, ITransport> clientBuilder, String sessionId) {
			this.socketIOUri = socketIOUri;
			this.clientBuilder = clientBuilder;

			var client = clientBuilder(socketIOUri, sessionId);
			client = this.SetupClientEvent(client);
			this.client = client;
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

			// ここで再接続しないと、ハンドシェイク後にエラーが返って再接続できません。
			var sessionId = GetSessionId(socketIOUri);
			var client = clientBuilder(socketIOUri, sessionId);
			client = this.SetupClientEvent(client);
			this.client = client;
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
			client.OnOpen += (sender, e) => {
				if (this.OnOpen != null) {
					this.OnOpen(sender, e);
				}
			};

			client.OnClose += (sender, e) => {
				if (this.OnClose != null) {
					this.OnClose(sender, e);
				}
			};

			client.OnError += (sender, message) => {
				Debug.WriteLine(message);
				if (this.OnError != null) {
					this.OnError(sender, message);
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