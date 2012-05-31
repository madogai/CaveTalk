namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Runtime.Remoting;
	using FNF.Utility;

	public sealed class BouyomiClientWrapper : ASpeechClient {
		private BouyomiChanClient client;

		public BouyomiClientWrapper()
			: base() {
			this.client = new BouyomiChanClient();
		}

		#region ASpeechClient メンバー

		public override String ApplicationName {
			get { return "棒読みちゃん"; }
		}

		public override Boolean IsConnect {
			get { return base.IsConnect && this.CanSpeech(); }
		}

		private Boolean CanSpeech() {
			try {
				var count = this.client.TalkTaskCount;
				return true;
			}
			catch (RemotingException) {
				return false;
			}
		}

		public override Boolean Connect() {
			if (this.CanSpeech() == false) {
				return false;
			}
			base.Connect();
			return true;
		}

		protected override Boolean Speak(String text) {
			try {
				this.client.AddTalkTask(text);
				return true;
			}
			catch (RemotingException) {
				return false;
			}
		}

		public override void Dispose() {
			if (this.client != null) {
				this.client.Dispose();
			}
		}

		#endregion
	}
}
