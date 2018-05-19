namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Data;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class YouTubeOptionViewModel : OptionBaseViewModel {
		private Config config;

		public String StreamKey {
			get { return this.config.YouTubeStreamKey; }
			set {
				this.config.YouTubeStreamKey = value;
				base.OnPropertyChanged("StreamKey");
			}
		}

		public String ChannelId {
			get { return this.config.YouTubeChannelId; }
			set {
				this.config.YouTubeChannelId = value;
				base.OnPropertyChanged("ChannelId");
			}
		}

		public YouTubeOptionViewModel() {
			this.config = Config.GetConfig();
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
