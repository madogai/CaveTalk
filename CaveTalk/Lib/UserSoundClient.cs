namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using CaveTube.CaveTalk.Model;
	using System.Windows.Media;
	using System.Windows.Threading;

	public sealed class UserSoundClient : ASpeechClient {
		private String soundFilePath;
		private Config config;
		private MediaPlayer player;
		private Dispatcher dispatcher;

		public UserSoundClient() {
			this.config = Config.GetConfig();
			this.soundFilePath = config.UserSoundPath;
			this.player = new MediaPlayer();
			this.dispatcher = Dispatcher.CurrentDispatcher;
		}

		#region ASpeechClient メンバー

		public override String ApplicationName {
			get {
				return "UserSound";
			}
		}

		public override Boolean IsConnect {
			get { return base.IsConnect && this.CanSpeech(); }
		}

		private Boolean CanSpeech() {
			return File.Exists(this.soundFilePath);
		}

		public override Boolean Connect() {
			if (this.CanSpeech() == false) {
				return false;
			}

			base.Connect();
			return true;
		}

		protected override Boolean Speak(string text) {
			if (this.IsConnect == false) {
				return false;
			}

			dispatcher.BeginInvoke(new Action(() => {
				this.player.Open(new Uri(soundFilePath, UriKind.Absolute));
				this.player.MediaEnded += (sender2, e2) => this.player.Close();
				this.player.Play();
			}));
			return true;
		}

		public override void Dispose() {
		}

		#endregion
	}
}
