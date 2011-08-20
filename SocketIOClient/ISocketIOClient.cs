namespace SocketIO {
	using System;

	public interface ISocketIOClient : IDisposable {
		event Action<Object, EventArgs> OnOpen;
		event Action<Object, String> OnMessage;
		event Action<Object, String> OnError;
		event Action<Object, Reason> OnClose;

		Boolean IsConnect { get; }
		String SessionId { get;  }

		void Connect();

		void Close();

		void Send(String message);
	}

	[Serializable]
	public class Reason {
		public Boolean IsTimeout { get; private set; }

		public Reason(Boolean isTimeout) {
			this.IsTimeout = IsTimeout;
		}
	}
}
