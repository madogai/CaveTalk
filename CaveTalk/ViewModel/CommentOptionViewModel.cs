namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Linq;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class CommentOptionViewModel : OptionBaseViewModel {
		private CaveTalkContext context;
		private Config config;

		public SpeakApplicationState SpeakApplication {
			get { return (SpeakApplicationState)this.config.SpeakApplication; }
			set {
				this.config.SpeakApplication = (Int32)value;
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

		public CommentPopupState PopupState {
			get { return (CommentPopupState)this.config.CommentPopupState; }
			set {
				this.config.CommentPopupState = (Int32)value;
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
			this.context = new CaveTalkContext();
			this.config = this.context.Config.First();

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
			this.context.SaveChanges();
		}
	}
}
