namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	[Serializable]
	public sealed class ConnectionException : Exception {
		public ConnectionException()
			: base() {
		}

		public ConnectionException(String message)
			: base(message) {
		}

		public ConnectionException(String message, Exception innerException)
			: base(message, innerException) {
		}
	}
}
