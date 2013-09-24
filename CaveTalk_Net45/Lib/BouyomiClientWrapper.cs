namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Runtime.Remoting;
	using CaveTube.CaveTalk.Model;
	using FNF.Utility;

	public sealed class BouyomiClientWrapper : ASpeechClient {
		private BouyomiChanClient client;
		private Config config;

		public BouyomiClientWrapper()
			: base() {
			this.client = new BouyomiChanClient();
			this.config = Config.GetConfig();
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
			} catch (RemotingException) {
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

		public override Boolean Speak(String text) {
			try {
				if (this.config.EnableBouyomiOption) {
					this.client.AddTalkTask(text, this.config.BouyomiSpeed, this.config.BouyomiVolume, this.config.BouyomiTone, VoiceType.Default);
				} else {
					this.client.AddTalkTask(text);
				}
				return true;
			} catch (RemotingException) {
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
