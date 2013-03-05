namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Configuration;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTubeClient;
	using NLog;

	public sealed class LoginBoxViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private Cursor cursor;
		public Cursor Cursor {
			get { return this.cursor; }
			set {
				this.cursor = value;
				base.OnPropertyChanged("Cursor");
			}
		}

		private String userId;
		public String UserId {
			get { return this.userId; }
			set {
				this.userId = value;
				base.OnPropertyChanged("UserId");
			}
		}

		private String password;
		public String Password {
			get { return this.password; }
			set {
				this.password = value;
				base.OnPropertyChanged("Password");
			}
		}

		private String errorMessage;
		public String ErrorMessage {
			get { return this.errorMessage; }
			set {
				this.errorMessage = value;

				Task.Factory.StartNew(() => {
					Thread.Sleep(2000);
					this.errorMessage = String.Empty;
					base.OnPropertyChanged("ErrorMessage");
				});

				base.OnPropertyChanged("ErrorMessage");
			}
		}

		public ICommand LoginCommand { get; private set; }

		public event Action OnClose;

		public LoginBoxViewModel() {
			this.LoginCommand = new RelayCommand(Login);
		}

		private void Login(Object data) {
			this.Cursor = Cursors.Wait;
			try {
				var devKey = ConfigurationManager.AppSettings["dev_key"];
				if (String.IsNullOrWhiteSpace(devKey)) {
					throw new ConfigurationErrorsException("[dev_key]が設定されていません。");
				}

				try {
					var apiKey = CavetubeAuth.Login(this.UserId, this.Password);
					if (String.IsNullOrWhiteSpace(apiKey)) {
						this.ErrorMessage = "ログインに失敗しました。";
						return;
					}

					var config = Config.GetConfig();
					config.ApiKey = apiKey;
					config.UserId = this.UserId;
					config.Password = this.Password;
					config.Save();
				} catch (ArgumentException e) {
					var message = "ログインに失敗しました。";
					this.ErrorMessage = message;
					logger.Error(message, e);
					return;
				} catch (WebException e) {
					var message = "ログインに失敗しました。";
					this.ErrorMessage = message;
					logger.Error(message, e);
				}
			} finally {
				this.Cursor = null;
			}

			if (this.OnClose != null) {
				this.OnClose();
			}
		}
	}
}
