namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Linq;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class CommentOptionViewModel : OptionBaseViewModel {
		private Config config;

		public Config.SpeakApplicationType SpeakApplication {
			get { return this.config.SpeakApplication; }
			set {
				this.config.SpeakApplication = value;
				base.OnPropertyChanged("SpeakApplication");
			}
		}

		public String SoftalkFilePath {
			get { return this.config.SofTalkPath; }
			set {
				this.config.SofTalkPath = value;
				base.OnPropertyChanged("SoftalkFilePath");
			}
		}

		public Config.CommentPopupDisplayType PopupState {
			get { return this.config.CommentPopupType; }
			set {
				this.config.CommentPopupType = value;
				base.OnPropertyChanged("PopupState");
			}
		}

		public Int32 PopupTime {
			get { return this.config.CommentPopupTime; }
			set {
				this.config.CommentPopupTime = value;
				base.OnPropertyChanged("PopupTime");
			}
		}

		public Boolean ReadNum {
			get { return this.config.ReadCommentNumber; }
			set {
				this.config.ReadCommentNumber = value;
				base.OnPropertyChanged("ReadNum");
			}
		}

		public Boolean ReadName {
			get { return this.config.ReadCommentName; }
			set {
				this.config.ReadCommentName = value;
				base.OnPropertyChanged("ReadName");
			}
		}

		public ICommand FindSoftalkExeCommand { get; private set; }

		public CommentOptionViewModel() {
			this.config = Config.GetConfig();

			this.FindSoftalkExeCommand = new RelayCommand(p => {
				var dialog = new OpenFileDialog {
					Filter = "実行ファイル|*.exe",
					CheckFileExists = true,
					CheckPathExists = true,
					Title = "Softalkの選択",
				};
				var result = dialog.ShowDialog();
				result.GetValueOrDefault();
				if (result.GetValueOrDefault() == false) {
					return;
				}
				this.SoftalkFilePath = dialog.FileName;
			});
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
