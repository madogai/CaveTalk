namespace CaveTube.CaveTubeClient {
	using System;

	[Serializable]
	public sealed class CavetubeException : Exception {
		public CavetubeException()
			: base() {
		}

		public CavetubeException(String message)
			: base(message) {
		}

		public CavetubeException(String message, Exception innerException)
			: base(message, innerException) {
		}
	}
}
