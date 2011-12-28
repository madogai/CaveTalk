namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Runtime.InteropServices;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Threading;
	using CaveTube.CaveTalk.Lib;
	using CaveTube.CaveTalk.Properties;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTalk.View;
	using CaveTube.CaveTubeClient;
	using Microsoft.Win32;
	using NLog;

	public sealed class MainWindowViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		internal CavetubeClient cavetubeClient;
		private ICommentClient commentClient;
		private IReadingApplicationClient readingClient;
		private Dispatcher uiDispatcher;
		private Lib.Summary summary;

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

		public SafeObservable<Message> MessageList { get; private set; }

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
				if (this.commentClient == null) {
					return false;
				}

				return String.IsNullOrEmpty(this.commentClient.RoomId) == false;
			}
		}

		public Boolean LoginStatus {
			get {
				return String.IsNullOrWhiteSpace(Settings.Default.ApiKey) == false;
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
				var apiKey = Settings.Default.ApiKey ?? String.Empty;
				this.PostComment(this.PostName, this.PostMessage, apiKey);
			});
			this.AboutBoxCommand = new RelayCommand(p => this.ShowVersion());
			this.SettingWindowCommand = new RelayCommand(p => this.ShowOption());

			#endregion

			this.PostName = Settings.Default.UserId;

			SystemEvents.PowerModeChanged += this.OnPowerModeChanged;
		}

		public void Initialize() {
			#region バージョン確認

			//using (var client = new WebClient()) {
			//    var version = client.DownloadString(ConfigurationManager.AppSettings["version_check_url"]);
			//    var fileInfo = FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]);
			//}

			#endregion

			#region CavetubeClientの接続

			this.cavetubeClient = new CavetubeClient(new Uri(ConfigurationManager.AppSettings["comment_server"]), new Uri(ConfigurationManager.AppSettings["web_server"]));
			this.cavetubeClient.OnNotifyLive += this.OnLiveNotification;
			this.cavetubeClient.OnClose += this.OnClose;

			try {
				this.cavetubeClient.Connect();
			} catch (WebException e) {
				logger.Error(e);
				MessageBox.Show("CaveTubeに接続できません。");
			}

			#endregion

			#region 読み上げソフト

			this.ConnectReadingApplication();

			#endregion

		}

		/// <summary>
		/// デストラクタ
		/// </summary>
		~MainWindowViewModel() {
			SystemEvents.PowerModeChanged -= this.OnPowerModeChanged;
		}

		/// <summary>
		/// 画面が閉じるときに呼ばれます。
		/// オブジェクトを破棄します。
		/// </summary>
		protected override void OnDispose() {
			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			if (this.cavetubeClient != null) {
				this.cavetubeClient.Dispose();
			}

			if (this.readingClient != null) {
				this.readingClient.Dispose();
			}
		}

		/// <summary>
		/// コメントを追加します。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="message"></param>
		private void AddMessage(Message message) {
			this.MessageList.Insert(0, message);

			if (this.ReadingApplicationStatus) {
				var isSpeech = this.SpeechComment(message);
				if (isSpeech == false) {
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
		private void AddMessage(IEnumerable<Message> messages) {
			base.OnPropertyChanged("ConnectingStatus");
			foreach (var message in messages) {
				this.MessageList.Insert(0, message);
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
			if (String.IsNullOrWhiteSpace(liveUrl)) {
				return;
			}

			Mouse.OverrideCursor = Cursors.Wait;

			this.LeaveRoom();

			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			var urlType = this.JudgeUrl(liveUrl);
			switch (urlType) {
				case UrlType.Cavetube:
					this.commentClient = new CaveTubeClientWrapper(this.cavetubeClient);
					break;
				case UrlType.Jbbs:
					this.commentClient = null;
					//var match = Regex.Match(liveUrl, @"^http://jbbs.livedoor.jp/bbs/read.cgi/([a-z]+/\d+/\d+)");
					//if (match.Success == false) {
					//    return;
					//}

					//this.LiveUrl = match.Groups[1].Value;
					break;
				default:
					this.commentClient = null;
					break;
			}

			if (this.commentClient == null) {
				Mouse.OverrideCursor = null;
				MessageBox.Show("不正なURLです。");
				Mouse.OverrideCursor = null;
				return;
			}

			try {
				var room = this.commentClient.GetRoomInfo(liveUrl);
				var roomId = room.Summary.RoomId;
				if (String.IsNullOrWhiteSpace(roomId)) {
					Mouse.OverrideCursor = null;
					MessageBox.Show("不正なURLです。");
					Mouse.OverrideCursor = null;
					return;
				}

				this.commentClient.OnJoin += this.OnJoin;
				this.commentClient.OnMessage += this.OnReceiveMessage;
				this.commentClient.OnUpdateMember += this.OnUpdateMember;
				this.commentClient.OnBan += this.OnBanUser;
				this.commentClient.OnUnBan += this.OnUnBanUser;

				if (String.IsNullOrWhiteSpace(roomId) == false) {
					this.LiveUrl = roomId;
				}

				this.AddMessage(room.Messages.Select(m => {
					var message = new Message(m, this.BanUser, this.UnBanUser);
					return message;
				}));
				this.summary = room.Summary;

				try {
					this.commentClient.JoinRoom(roomId);
				} catch (WebException) {
					Mouse.OverrideCursor = null;
					MessageBox.Show("コメントサーバに接続できませんでした。");
					return;
				}

			} catch (CavetubeException e) {
				Mouse.OverrideCursor = null;
				MessageBox.Show(e.Message);
				logger.Error(e);
				return;
			}
		}

		/// <summary>
		/// コメント部屋から抜けます。
		/// </summary>
		private void LeaveRoom() {
			if (this.commentClient == null) {
				return;
			}

			var roomId = this.commentClient.RoomId;
			if (String.IsNullOrEmpty(this.commentClient.RoomId)) {
				return;
			}

			this.commentClient.LeaveRoom();

			base.OnPropertyChanged("RoomJoinStatus");
			this.ResetStatus();
		}

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="postName"></param>
		/// <param name="postMessage"></param>
		/// <param name="apiKey"></param>
		private void PostComment(String postName, String postMessage, String apiKey) {
			if (String.IsNullOrEmpty(this.commentClient.RoomId)) {
				return;
			}

			this.commentClient.PostComment(postName, postMessage, apiKey);
			this.PostMessage = String.Empty;
		}

		/// <summary>
		/// ユーザーをBANします。
		/// </summary>
		/// <param name="commentNum"></param>
		private void BanUser(Int32 commentNum) {
			if (this.LoginStatus == false) {
				MessageBox.Show("BANするにはログインが必須です。");
				return;
			}

			if (Settings.Default.UserId != this.summary.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				var isSuccess = this.commentClient.BanListener(commentNum, Settings.Default.ApiKey);
				if (isSuccess == false) {
					MessageBox.Show("BANに失敗しました。");
				}
			} catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BANできませんでした。");
			} catch (WebException) {
				logger.Error("CaveTubeとの通信に失敗しました。");
				MessageBox.Show("BANに失敗しました。");
			}
		}

		/// <summary>
		/// ユーザーBANを解除します。
		/// </summary>
		/// <param name="commentNum"></param>
		private void UnBanUser(Int32 commentNum) {
			if (this.LoginStatus == false) {
				MessageBox.Show("BANするにはログインが必須です。");
				return;
			}

			if (Settings.Default.UserId != this.summary.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				var isSuccess = this.commentClient.UnBanListener(commentNum, Settings.Default.ApiKey);
				if (isSuccess == false) {
					MessageBox.Show("BANに失敗しました。");
				}

			} catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BAN解除できませんでした。");
			} catch (WebException) {
				logger.Error("CaveTubeとの通信に失敗しました。");
				MessageBox.Show("BAN解除に失敗しました。");
			}
		}

		#region CommentClientに登録するイベント

		/// <summary>
		/// 放送への接続時に実行されるイベントです。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="messages"></param>
		private void OnJoin(String roomId) {
			base.OnPropertyChanged("RoomJoinStatus");

			uiDispatcher.BeginInvoke(new Action(() => {
				Mouse.OverrideCursor = null;
			}));
		}

		/// <summary>
		/// メッセージ受信時に実行されるイベントです。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="mes"></param>
		private void OnReceiveMessage(Lib.Message mes) {
			var message = new Message(mes, this.BanUser, this.UnBanUser);

			this.AddMessage(message);

			if (this.OnMessage != null) {
				uiDispatcher.BeginInvoke(new Action(() => {
					this.OnMessage(message);
				}));
			}
		}

		/// <summary>
		/// 人数更新受信時に実行されるイベントです。
		/// </summary>
		/// <param name="count"></param>
		private void OnUpdateMember(Int32 count) {
			this.Listener = count;
		}

		/// <summary>
		/// BAN通知受信時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void OnBanUser(Lib.Message message) {
			var oldComment = this.MessageList.FirstOrDefault(m => m.Number == message.Number);
			if (oldComment == null) {
				return;
			}

			var newMessage = new Message(message, this.BanUser, this.UnBanUser);
			var index = this.MessageList.IndexOf(oldComment);
			this.MessageList.RemoveAt(index);
			this.MessageList.Insert(index, newMessage);
		}

		/// <summary>
		/// BAN解除通知時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void OnUnBanUser(Lib.Message message) {
			var oldComment = this.MessageList.FirstOrDefault(m => m.Number == message.Number);
			if (oldComment == null) {
				return;
			}

			var newMessage = new Message(message, this.BanUser, this.UnBanUser);
			var index = this.MessageList.IndexOf(oldComment);
			this.MessageList.RemoveAt(index);
			this.MessageList.Insert(index, newMessage);
		}

		#endregion

		#region CavetubeClientに登録するイベント

		/// <summary>
		/// ライブ通知を受け取ったときに実行されるイベントです。
		/// </summary>
		/// <param name="liveInfo"></param>
		private void OnLiveNotification(LiveNotification liveInfo) {
			if (this.OnNotifyLive == null || (NotifyPopupStateEnum)Settings.Default.NotifyState == NotifyPopupStateEnum.False) {
				return;
			}
			uiDispatcher.BeginInvoke(new Action(() => {
				this.OnNotifyLive(liveInfo);
			}));
		}

		private void OnClose(Reason reason) {
			if (reason != Reason.Timeout) {
				return;
			}

			this.cavetubeClient.Connect();
			if (this.RoomJoinStatus) {
				this.JoinRoom(this.commentClient.RoomId);
			}
		}

		#endregion

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
						MessageBox.Show("SofTalkに接続できませんでした。\nオプションでSofTalk.exeの正しいパスを指定してください。");
						this.ReadingApplicationStatus = false;
					}
					break;
				default:
					this.readingClient = new BouyomiClientWrapper();
					if (this.readingClient.IsConnect) {
						this.ReadingApplicationStatus = true;
					} else {
						MessageBox.Show("棒読みちゃんに接続できませんでした。\n後から棒読みちゃんを起動した場合は、リボンの読み上げアイコンから読み上げソフトに接続を選択してください。");
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

		/// <summary>
		/// 読み上げソフトにコメントを渡します。
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private Boolean SpeechComment(Message message) {
			var comment = message.Comment;

			comment = message.IsAsciiArt ? "アスキーアート" : comment;

			comment = Regex.Replace(comment, @"https?://(?:[^.]+\.)?(?:images-)?amazon\.(?:com|ca|co\.uk|de|co\.jp|jp|fr|cn)(/.+)(?![\w\s!?&.\/\+:;#~%""=-]*>)", "アマゾンリンク");

			comment = comment.Replace("\n", " ");

			if (Settings.Default.ReadName && String.IsNullOrWhiteSpace(message.Name) == false) {
				comment = String.Format("{0}さん {1}", message.Name, comment);
			}

			if (Settings.Default.ReadNum) {
				comment = String.Format("コメント{0} {1}", message.Number, comment);
			}

			return this.readingClient.Add(comment);
		}

		#endregion

		#region Logging関係

		private void LoginCavetube() {
			var loginBox = new LoginBox();
			var viewModel = new LoginBoxViewModel(this.cavetubeClient);
			viewModel.OnClose += () => {
				loginBox.Close();
			};
			loginBox.DataContext = viewModel;
			loginBox.ShowDialog();
			base.OnPropertyChanged("LoginStatus");
			this.PostName = Settings.Default.UserId;
		}

		private void LogoutCavetube() {
			var apiKey = Settings.Default.ApiKey;
			if (String.IsNullOrWhiteSpace(apiKey)) {
				return;
			}

			var userId = Settings.Default.UserId;
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ConfigurationErrorsException("UserIdが登録されていません。");
			}

			var password = Settings.Default.Password;
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ConfigurationErrorsException("Passwordが登録されていません。");
			}

			var devKey = ConfigurationManager.AppSettings["dev_key"];
			if (String.IsNullOrWhiteSpace(devKey)) {
				throw new ConfigurationErrorsException("[dev_key]が設定されていません。");
			}
			try {
				var isSuccess = cavetubeClient.Logout(userId, password, devKey);
				if (isSuccess) {
					Settings.Default.Reset();
					base.OnPropertyChanged("LoginStatus");
				}
			} catch (WebException) {
				MessageBox.Show("ログアウトに失敗しました。");
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

		private UrlType JudgeUrl(String url) {
			var webServer = ConfigurationManager.AppSettings["web_server"];
			if (Regex.IsMatch(url, String.Format(@"^(?:{0}(?:\:\d{{1,5}})?/[a-z]+/)?([0-9A-Z]{{32}})", webServer))) {
				return UrlType.Cavetube;
			} else if (Regex.IsMatch(url, String.Format(@"^{0}(?:\:\d{{1,5}})?/live/(.*)", webServer))) {
				return UrlType.Cavetube;
			} else if (Regex.IsMatch(url, String.Format(@"^http://jbbs.livedoor.jp/bbs/read.cgi/[a-z0-9]+/\d+$"))) {
				return UrlType.Jbbs;
			} else {
				return UrlType.Unknown;
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

		private enum UrlType {
			Cavetube,
			Jbbs,
			Unknown,
		}
	}

	public sealed class Message : Lib.Message {
		private Logger logger = LogManager.GetCurrentClassLogger();

		public event Action<Int32> OnBanUser;
		public event Action<Int32> OnUnBanUser;

		public ICommand CopyCommentCommand { get; private set; }
		public ICommand BanUserCommand { get; private set; }
		public ICommand UnBanUserCommand { get; private set; }

		public Message(Lib.Message message, Action<Int32> OnBan, Action<Int32> OnUnBan) {
			this.Number = message.Number;
			this.Id = message.Id;
			this.Name = message.Name;
			this.Comment = message.Comment;
			this.Time = message.Time;
			this.Auth = message.Auth;
			this.IsBan = message.IsBan;

			this.OnBanUser += OnBan;
			this.OnUnBanUser += OnUnBan;

			this.CopyCommentCommand = new RelayCommand(p => {
				try {
					Clipboard.SetText(this.Comment);
				} catch (ExternalException e) {
					MessageBox.Show("クリップボードへのコピーに失敗しました。");
					logger.Error("クリップボードのコピーへの失敗しました。", e);
				} catch (ArgumentException e) {
					logger.Error("コメントがnullのためクリップボードにコピーできませんでした。", e);
				}
			});
			this.BanUserCommand = new RelayCommand(p => {
				if (this.OnBanUser != null) {
					this.OnBanUser(this.Number);
				}
			});
			this.UnBanUserCommand = new RelayCommand(p => {
				if (this.OnUnBanUser != null) {
					this.OnUnBanUser(this.Number);
				}
			});
		}
	}
}