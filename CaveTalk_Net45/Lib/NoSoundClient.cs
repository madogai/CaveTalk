namespace CaveTube.CaveTalk.Lib {
	using System;

	public sealed class NoSoundClient : ASpeechClient {
		#region ASpeechClient メンバー

		public override String ApplicationName {
			get { return "NoSound"; }
		}

		public override Boolean IsConnect {
			get { return true; }
		}

		private Boolean CanSpeech() {
			return true;
		}

		public override Boolean Connect() {
			return true;
		}

		public override Boolean Speak(String text) {
			return true;
		}

		public sealed override void Dispose() {
		}

		#endregion
	}
}
