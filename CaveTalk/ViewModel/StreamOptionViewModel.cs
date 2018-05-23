namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Data;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class StreamOptionViewModel : OptionBaseViewModel {
		private Config config;

		public String YouTubeStreamKey {
			get { return this.config.YouTubeStreamKey; }
			set {
				this.config.YouTubeStreamKey = value;
				base.OnPropertyChanged("YouTubeStreamKey");
			}
		}

		public String YouTubeChannelId {
			get { return this.config.YouTubeChannelId; }
			set {
				this.config.YouTubeChannelId = value;
				base.OnPropertyChanged("YouTubeChannelId");
			}
		}

		public String MixerStreamKey {
			get { return this.config.MixerStreamKey; }
			set {
				this.config.MixerStreamKey = value;
				base.OnPropertyChanged("MixerStreamKey");
			}
		}

		public String MixerUserId {
			get { return this.config.MixerUserId; }
			set {
				this.config.MixerUserId = value;
				base.OnPropertyChanged("MixerUserId");
			}
		}

		public String TwitchStreamKey {
			get { return this.config.TwitchStreamKey; }
			set {
				this.config.TwitchStreamKey = value;
				base.OnPropertyChanged("TwitchStreamKey");
			}
		}

		public String TwitchUserId {
			get { return this.config.TwitchUserId; }
			set {
				this.config.TwitchUserId = value;
				base.OnPropertyChanged("TwitchUserId");
			}
		}

		public StreamOptionViewModel() {
			this.config = Config.GetConfig();
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
