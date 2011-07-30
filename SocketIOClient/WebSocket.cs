namespace SocketIO {
	using System;
	using WebSocketSharp;

	public class WebSocket : WebSocketSharp.WebSocket, ITransport {

		#region ITrancePort メンバー

		public new event Action<object, EventArgs> OnOpen;
		public new event Action<object, string> OnMessage;
		public new event Action<object, string> OnError;
		public new event Action<object, EventArgs> OnClose;

		public Boolean IsConnect {
			get {
				return base.ReadyState == WsState.OPEN;
			}
		}

		public WebSocket(String url) : base(url) {
			base.OnOpen += (obj, e) => this.OnOpen(obj, e);
			base.OnMessage += (obj, msg) => this.OnMessage(obj, msg);
			base.OnError += (obj, msg) => this.OnError(obj, msg);
			base.OnClose += (obj, e) => this.OnClose(obj, e);
		}

		#endregion

		#region IDisposable メンバー

		public new void Dispose() {
			base.Dispose();
		}

		#endregion
	}
}
