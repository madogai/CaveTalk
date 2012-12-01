namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Configuration;
	using System.Linq;
	using System.Media;
	using System.Net;
	using System.Runtime.InteropServices;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Threading;
	using CaveTube.CaveTalk.Lib;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTalk.View;
	using CaveTube.CaveTubeClient;
	using NLog;

	public sealed class MainWindowViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private ACommentClient commentClient;
		private Dispatcher uiDispatcher;
		private Model.Config config;

		private ASpeechClient speechClient;

		public event Action<Lib.Message, Model.Config> OnMessage;

		#region プロパティ

		private Int32 messageIndex;

		public Int32 MessageIndex {
			get { return this.messageIndex; }
			set {
				this.messageIndex = value;
				base.OnPropertyChanged("MessageIndex");
			}
		}

		public ObservableCollection<Message> MessageList { get; private set; }

		private String liveUrl;

		public String LiveUrl {
			get {
				if (this.LoginStatus == true && String.IsNullOrWhiteSpace(this.liveUrl)) {
					return String.Format("{0}/live/{1}", ConfigurationManager.AppSettings["web_server"], this.config.UserId);
				} else {
					return this.liveUrl;
				}
			}
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

		public Boolean SpeakApplicationStatus {
			get { return this.speechClient != null && this.speechClient.IsConnect; }
		}

		public Boolean ConnectingStatus {
			get {
				return this.commentClient.IsConnect;
			}
		}

		public Boolean RoomJoinStatus {
			get {
				if (this.commentClient == null) {
					return false;
				}

				if (this.commentClient.JoinedRoomSummary == null) {
					return false;
				}

				if (String.IsNullOrEmpty(this.commentClient.JoinedRoomSummary.RoomId)) {
					return false;
				}

				return true;
			}
		}

		public Boolean LoginStatus {
			get {
				return String.IsNullOrWhiteSpace(this.config.ApiKey) == false;
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

		public Int32 FontSize {
			get { return this.config.FontSize; }
		}

		public Boolean TopMost {
			get { return this.config.TopMost; }
		}

		public Double WindowTop {
			get {
				var top = this.config.WindowTop;
				top = Math.Max(top, SystemParameters.VirtualScreenTop);
				top = Math.Min(top, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight);
				return top;
			}
			set {
				this.config.WindowTop = value;
				base.OnPropertyChanged("WindowTop");
			}
		}

		public Double WindowLeft {
			get {
				var left = this.config.WindowLeft;
				left = Math.Max(left, SystemParameters.VirtualScreenLeft);
				left = Math.Min(left, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth);
				return left;
			}
			set {
				this.config.WindowLeft = value;
				base.OnPropertyChanged("WindowLeft");
			}
		}

		public Double WindowHeight {
			get { return this.config.WindowHeight; }
			set {
				this.config.WindowHeight = value;
				base.OnPropertyChanged("WindowHeight");
			}
		}

		public Double WindowWidth {
			get {
				return this.config.WindowWidth;
			}
			set {
				this.config.WindowWidth = value;
				base.OnPropertyChanged("WindowWidth");
			}
		}

		public Boolean DisplayIdColumn {
			get { return this.config.DisplayIdColumn; }
			set {
				this.config.DisplayIdColumn = value;
				base.OnPropertyChanged("DisplayIdColumn");
			}
		}

		public Boolean DisplayPostTimeColumn {
			get { return this.config.DisplayPostTimeColumn; }
			set {
				this.config.DisplayPostTimeColumn = value;
				base.OnPropertyChanged("DisplayPostTimeColumn");
			}
		}

		public Boolean DisplayElapsedPostTimeColumn {
			get { return this.config.DisplayElapsedPostTimeColumn; }
			set {
				this.config.DisplayElapsedPostTimeColumn = value;
				base.OnPropertyChanged("DisplayElapsedPostTimeColumn");
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
		public ICommand ConnectSpeakApplicationCommand { get; private set; }

		/// <summary>
		/// 読み上げソフトのコネクションを切断します。
		/// </summary>
		public ICommand DisconnectSpeakApplicationCommand { get; private set; }

		/// <summary>
		/// AboutBoxを表示します。
		/// </summary>
		public ICommand AboutBoxCommand { get; private set; }

		/// <summary>
		/// オプション画面を表示します。
		/// </summary>
		public ICommand SettingWindowCommand { get; private set; }

		/// <summary>
		/// 配信開始画面を表示します。
		/// </summary>
		public ICommand StartBroadcastWindowCommand { get; private set; }
		#endregion

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public MainWindowViewModel() {
			this.MessageList = new ObservableCollection<Message>();
			BindingOperations.EnableCollectionSynchronization(this.MessageList, new Object());
			this.uiDispatcher = Dispatcher.CurrentDispatcher;

			// 設定データの取得
			this.config = Config.GetConfig();

			#region Commandの登録

			this.ConnectSpeakApplicationCommand = new RelayCommand(p => this.ConnectSpeakApplication());
			this.DisconnectSpeakApplicationCommand = new RelayCommand(p => this.DisconnectSpeakApplication());
			this.JoinRoomCommand = new RelayCommand(p => this.JoinRoom(this.LiveUrl));
			this.LeaveRoomCommand = new RelayCommand(p => this.LeaveRoom());
			this.LoginCommand = new RelayCommand(p => this.LoginCavetube());
			this.LogoutCommand = new RelayCommand(p => this.LogoutCavetube());
			this.SwitchAccountCommand = new RelayCommand(p => {
				this.LogoutCavetube();
				this.LoginCavetube();
			});
			this.PostCommentCommand = new RelayCommand(p => {
				var apiKey = this.config.ApiKey ?? String.Empty;
				this.PostComment(this.PostName, this.PostMessage, apiKey);
			});
			this.AboutBoxCommand = new RelayCommand(p => this.ShowVersion());
			this.SettingWindowCommand = new RelayCommand(p => this.ShowOption());
			this.StartBroadcastWindowCommand = new RelayCommand(p => this.ShowStartBroadcast());

			#endregion

			this.PostName = this.config.UserId;
		}

		/// <summary>
		/// 初期化を行います。
		/// </summary>
		public void Initialize() {
			// バージョンチェック
			this.UpdateCheck();

			// 読み上げソフトとの接続
			this.ConnectSpeakApplication();
		}

		/// <summary>
		/// 画面が閉じるときに呼ばれます。
		/// オブジェクトを破棄します。
		/// </summary>
		protected override void OnDispose() {
			base.OnDispose();

			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			if (this.speechClient != null) {
				this.speechClient.Dispose();
			}

			this.config.Save();
		}

		/// <summary>
		/// メッセージをDBに保存します。
		/// </summary>
		/// <param name="messages"></param>
		private void SaveMessage(IEnumerable<Model.Message> messages) {
			//var dbMessages = this.context.Messages.Where(message => message.Room.RoomId == room.RoomId);

			//messages.Where(m => dbMessages.All(dm => dm.Number != m.Number && dm.PostTime != m.PostTime)).ForEach(m => {
			//    this.context.Messages.Add(m);
			//});

			//this.context.SaveChanges();
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

			// コメントクライアントを生成します。
			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			this.commentClient = ACommentClient.CreateInstance(liveUrl);
			if (this.commentClient == null) {
				Mouse.OverrideCursor = null;
				MessageBox.Show("不正なURLです。");
				return;
			}

			try {
				this.commentClient.OnJoin += this.OnJoin;
				this.commentClient.OnNewMessage += this.OnReceiveMessage;
				this.commentClient.OnUpdateMember += this.OnUpdateMember;
				this.commentClient.OnBan += this.OnBanUser;
				this.commentClient.OnUnBan += this.OnUnBanUser;
				this.commentClient.OnAdminShout += this.OnAdminShout;
				this.commentClient.OnNotifyLiveClose += this.OnNotifyLiveClose;
				this.commentClient.OnError += this.OnError;
				this.commentClient.Connect();

				var room = this.commentClient.GetRoom(liveUrl);
				if (room == null) {
					Mouse.OverrideCursor = null;
					MessageBox.Show("接続に失敗しました。");
					return;
				}

				var roomId = room.Summary.RoomId;
				if (String.IsNullOrWhiteSpace(roomId)) {
					Mouse.OverrideCursor = null;
					MessageBox.Show("接続に失敗しました。");
					return;
				}

				// URLを部屋名に更新します。
				this.LiveUrl = roomId;

				room.Messages.ForEach(m => {
					var message = new Message(room.Summary, m);
					message.OnBanUser += this.BanUser;
					message.OnUnBanUser += this.UnBanUser;
					message.OnShowId += this.ShowId;
					message.OnHideId += this.HideId;
					this.MessageList.Insert(0, message);
				});

				this.commentClient.JoinRoom(roomId);
			} catch (CommentException e) {
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

			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			if (String.IsNullOrEmpty(this.commentClient.JoinedRoomSummary.RoomId)) {
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
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			if (String.IsNullOrEmpty(this.commentClient.JoinedRoomSummary.RoomId)) {
				return;
			}

			this.commentClient.PostComment(postName, postMessage, apiKey);
			this.PostMessage = String.Empty;
		}

		/// <summary>
		/// ユーザーをBANします。
		/// </summary>
		/// <param name="message"></param>
		private void BanUser(Message message) {
			if (this.LoginStatus == false) {
				MessageBox.Show("BANするにはログインが必須です。");
				return;
			}

			if (this.commentClient.JoinedRoomSummary == null) {
				MessageBox.Show("部屋に所属していません。");
				return;
			}

			if (this.config.UserId != this.commentClient.JoinedRoomSummary.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				this.commentClient.BanListener(message.Number, this.config.ApiKey);
			} catch (ArgumentException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			} catch (CavetubeException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			}
		}

		/// <summary>
		/// ユーザーBANを解除します。
		/// </summary>
		/// <param name="message"></param>
		private void UnBanUser(Message message) {
			if (this.LoginStatus == false) {
				MessageBox.Show("BANするにはログインが必須です。");
				return;
			}

			if (this.commentClient.JoinedRoomSummary == null) {
				MessageBox.Show("部屋に所属していません。");
				return;
			}

			if (this.config.UserId != this.commentClient.JoinedRoomSummary.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				this.commentClient.UnBanListener(message.Number, this.config.ApiKey);
			} catch (ArgumentException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			} catch (CavetubeException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			}
		}

		/// <summary>
		/// リスナーの強制ID表示を有効にします。
		/// </summary>
		/// <param name="message"></param>
		private void ShowId(Message message) {
			if (this.LoginStatus == false) {
				MessageBox.Show("ID表示指定するにはログインが必須です。");
				return;
			}

			if (this.commentClient.JoinedRoomSummary == null) {
				MessageBox.Show("部屋に所属していません。");
				return;
			}

			if (this.config.UserId != this.commentClient.JoinedRoomSummary.Author) {
				MessageBox.Show("配信者でないとID表示指定することはできません。");
				return;
			}

			try {
				this.commentClient.ShowId(message.Number, this.config.ApiKey);
			} catch (ArgumentException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			} catch (CavetubeException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			}
		}

		/// <summary>
		/// リスナーの強制ID表示を解除します。
		/// </summary>
		/// <param name="message"></param>
		private void HideId(Message message) {
			if (this.LoginStatus == false) {
				MessageBox.Show("ID表示解除するにはログインが必須です。");
				return;
			}

			if (this.commentClient.JoinedRoomSummary == null) {
				MessageBox.Show("部屋に所属していません。");
				return;
			}

			if (this.config.UserId != this.commentClient.JoinedRoomSummary.Author) {
				MessageBox.Show("配信者でないとID表示解除することはできません。");
				return;
			}

			try {
				this.commentClient.HideId(message.Number, this.config.ApiKey);
			} catch (ArgumentException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			} catch (CavetubeException ex) {
				MessageBox.Show(ex.Message);
				logger.Error(ex);
			}
		}

		/// <summary>
		/// 読み上げソフトに接続します。
		/// </summary>
		private void ConnectSpeakApplication() {
			if (this.speechClient == null) {
				this.speechClient = ASpeechClient.CreateInstance();
			}

			try {
				var isConnect = this.speechClient.Connect();
				if (isConnect == false) {
					throw new ConnectionException("読み上げソフトに接続できませんでした。後から読み上げソフトを立ち上げた場合は、メニューの読み上げアイコンから読み上げソフトに接続を選択してください。");
				}
			} catch (ConnectionException e) {
				MessageBox.Show(e.Message);
			}

			base.OnPropertyChanged("SpeakApplicationStatus");
		}

		/// <summary>
		/// 読み上げソフトを切断します。
		/// </summary>
		private void DisconnectSpeakApplication() {
			this.speechClient.Disconnect();
			base.OnPropertyChanged("SpeakApplicationStatus");
		}

		/// <summary>
		/// アプリケーションのアップデートをチェックします。
		/// </summary>
		private void UpdateCheck() {
			using (var client = new WebClient()) {
				try {
					client.DownloadStringCompleted += (e, sender) => {
						var result = sender.Result as String;
						if (String.IsNullOrEmpty(result)) {
							return;
						}
						var serverVersion = DateTime.Parse(result);
						var localVersion = DateTime.Parse(ConfigurationManager.AppSettings["version"]);
						if (serverVersion > localVersion) {
							new NotifyUpdateBox().ShowDialog();
						}
					};
					client.DownloadStringAsync(new Uri(ConfigurationManager.AppSettings["version_check_url"]));
				} catch (Exception ex) {
					logger.Warn(ex);
				}
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
		/// コメント受信時に実行されるイベントです。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="message"></param>
		private void OnReceiveMessage(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			// コメントを追加
			var newMessage = new Message(this.commentClient.JoinedRoomSummary, message);
			newMessage.OnBanUser += this.BanUser;
			newMessage.OnUnBanUser += this.UnBanUser;
			newMessage.OnShowId += this.ShowId;
			newMessage.OnHideId += this.HideId;
			this.MessageList.Insert(0, newMessage);

			// コメントの読み上げ
			var isConnect = this.speechClient != null || this.speechClient.IsConnect == false;
			if (this.SpeakApplicationStatus && isConnect) {
				var speechResult = this.speechClient.Speak(message);
				if (speechResult == false) {
					base.OnPropertyChanged("SpeakApplicationStatus");
					MessageBox.Show("読み上げに失敗しました。");
				}
			}

			// コードビハインドのイベントを実行
			if (this.OnMessage != null) {
				uiDispatcher.BeginInvoke(new Action(() => {
					this.OnMessage(message, this.config);
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
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			var newMessage = new Message(this.commentClient.JoinedRoomSummary, message);
			newMessage.OnBanUser += this.BanUser;
			newMessage.OnUnBanUser += this.UnBanUser;
			newMessage.OnShowId += this.ShowId;
			newMessage.OnHideId += this.HideId;

			var index = this.MessageList.IndexOf(newMessage);
			if (index < 0) {
				return;
			}

			this.MessageList[index] = newMessage;
		}

		/// <summary>
		/// BAN解除通知時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void OnUnBanUser(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			var newMessage = new Message(this.commentClient.JoinedRoomSummary, message);
			newMessage.OnBanUser += this.BanUser;
			newMessage.OnUnBanUser += this.UnBanUser;
			newMessage.OnShowId += this.ShowId;
			newMessage.OnHideId += this.HideId;
			var index = this.MessageList.IndexOf(newMessage);
			if (index < 0) {
				return;
			}

			this.MessageList[index] = newMessage;
		}

		/// <summary>
		/// 管理者メッセージ通知時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void OnAdminShout(String message) {
			SystemSounds.Asterisk.Play();
			MessageBox.Show(message, "管理者メッセージ", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
		}

		/// <summary>
		/// 配信終了通知時に実行されるイベントです。
		/// </summary>
		/// <param name="liveEntry"></param>
		private void OnNotifyLiveClose(Lib.LiveNotification liveEntry) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			if (liveEntry.RoomId != this.commentClient.JoinedRoomSummary.RoomId) {
				return;
			}

			var isConnect = this.speechClient != null || this.speechClient.IsConnect == false;
			if ((this.config.NoticeLiveClose && this.SpeakApplicationStatus && isConnect && this.config.NoticeLiveClose) == false) {
				return;
			}

			MessageBox.Show("配信が終了しました。");
		}

		/// <summary>
		/// CaveTubeClientから何かしらのエラーが通知されたときに実行されるイベントです。
		/// </summary>
		/// <param name="e"></param>
		private void OnError(Exception e) {
			logger.Error(e);
		}

		#endregion

		#region Logging関係

		private void LoginCavetube() {
			var loginBox = new LoginBox();
			var viewModel = new LoginBoxViewModel();
			viewModel.OnClose += () => {
				loginBox.Close();
			};
			loginBox.DataContext = viewModel;
			loginBox.ShowDialog();

			base.OnPropertyChanged("LoginStatus");
			this.PostName = this.config.UserId;
		}

		private void LogoutCavetube() {
			var apiKey = this.config.ApiKey;
			if (String.IsNullOrWhiteSpace(apiKey)) {
				return;
			}

			var userId = this.config.UserId;
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ConfigurationErrorsException("UserIdが登録されていません。");
			}

			var password = this.config.Password;
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ConfigurationErrorsException("Passwordが登録されていません。");
			}

			var devKey = ConfigurationManager.AppSettings["dev_key"];
			if (String.IsNullOrWhiteSpace(devKey)) {
				throw new ConfigurationErrorsException("[dev_key]が設定されていません。");
			}
			try {
				var isSuccess = CavetubeAuth.Logout(userId, password, devKey);
				if (isSuccess) {
					this.config.ApiKey = String.Empty;
					this.config.UserId = String.Empty;
					this.config.Password = String.Empty;
					this.config.Save();

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

			if (this.speechClient != null) {
				this.speechClient.Dispose();
			}
			this.speechClient = ASpeechClient.CreateInstance();
			this.ConnectSpeakApplication();
			base.OnPropertyChanged("FontSize");
			base.OnPropertyChanged("TopMost");
		}

		#endregion

		private void ShowStartBroadcast() {
			var window = new StartBroadcast();
			var viewModel = new StartBroadcastViewModel();
			viewModel.OnClose += roomId => {
				uiDispatcher.BeginInvoke(new Action(() => {
					window.Close();
					if (String.IsNullOrWhiteSpace(roomId)) {
						return;
					}
					this.JoinRoom(roomId);
				}));
			};
			window.DataContext = viewModel;
			window.ShowDialog();
		}
	}

	public sealed class Message : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();
		private Lib.Message message;
		private Lib.Summary summary;

		public Int32 Number {
			get { return this.message.Number; }
		}

		public String ListenerId {
			get {
				if (this.message.ListenerId == null) {
					return null;
				}
				return this.message.ListenerId;
			}
		}

		public String Name {
			get { return this.message.Name; }
		}

		public String Comment {
			get { return this.message.Comment; }
		}

		public DateTime PostTime {
			get { return this.message.PostTime; }
		}

		public TimeSpan ElapsedPostTime {
			get {
				return this.PostTime - this.summary.StartTime;
			}
		}

		public Boolean IsAuth {
			get { return this.message.IsAuth; }
		}

		public Boolean IsBan {
			get { return this.message.IsBan; }
		}

		public Boolean IsAsciiArt {
			get {
				return this.message.IsAsciiArt;
			}
		}

		public Brush Color {
			get {
				if (this.message.IsAuth == false && String.IsNullOrWhiteSpace(this.message.ListenerId)) {
					return new SolidColorBrush(Colors.White);
				}

				if (this.message.IsAuth) {
					var account = Model.Account.GetAccount(this.Name);
					if (account == null || String.IsNullOrWhiteSpace(account.Color)) {
						return new SolidColorBrush(Colors.White);
					}
					return (Brush)new BrushConverter().ConvertFrom(account.Color);
				}

				var listener = Model.Listener.GetListener(this.message.ListenerId);
				if (listener == null || String.IsNullOrWhiteSpace(listener.Color)) {
					return new SolidColorBrush(Colors.White);
				}
				return (Brush)new BrushConverter().ConvertFrom(listener.Color);
			}
			private set {
				var color = value.ToString();

				if (this.IsAuth) {
					var account = Model.Account.GetAccount(this.Name);
					if (account == null) {
						return;
					}

					// 全コメントの色を変更します。
					account.Color = color;
					Model.Account.UpdateAccount(account);

					var listeners = account.Listeners.Select(l => {
						l.Color = color;
						return l;
					});
					Model.Listener.UpdateListener(listeners);
					base.OnPropertyChanged("Color");
				} else if (String.IsNullOrWhiteSpace(this.message.ListenerId) == false) {
					var listener = Model.Listener.GetListener(this.message.ListenerId);
					if (listener == null) {
						return;
					}

					listener.Color = color;
					Model.Listener.UpdateListener(listener);

					var account = listener.Account;
					if (account == null) {
						base.OnPropertyChanged("Color");
						return;
					}

					account.Color = color;
					Model.Account.UpdateAccount(account);
					base.OnPropertyChanged("Color");
				}
			}
		}

		public event Action<Message> OnBanUser;
		public event Action<Message> OnUnBanUser;
		public event Action<Message> OnMarkListener;
		public event Action<Message> OnShowId;
		public event Action<Message> OnHideId;

		public ICommand CopyCommentCommand { get; private set; }
		public ICommand BanUserCommand { get; private set; }
		public ICommand UnBanUserCommand { get; private set; }
		public ICommand ShowIdCommand { get; private set; }
		public ICommand HideIdCommand { get; private set; }
		public ICommand MarkCommand { get; private set; }

		public Message(Lib.Summary summary, Lib.Message message) {
			this.summary = summary;
			this.message = message;

			this.CopyCommentCommand = new RelayCommand(p => {
				if (String.IsNullOrEmpty(this.Comment)) {
					return;
				}

				for (var i = 0; i < 3; i++) {
					try {
						Clipboard.SetText(this.Comment);
					} catch (ExternalException) {
						System.Threading.Thread.Sleep(0);
					}
				}
			});

			this.BanUserCommand = new RelayCommand(p => {
				if (this.OnBanUser != null) {
					this.OnBanUser(this);
				}
			});
			this.UnBanUserCommand = new RelayCommand(p => {
				if (this.OnUnBanUser != null) {
					this.OnUnBanUser(this);
				}
			});
			this.ShowIdCommand = new RelayCommand(p => {
				if (this.OnShowId != null) {
					this.OnShowId(this);
				}
			});
			this.HideIdCommand = new RelayCommand(p => {
				if (this.OnHideId != null) {
					this.OnHideId(this);
				}
			});

			this.MarkCommand = new RelayCommand(p => {
				if (this.IsAuth == false && String.IsNullOrWhiteSpace(this.ListenerId)) {
					return;
				}

				var solidBrush = this.Color as SolidColorBrush;

				if (solidBrush.Color != Colors.White) {
					this.Color = Brushes.White;
				} else {
					var random = new Random();
					// 暗い色だと文字が見えなくなるので、96以上とします。
					var red = (byte)random.Next(96, 255);
					var green = (byte)random.Next(96, 255);
					var blue = (byte)random.Next(96, 255);
					this.Color = new SolidColorBrush(System.Windows.Media.Color.FromRgb(red, green, blue));
				}

				if (this.OnMarkListener != null) {
					this.OnMarkListener(this);
				}
			});
		}

		public override bool Equals(object obj) {
			var other = obj as Message;
			if (other == null) {
				return false;
			}

			var isNumberSame = this.Number == other.Number;
			var isNameSame = this.Name == other.Name;
			var isCommentSame = this.Comment == other.Comment;
			return isNumberSame && isNameSame && isCommentSame;
		}

		public override int GetHashCode() {
			return this.Number.GetHashCode() ^ this.Name.GetHashCode() ^ this.Comment.GetHashCode();
		}
	}
}