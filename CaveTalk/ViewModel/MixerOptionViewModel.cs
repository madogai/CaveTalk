namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Data;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class MixerOptionViewModel : OptionBaseViewModel {
		private Config config;

		public String StreamKey {
			get { return this.config.MixerStreamKey; }
			set {
				this.config.MixerStreamKey = value;
				base.OnPropertyChanged("StreamKey");
			}
		}

		public String UserId {
			get { return this.config.MixerUserId; }
			set {
				this.config.MixerUserId = value;
				base.OnPropertyChanged("UserId");
			}
		}

		public MixerOptionViewModel() {
			this.config = Config.GetConfig();
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
