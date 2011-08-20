namespace SocketIO {
	using System;

	public interface ISocketIOClient : IDisposable {
		event Action<Object, EventArgs> OnOpen;
		event Action<Object, String> OnMessage;
		event Action<Object, String> OnError;
		event Action<Object, EventArgs> OnClose;

		Boolean IsConnect { get; }
		String SessionId { get;  }

		void Connect();

		void Close();

		void Send(String message);
	}
}
