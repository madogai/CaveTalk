namespace Drumcan.SocketIO {
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

	public enum Reason {
		Timeout, Unknown,
	}
}
