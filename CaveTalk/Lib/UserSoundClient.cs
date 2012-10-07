namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using CaveTube.CaveTalk.Model;
	using System.Windows.Media;
	using System.Windows.Threading;
	using System.Threading.Tasks;

	public sealed class UserSoundClient : ASpeechClient {
		private String soundFilePath;
		private Config config;
		private MediaPlayer player;
		private Dispatcher dispatcher;
		private DispatcherTimer timer;

		public UserSoundClient() {
			this.config = Config.GetConfig();
			this.soundFilePath = this.config.UserSoundFilePath;
			this.player = new MediaPlayer();
			this.player.Volume = this.config.UserSoundVolume;
			this.dispatcher = Dispatcher.CurrentDispatcher;
			if (File.Exists(this.soundFilePath)) {
				this.player.Open(new Uri(this.soundFilePath, UriKind.Absolute));
			}
			this.timer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher) {
				Interval = TimeSpan.FromSeconds(Decimal.ToDouble(config.UserSoundTimeout)),
			};
			this.timer.Tick += (e, sender) => {
				this.player.Stop();
				this.timer.Stop();
			};
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

		public override Boolean Speak(String text) {
			if (this.IsConnect == false) {
				return false;
			}

			dispatcher.BeginInvoke(new Action(() => {
				this.player.Stop();
				this.player.Play();
				this.timer.Start();
			}));
			return true;
		}

		public override void Dispose() {
			if (this.player.Source != null) {
				this.player.Close();
			}
		}

		#endregion
	}
}
