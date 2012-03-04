namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Linq;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class NotifyOptionViewModel : OptionBaseViewModel {
		private CaveTalkContext context;
		private Config config;

		public NotifyPopupState NotifyState {
			get { return (NotifyPopupState)this.config.NotifyPopupState; }
			set {
				this.config.NotifyPopupState = (Int32)value;
				base.OnPropertyChanged("NotifyState");
			}
		}

		public Int32 PopupTime {
			get { return this.config.NotifyPopupTime; }
			set {
				this.config.NotifyPopupTime = value;
				base.OnPropertyChanged("PopupTime");
			}
		}

		public String SoundFilePath {
			get { return this.config.NotifySoundFilePath; }
			set {
				this.config.NotifySoundFilePath = value;
				base.OnPropertyChanged("SoundFilePath");
			}
		}

		public ICommand FindSoundFileCommand { get; private set; }

		public NotifyOptionViewModel() {
			this.context = new CaveTalkContext();
			this.config = this.context.Config.First();

			this.FindSoundFileCommand = new RelayCommand(p => {
				var dialog = new OpenFileDialog {
					Filter = "サウンドファイル|*.wav;*.mp3|すべてのファイル|*.*",
					CheckFileExists = true,
					CheckPathExists = true,
					Title = "通知サウンドの選択",
				};
				var result = dialog.ShowDialog();
				result.GetValueOrDefault();
				if (result.GetValueOrDefault() == false) {
					return;
				}
				this.SoundFilePath = dialog.FileName;
			});
		}

		internal override void Save() {
			this.context.SaveChanges();
		}
	}
}
