namespace CaveTube.CaveTalk.ViewModel {

	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using System.Net;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Threading;
	using CaveTube.CaveTalk.CaveTubeClient;
	using CaveTube.CaveTalk.Lib;
	using CaveTube.CaveTalk.Properties;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTalk.View;
	using NLog;
	using System.IO;
	using Microsoft.Win32;

	public sealed class MainWindowViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private CavetubeClient cavetubeClient;
		private IReadingApplicationClient readingClient;
		private Dispatcher uiDispatcher;

		public event Action<LiveNotification> OnNotifyLive;

		public event Action<Message> OnMessage;

		#region プロパティ

		private Int32 messageIndex;

		public Int32 MessageIndex {
			get { return this.messageIndex; }
			set {
				this.messageIndex = value;
				base.OnPropertyChanged("MessageIndex");
			}
		}

		public IList<Message> MessageList { get; private set; }

		private String liveUrl;

		public String LiveUrl {
			get { return this.liveUrl; }
			set {
				this.liveUrl = value;
				base.OnPropertyChanged("LiveUrl");
			}
		}

		private Int32 listener;

		public Int32 Listener {
			get { return this.listener; }
			set {
				this.listener = value;
				base.OnPropertyChanged("Listener");
			}
		}

		public Boolean readingApplicationStatus;

		public Boolean ReadingApplicationStatus {
			get { return this.readingApplicationStatus; }
			set {
				this.readingApplicationStatus = value;
				base.OnPropertyChanged("ReadingApplicationStatus");
			}
		}

		public Boolean ConnectingStatus {
			get {
				return this.cavetubeClient.IsConnect;
			}
		}

		public Boolean RoomJoinStatus {
			get {
				return String.IsNullOrEmpty(this.cavetubeClient.JoinedRoomId) == false;
			}
		}

		public Boolean LoginStatus {
			get {
				return String.IsNullOrWhiteSpace(CaveTalk.Properties.Settings.Default.ApiKey) == false;
			}
		}

		private String postName;

		public String PostName {
			get { return this.postName; }
			set {
				this.postName = value;
				base.OnPropertyChanged("PostName");
			}
		}

		private String postMessage;

		public String PostMessage {
			get { return this.postMessage; }
			set {
				this.postMessage = value;
				base.OnPropertyChanged("PostMessage");
			}
		}

		#endregion

		#region コマンド

		/// <summary>
		/// コメント部屋に接続します。
		/// </summary>
		public ICommand JoinRoomCommand { get; private set; }

		/// <summary>
		/// コメント部屋から抜けます。
		/// </summary>
		public ICommand LeaveRoomCommand { get; private set; }

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		public ICommand PostCommentCommand { get; private set; }

		/// <summary>
		/// コメントをコピーします。
		/// </summary>
		public ICommand CopyCommentCommand { get; private set; }

		/// <summary>
		/// CaveTubeにログインします。
		/// </summary>
		public ICommand LoginCommand { get; private set; }

		/// <summary>
		/// CaveTubeからログアウトします。
		/// </summary>
		public ICommand LogoutCommand { get; private set; }

		/// <summary>
		/// ログインアカウントを切り替えます。
		/// </summary>
		public ICommand SwitchAccountCommand { get; private set; }

		/// <summary>
		/// 読み上げソフトに接続します。
		/// </summary>
		public ICommand ConnectReadingApplicationCommand { get; private set; }

		/// <summary>
		/// 読み上げソフトのコネクションを切断します。
		/// </summary>
		public ICommand DisconnectReadingApplicationCommand { get; private set; }

		/// <summary>
		/// 通知を有効にします。
		/// </summary>
		public ICommand EnableNotifyCommand { get; private set; }

		/// <summary>
		/// 通知を無効にします。
		/// </summary>
		public ICommand DisableNotifyCommand { get; private set; }

		/// <summary>
		/// AboutBoxを表示します。
		/// </summary>
		public ICommand AboutBoxCommand { get; private set; }

		/// <summary>
		/// オプション画面を表示します。
		/// </summary>
		public ICommand SettingWindowCommand { get; private set; }

