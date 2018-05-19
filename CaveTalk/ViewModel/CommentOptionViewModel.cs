namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Threading;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class CommentOptionViewModel : OptionBaseViewModel {
		private Config config;

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

		public Boolean EnableFlashCommentGenerator {
			get { return this.config.EnableFlashCommentGenerator; }
			set {
				this.config.EnableFlashCommentGenerator = value;
				base.OnPropertyChanged("EnableFlashCommentGenerator");
			}
		}

		public String FlashCommentGeneratorDatFilePath {
			get { return this.config.FlashCommentGeneratorDatFilePath; }
			set {
				this.config.FlashCommentGeneratorDatFilePath = value;
				base.OnPropertyChanged("FlashCommentGeneratorDatFilePath");
			}
		}

		public Boolean EnableHtml5CommentGenerator {
			get { return this.config.EnableHtml5CommentGenerator; }
			set {
				this.config.EnableHtml5CommentGenerator = value;
				base.OnPropertyChanged("EnableHtml5CommentGenerator");
			}
		}

		public String Html5CommentGeneratorCommentFilePath {
			get { return this.config.Html5CommentGeneratorCommentFilePath; }
			set {
				this.config.Html5CommentGeneratorCommentFilePath = value;
				base.OnPropertyChanged("Html5CommentGeneratorCommentFilePath");
			}
		}

		public ICommand FindFlashCommentGeneratorDatCommand { get; private set; }
		public ICommand FindHtml5CommentGeneratorCommentFileCommand { get; private set; }

		public CommentOptionViewModel() {
			this.config = Config.GetConfig();
			this.FindFlashCommentGeneratorDatCommand = new RelayCommand(p => {
				var dialog = new OpenFileDialog {
					Filter = "DATファイル|*.dat",
					CheckFileExists = false,
					CheckPathExists = false,
					Title = "DATファイルの選択",
				};
				var result = dialog.ShowDialog();
				result.GetValueOrDefault();
				if (result.GetValueOrDefault() == false) {
					return;
				}
				this.FlashCommentGeneratorDatFilePath = dialog.FileName;
			});
			this.FindHtml5CommentGeneratorCommentFileCommand = new RelayCommand(p => {
				var dialog = new OpenFileDialog {
					Filter = "コメントファイル|*.xml",
					CheckFileExists = false,
					CheckPathExists = false,
					Title = "コメントファイルの選択",
				};
				var result = dialog.ShowDialog();
				result.GetValueOrDefault();
				if (result.GetValueOrDefault() == false) {
					return;
				}
				this.Html5CommentGeneratorCommentFilePath = dialog.FileName;
			});
		}

		protected override void OnDispose() {
			base.OnDispose();
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
