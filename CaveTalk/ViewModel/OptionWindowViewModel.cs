namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Utils;

	public sealed class OptionWindowViewModel : ViewModelBase {
		public event Action OnClose;

		private OptionBaseViewModel optionWindow;
		public OptionBaseViewModel OptionWindow {
			get { return optionWindow; }
			set {
				optionWindow = value;
				base.OnPropertyChanged("OptionWindow");
			}
		}

		private OptionBaseViewModel commentOption;
		private OptionBaseViewModel notifyOption;

		public ICommand CommentOptionOpenCommand { get; private set; }
		public ICommand NotifyOptionOpenCommand { get; private set; }
		public ICommand SaveCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }

		public OptionWindowViewModel() {
			this.commentOption = new CommentOptionViewModel();
			this.notifyOption = new NotifyOptionViewModel();
			this.OptionWindow = this.commentOption;

			this.CommentOptionOpenCommand = new RelayCommand(p => this.OptionWindow = this.commentOption);
			this.NotifyOptionOpenCommand = new RelayCommand(p => this.OptionWindow = this.notifyOption);
			this.SaveCommand = new RelayCommand(p => {
				this.notifyOption.Save();
				this.commentOption.Save();
				if (this.OnClose != null) {
					this.OnClose();
				} 
			});
			this.CancelCommand = new RelayCommand(p => {
				if (this.OnClose != null) {
					this.OnClose();
				}
			});
		}
	}
}