		#endregion

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public MainWindowViewModel() {
			this.MessageList = new SafeObservable<Message>();
			this.uiDispatcher = Dispatcher.CurrentDispatcher;

			#region Commandの登録

			this.ConnectReadingApplicationCommand = new RelayCommand(p => this.ConnectReadingApplication());
			this.DisconnectReadingApplicationCommand = new RelayCommand(p => this.DisconnectReadingApplication());
			this.JoinRoomCommand = new RelayCommand(p => this.JoinRoom(this.LiveUrl));
			this.LeaveRoomCommand = new RelayCommand(p => this.LeaveRoom());
			this.LoginCommand = new RelayCommand(p => this.LoginCavetube());
			this.LogoutCommand = new RelayCommand(p => this.LogoutCavetube());
			this.SwitchAccountCommand = new RelayCommand(p => {
				this.LogoutCavetube();
				this.LoginCavetube();
			});
			this.PostCommentCommand = new RelayCommand(p => {
				var apiKey = CaveTalk.Properties.Settings.Default.ApiKey ?? String.Empty;
				this.PostComment(this.PostName, this.PostMessage, apiKey);
			});
			this.AboutBoxCommand = new RelayCommand(p => this.ShowVersion());
			this.SettingWindowCommand = new RelayCommand(p => this.ShowOption());

			#endregion

			#region CavetubeClientの接続

			this.cavetubeClient = new CavetubeClient(new Uri(ConfigurationManager.AppSettings["comment_server"]), new Uri(ConfigurationManager.AppSettings["web_server"]));
			this.cavetubeClient.OnMessage += (summary, mes) => {
				var message = new Message(mes);
				this.AddMessage(summary, message);

				if (this.OnMessage != null) {
					uiDispatcher.BeginInvoke(new Action(() => {
						this.OnMessage(message);
					}));
				}
			};
			this.cavetubeClient.OnUpdateMember += this.UpdateListenerCount;
			this.cavetubeClient.OnJoin += (summary, messages) => {
				base.OnPropertyChanged("RoomJoinStatus");
				this.AddMessage(summary, messages.Select(m => new Message(m)));

				uiDispatcher.BeginInvoke(new Action(() => {
					Mouse.OverrideCursor = null;
				}));
			};
			this.cavetubeClient.OnNotifyLive += liveInfo => {
				if (this.OnNotifyLive == null || (NotifyPopupStateEnum)Settings.Default.NotifyState == NotifyPopupStateEnum.False) {
					return;
				}
				uiDispatcher.BeginInvoke(new Action(() => {
					this.OnNotifyLive(liveInfo);
				}));
			};
			this.cavetubeClient.OnClose += (e) => {
				if (e.IsTimeout) {
					this.cavetubeClient.Connect();
					if (this.RoomJoinStatus) {
						this.JoinRoom(this.cavetubeClient.JoinedRoomId);
					}
				}
			};

			this.cavetubeClient.Connect();

			#endregion

			this.PostName = CaveTalk.Properties.Settings.Default.UserId;

			#region 読み上げソフト

			this.ConnectReadingApplication();

			#endregion

			SystemEvents.PowerModeChanged += OnPowerModeChanged;
		}

		/// <summary>
		/// デストラクタ
		/// </summary>
		~MainWindowViewModel() {
			SystemEvents.PowerModeChanged -= OnPowerModeChanged;
		}

		/// <summary>
		/// 画面が閉じるときに呼ばれます。
		/// オブジェクトを破棄します。
		/// </summary>
		protected override void OnDispose() {
			if (this.cavetubeClient != null) {
				this.cavetubeClient.Dispose();
			}

			if (this.readingClient != null) {
				this.readingClient.Dispose();
			}
		}

		/// <summary>
		/// 視聴人数を更新します。
		/// </summary>
		/// <param name="count"></param>
		private void UpdateListenerCount(Int32 count) {
			this.Listener = count;
		}

		/// <summary>
		/// コメントを追加します。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="message"></param>
		private void AddMessage(Summary summary, Message message) {
			if (message.IsBan) {
				return;
			}

			this.MessageList.Insert(0, message);

			if (this.ReadingApplicationStatus) {
				var isAdded = this.readingClient.Add(message.Comment);
				if (isAdded == false) {
					MessageBox.Show("読み上げに失敗しました。");
					this.ReadingApplicationStatus = false;
				}
			}
		}

		/// <summary>
		/// コメントを追加します。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="messages"></param>
		private void AddMessage(Summary summary, IEnumerable<Message> messages) {
			base.OnPropertyChanged("ConnectingStatus");
			foreach (var message in messages) {
				if (message.IsBan == false) {
					this.MessageList.Insert(0, message);
				}
			}
		}

		/// <summary>
		/// 接続人数やコメント一覧をクリアして初期状態に戻します。
		/// </summary>
		private void ResetStatus() {
			base.OnPropertyChanged("ConnectingStatus");
			this.Listener = 0;
			this.MessageList.Clear();
		}

		/// <summary>
		/// コメント部屋に接続します。
		/// </summary>
		/// <param name="liveUrl"></param>
		private void JoinRoom(String liveUrl) {
			Mouse.OverrideCursor = Cursors.Wait;

			this.LeaveRoom();

			var roomId = this.ParseUrl(liveUrl);
			if (String.IsNullOrEmpty(roomId)) {
				return;
			}

			this.LiveUrl = roomId;
			try {
				this.cavetubeClient.JoinRoom(roomId);
			} catch (WebException) {
				MessageBox.Show("Cavetubeに接続できませんでした。");
			}
		}

