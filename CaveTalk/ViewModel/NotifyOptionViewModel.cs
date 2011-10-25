using System;
using System.Windows.Input;
using CaveTube.CaveTalk.Properties;
using CaveTube.CaveTalk.Utils;
using Microsoft.Win32;

namespace CaveTube.CaveTalk.ViewModel {
	public sealed class NotifyOptionViewModel : OptionBaseViewModel {
		private NotifyPopupStateEnum notifyState;

		public NotifyPopupStateEnum NotifyState {
			get { return notifyState; }
			set {
				notifyState = value;
				base.OnPropertyChanged("NotifyState");
			}
		}

		private int popupTime;

		public int PopupTime {
			get { return this.popupTime; }
			set {
				this.popupTime = value;
				base.OnPropertyChanged("PopupTime");
			}
		}

		private String soundFilePath;

		public String SoundFilePath {
			get { return this.soundFilePath; }
			set {
				this.soundFilePath = value;
				base.OnPropertyChanged("SoundFilePath");
			}
		}

		public ICommand FindSoundFileCommand { get; private set; }

		public NotifyOptionViewModel() {
			this.NotifyState = (NotifyPopupStateEnum)Settings.Default.NotifyState;
			this.PopupTime = Settings.Default.NotifyPopupTime;
			this.SoundFilePath = Settings.Default.NotifySoundFilePath;

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
			Settings.Default.NotifyState = (int)this.NotifyState;
			Settings.Default.NotifyPopupTime = this.PopupTime;
			Settings.Default.NotifySoundFilePath = this.SoundFilePath;
			Settings.Default.Save();
		}
	}
}
