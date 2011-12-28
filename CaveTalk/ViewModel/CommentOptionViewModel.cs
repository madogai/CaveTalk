namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Properties;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class CommentOptionViewModel : OptionBaseViewModel {

		private ReadingApplicationEnum readingApplication;

		public ReadingApplicationEnum ReadingApplication {
			get { return this.readingApplication; }
			set {
				this.readingApplication = value;
				base.OnPropertyChanged("ReadingApplication");
			}
		}

		private String softalkFilePath;

		public String SoftalkFilePath {
			get { return this.softalkFilePath; }
			set {
				this.softalkFilePath = value;
				base.OnPropertyChanged("SoftalkFilePath");
			}
		}

		private CommentPopupStateEnum popupState;

		public CommentPopupStateEnum PopupState {
			get { return this.popupState; }
			set {
				this.popupState = value;
				base.OnPropertyChanged("PopupState");
			}
		}

		private Int32 popupTime;

		public Int32 PopupTime {
			get { return this.popupTime; }
			set {
				this.popupTime = value;
				base.OnPropertyChanged("PopupTime");
			}
		}

		private Boolean readNum;

		public Boolean ReadNum {
			get { return this.readNum; }
			set {
				this.readNum = value;
				base.OnPropertyChanged("ReadNum");
			}
		}

		public Boolean readName;

		public Boolean ReadName {
			get { return this.readName; }
			set {
				this.readName = value;
				base.OnPropertyChanged("ReadName");
			}
		}

		public ICommand FindSoftalkExeCommand { get; private set; }

		public CommentOptionViewModel() {
			this.ReadingApplication = (ReadingApplicationEnum)Settings.Default.ReadingApplication;
			this.PopupTime = Settings.Default.CommentPopupTime;
			this.PopupState = (CommentPopupStateEnum)Settings.Default.CommentPopup;
			this.SoftalkFilePath = Settings.Default.SofTalkPath;
			this.ReadNum = Settings.Default.ReadNum;
			this.ReadName = Settings.Default.ReadName;

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
			Settings.Default.ReadingApplication = (Int32)this.ReadingApplication;
			Settings.Default.SofTalkPath = this.SoftalkFilePath;
			Settings.Default.CommentPopup = (Int32)this.PopupState;
			Settings.Default.CommentPopupTime = this.PopupTime;
			Settings.Default.ReadNum = this.ReadNum;
			Settings.Default.ReadName = this.ReadName;
			Settings.Default.Save();
		}
	}
}