		/// <summary>
		/// コメント部屋から抜けます。
		/// </summary>
		private void LeaveRoom() {
			if (String.IsNullOrEmpty(this.cavetubeClient.JoinedRoomId) == false) {
				this.cavetubeClient.LeaveRoom();

				base.OnPropertyChanged("RoomJoinStatus");
				this.ResetStatus();
			}
		}

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="postName"></param>
		/// <param name="postMessage"></param>
		/// <param name="apiKey"></param>
		private void PostComment(String postName, String postMessage, String apiKey) {
			if (String.IsNullOrEmpty(this.cavetubeClient.JoinedRoomId)) {
				return;
			}

			this.cavetubeClient.PostComment(postName, postMessage, apiKey);
			this.PostMessage = String.Empty;
		}

		#region 読み上げソフト

		/// <summary>
		/// 読み上げソフトに接続します。
		/// </summary>
		private void ConnectReadingApplication() {
			this.DisconnectReadingApplication();

			switch ((ReadingApplicationEnum)Settings.Default.ReadingApplication) {
				case ReadingApplicationEnum.Softalk:
					try {
						this.readingClient = new SofTalkClient(Settings.Default.SofTalkPath);
						this.ReadingApplicationStatus = true;
					} catch (FileNotFoundException) {
						MessageBox.Show("SofTalkに接続できませんでした。\nオプションからSofTalkの正しいパスを指定してください。");
						this.ReadingApplicationStatus = false;
					}
					break;
				default:
					this.readingClient = new BouyomiClientWrapper();
					if (this.readingClient.IsConnect) {
						this.ReadingApplicationStatus = true;
					} else {
						MessageBox.Show("棒読みちゃんに接続できませんでした。\n後から棒読みちゃんを起動する場合は、リボンの棒読みアイコンを押してください。");
						this.ReadingApplicationStatus = false;
					}
					break;
			}
		}

		/// <summary>
		/// 読み上げソフトから切断します。
		/// </summary>
		private void DisconnectReadingApplication() {
			if (this.readingClient != null) {
				this.readingClient.Dispose();
				this.ReadingApplicationStatus = false;
			}
		}

		#endregion

		#region Logging関係

		private void LoginCavetube() {
			var loginBox = new LoginBox();
			var viewModel = new LoginBoxViewModel(cavetubeClient);
			viewModel.OnClose += () => {
				loginBox.Close();
			};
			loginBox.DataContext = viewModel;
			loginBox.ShowDialog();
			base.OnPropertyChanged("LoginStatus");
			this.PostName = CaveTalk.Properties.Settings.Default.UserId;
		}

		private void LogoutCavetube() {
			var apiKey = CaveTalk.Properties.Settings.Default.ApiKey;
			if (String.IsNullOrWhiteSpace(apiKey)) {
				return;
			}

			var userId = CaveTalk.Properties.Settings.Default.UserId;
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ConfigurationErrorsException("UserIdが登録されていません。");
			}

			var password = CaveTalk.Properties.Settings.Default.Password;
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ConfigurationErrorsException("Passwordが登録されていません。");
			}

			var devKey = ConfigurationManager.AppSettings["dev_key"];
			if (String.IsNullOrWhiteSpace(devKey)) {
				throw new ConfigurationErrorsException("[dev_key]が設定されていません。");
			}
			var isSuccess = cavetubeClient.Logout(userId, password, devKey);
			if (isSuccess) {
				CaveTalk.Properties.Settings.Default.Reset();
				base.OnPropertyChanged("LoginStatus");
			}
		}

		#endregion

		#region バージョン情報

		private void ShowVersion() {
			new AboutBox().ShowDialog();
		}

		#endregion

		#region オプション

		private void ShowOption() {
			var option = new OptionWindow();
			var viewModel = new OptionWindowViewModel();
			viewModel.OnClose += () => option.Close();
			option.DataContext = viewModel;
			option.ShowDialog();

			this.ConnectReadingApplication();
		}

		#endregion

		private String ParseUrl(String url) {
			var pattern = String.Format(@"({0}(?:\:\d{{1,5}})?/[a-z]+/)?([0-9A-Z]{{32}})", ConfigurationManager.AppSettings["web_server"]);
			var match = Regex.Match(url, pattern);
			if (match.Success) {
				return match.Groups[2].Value;
			} else {
				return String.Empty;
			}
		}

		/// <summary>
		/// 電源状態の変更時にコメントサーバへの接続/切断を行います。
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnPowerModeChanged(Object sender, PowerModeChangedEventArgs e) {
			switch (e.Mode) {
				case PowerModes.Resume:
					this.cavetubeClient.Connect();
					break;
				case PowerModes.Suspend:
					this.cavetubeClient.Close();
					break;
			}
		}
	}

	public sealed class Message : CaveTubeClient.Message {
		private Logger logger = LogManager.GetCurrentClassLogger();

		public ICommand CopyCommentCommand { get; private set; }

		public Message(CaveTubeClient.Message message)
			: base(message.Number, message.Name, message.Comment, message.Time, message.Auth, message.IsBan) {
			this.CopyCommentCommand = new RelayCommand(p => {
				try {
					Clipboard.SetText(this.Comment);
				} catch (ArgumentException e) {
					logger.Error("コメントがnullのためクリップボードにコピーできませんでした。", e);
				}
			});
		}
	}
}