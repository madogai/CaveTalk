namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Utils;
	using NLog;

	public sealed class InstantMessageBoxViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private Boolean isOpen;
		public Boolean IsOpen {
			get { return this.isOpen; }
			set {
				this.isOpen = value;
				base.OnPropertyChanged("IsOpen");
			}
		}

		private String message;
		public String Message {
			get { return this.message; }
			set {
				this.message = value;
				base.OnPropertyChanged("Message");
			}
		}

		public ICommand OpenMessageCommand { get; private set; }

		public InstantMessageBoxViewModel(String message) {
			this.IsOpen = false;
			this.Message = message;

			this.OpenMessageCommand = new RelayCommand(p => {
				this.IsOpen = true;
			});
		}
	}
}
