using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Drumcan.SocketIO {
	public interface ITransport : IDisposable {
		event Action<Object, EventArgs> OnOpen;
		event Action<Object, String> OnMessage;
		event Action<Object, String> OnError;
		event Action<Object, EventArgs> OnClose;

		Boolean IsConnect { get; }

		void Connect();

		void Close();

		void Send(String message);
	}
}
