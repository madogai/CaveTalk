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

		private OptionBaseViewModel generalOption;
		private OptionBaseViewModel commentOption;

		public ICommand GeneralOptionOpenCommand { get; private set; }
		public ICommand CommentOptionOpenCommand { get; private set; }
		public ICommand NotifyOptionOpenCommand { get; private set; }
		public ICommand SaveCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }

		public OptionWindowViewModel() {
			this.generalOption = new GeneralOptionViewModel();
			this.commentOption = new CommentOptionViewModel();
			this.OptionWindow = this.commentOption;

			this.GeneralOptionOpenCommand = new RelayCommand(p => this.OptionWindow = this.generalOption);
			this.CommentOptionOpenCommand = new RelayCommand(p => this.OptionWindow = this.commentOption);
			this.SaveCommand = new RelayCommand(p => {
				this.generalOption.Save();
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
