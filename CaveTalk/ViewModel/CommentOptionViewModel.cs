using System;
using System.Windows.Input;
using CaveTube.CaveTalk.Properties;
using CaveTube.CaveTalk.Utils;
using Microsoft.Win32;

namespace CaveTube.CaveTalk.ViewModel {
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

		private int popupTime;

		public int PopupTime {
			get { return this.popupTime; }
			set {
				this.popupTime = value;
				base.OnPropertyChanged("PopupTime");
			}
		}

		public ICommand FindSoftalkExeCommand { get; private set; }

		public CommentOptionViewModel() {
			this.ReadingApplication = (ReadingApplicationEnum)Settings.Default.ReadingApplication;
			this.PopupTime = Settings.Default.CommentPopupTime;
			this.PopupState = (CommentPopupStateEnum)Settings.Default.CommentPopup;
			this.SoftalkFilePath = Settings.Default.SofTalkPath;

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
			Settings.Default.ReadingApplication = (int)this.ReadingApplication;
			Settings.Default.SofTalkPath = this.SoftalkFilePath;
			Settings.Default.CommentPopup = (int)this.PopupState;
			Settings.Default.CommentPopupTime = this.PopupTime;
			Settings.Default.Save();
		}
	}
}
