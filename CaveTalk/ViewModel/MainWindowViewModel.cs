namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Media;
	using System.Net;
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
					return $"{ConfigurationManager.AppSettings["web_server"]}/live/{this.config.UserId}";
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

		public ListSortDirection? SortDirection {
			get {
				return this.config.SortDirection;
			}
			set {
				if (value == ListSortDirection.Ascending) {
					this.config.SortDirection = ListSortDirection.Ascending;
				} else {
					this.config.SortDirection = null;
				}
				base.OnPropertyChanged("SortDirection");
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

		public Boolean DisplayIconColumn {
			get { return this.config.DisplayIconColumn; }
			set {
				this.config.DisplayIconColumn = value;
				base.OnPropertyChanged("DisplayIconColumn");
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
		private async void JoinRoom(String liveUrl) {
			if (String.IsNullOrWhiteSpace(liveUrl)) {
				return;
			}

			Mouse.OverrideCursor = Cursors.Wait;

			this.LeaveRoom();

			// コメントクライアントを生成します。
			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			try {
				this.commentClient = await ACommentClient.CreateInstance(liveUrl);
			} catch(ArgumentException) {
				Mouse.OverrideCursor = null;
				MessageBox.Show("URLが正しくありません。");
				return;
			} catch(InvalidOperationException) {
				Mouse.OverrideCursor = null;
				MessageBox.Show("接続に失敗しました。");
				return;
			}

			try {
				this.commentClient.OnJoin += this.OnJoin;
				this.commentClient.OnNewMessage += this.AddComment;
				this.commentClient.OnNewMessage += this.SpeakMessage;
				this.commentClient.OnNewMessage += this.NotifyMessageToFlashCommentGenerator;
				this.commentClient.OnNewMessage += this.NotifyMessageToHtml5CommentGenerator;
				this.commentClient.OnUpdateMember += this.UpdateMember;
				this.commentClient.OnBan += this.CommentStatusChange;
				this.commentClient.OnUnBan += this.CommentStatusChange;
				this.commentClient.OnHideComment += this.CommentStatusChange;
				this.commentClient.OnShowComment += this.CommentStatusChange;
				this.commentClient.OnInstantMessage += this.NotifyInstantMessage;
				this.commentClient.OnAdminShout += this.NotifyAdminShout;
				this.commentClient.OnNotifyLiveClose += this.NotifyLiveClose;
				this.commentClient.OnError += this.LogError;
				this.commentClient.Connect();

				var room = await this.commentClient.GetRoomAsync(liveUrl);
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
					message.OnShowComment += this.ShowComment;
					message.OnHideComment += this.HideComment;
					message.OnAllowInstantMessage += this.AllowInstantMessage;
					if (this.SortDirection == ListSortDirection.Ascending) {
						this.MessageList.Add(message);
					} else {
						this.MessageList.Insert(0, message);
					}
				});

				await this.commentClient.JoinRoomGenAsync(roomId);
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
			SendBroadcasterCommandHelper(() => {
				this.commentClient.BanListener(message.Number, this.config.ApiKey);
			});
		}

		/// <summary>
		/// ユーザーBANを解除します。
		/// </summary>
		/// <param name="message"></param>
		private void UnBanUser(Message message) {
			SendBroadcasterCommandHelper(() => {
				this.commentClient.UnBanListener(message.Number, this.config.ApiKey);
			});
		}

		/// <summary>
		/// リスナーの強制ID表示を有効にします。
		/// </summary>
		/// <param name="message"></param>
		private void ShowId(Message message) {
			SendBroadcasterCommandHelper(() => {
				this.commentClient.ShowId(message.Number, this.config.ApiKey);
			});
		}

		/// <summary>
		/// リスナーの強制ID表示を解除します。
		/// </summary>
		/// <param name="message"></param>
		private void HideId(Message message) {
			SendBroadcasterCommandHelper(() => {
				this.commentClient.HideId(message.Number, this.config.ApiKey);
			});
		}

		/// <summary>
		/// コメントを再表示します。
		/// </summary>
		/// <param name="message"></param>
		private void ShowComment(Message message) {
			SendBroadcasterCommandHelper(() => {
				this.commentClient.ShowComment(message.Number, this.config.ApiKey);
			});
		}

		/// <summary>
		/// コメントを非表示にします。
		/// </summary>
		/// <param name="message"></param>
		private void HideComment(Message message) {
			SendBroadcasterCommandHelper(() => {
				this.commentClient.HideComment(message.Number, this.config.ApiKey);
			});
		}

		/// <summary>
		/// インスタントメッセージを許可します。
		/// </summary>
		/// <param name="message"></param>
		private void AllowInstantMessage(Message message) {
			SendBroadcasterCommandHelper(async () => {
				var result = await this.commentClient.AllowInstantMessageAsync(message.Number, this.config.ApiKey);
				if (result == false) {
					MessageBox.Show("インスタントメッセージの許可に失敗しました。");
				}
			});
		}

		/// <summary>
		/// 配信者用コマンドを送信するためのヘルパーです。
		/// </summary>
		/// <param name="act">実行するコマンド</param>
		private void SendBroadcasterCommandHelper(Action act) {
			try {
				if (this.LoginStatus == false) {
					throw new ArgumentException("ログインが必要です。");
				}

				if (this.commentClient.JoinedRoomSummary == null) {
					throw new ArgumentException("部屋に所属していません。");
				}

				if (this.config.UserId != this.commentClient.JoinedRoomSummary.Author) {
					throw new ArgumentException("配信者でないので動作を行うことができません。");
				}

				act();
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
		private async void UpdateCheck() {
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			using (var client = new WebClient()) {
				try {
					client.Headers.Add(HttpRequestHeader.UserAgent, "CaveChat");
					var result = await client.DownloadStringTaskAsync(new Uri(ConfigurationManager.AppSettings["version_check_url"]));
					if (String.IsNullOrEmpty(result)) {
						return;
					}
					var serverVersion = DateTime.Parse(result);
					var localVersion = DateTime.Parse(ConfigurationManager.AppSettings["version"]);
					if (serverVersion > localVersion) {
						new NotifyUpdateBox().ShowDialog();
					}
				} catch (WebException ex) {
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
		/// コメント受信時にコメントを追加します。
		/// </summary>
		/// <param name="message"></param>
		private void AddComment(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			// コメントを追加
			var newMessage = new Message(this.commentClient.JoinedRoomSummary, message);
			newMessage.OnBanUser += this.BanUser;
			newMessage.OnUnBanUser += this.UnBanUser;
			newMessage.OnShowId += this.ShowId;
			newMessage.OnHideId += this.HideId;
			newMessage.OnShowComment += this.ShowComment;
			newMessage.OnHideComment += this.HideComment;
			newMessage.OnAllowInstantMessage += this.AllowInstantMessage;
			if (this.SortDirection == ListSortDirection.Ascending) {
				this.MessageList.Add(newMessage);
			} else {
				this.MessageList.Insert(0, newMessage);
			}

			// コードビハインドのイベントを実行
			if (this.OnMessage != null) {
				uiDispatcher.BeginInvoke(new Action(() => {
					this.OnMessage(message, this.config);
				}));
			}
		}

		/// <summary>
		/// コメント受信時に読み上げを実行します。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="message"></param>
		private void SpeakMessage(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			// コメントの読み上げ
			var isConnect = this.speechClient != null || this.speechClient.IsConnect == false;
			if ((this.SpeakApplicationStatus && isConnect) == false) {
				return;
			}

			var speechResult = this.speechClient.Speak(message);
			if (speechResult == false) {
				base.OnPropertyChanged("SpeakApplicationStatus");
				MessageBox.Show("読み上げに失敗しました。");
			}
		}

		/// <summary>
		/// コメント受信時にFlashコメントジェネレーターへの通知を行います。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="message"></param>
		private void NotifyMessageToFlashCommentGenerator(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			if (this.config.EnableFlashCommentGenerator == false) {
				return;
			}

			var filePath = this.config.FlashCommentGeneratorDatFilePath;
			if (Path.IsPathRooted(filePath) == false) {
				return;
			}

			if (String.IsNullOrEmpty(Path.GetFileName(filePath))) {
				return;
			}

			try {
				FlashCommentGeneratorNotifier.write(filePath, message);
			} catch (IOException e) {
				logger.Error(e);
			}
		}

		/// <summary>
		/// コメント受信時にHtml5コメントジェネレーターへの通知を行います。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="message"></param>
		private void NotifyMessageToHtml5CommentGenerator(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			if (this.config.EnableHtml5CommentGenerator == false) {
				return;
			}

			var filePath = this.config.Html5CommentGeneratorCommentFilePath;
			if (Path.IsPathRooted(filePath) == false) {
				return;
			}

			if (String.IsNullOrEmpty(Path.GetFileName(filePath))) {
				return;
			}

			try {
				Html5CommentGeneratorNotifier.write(filePath, message);
			} catch (IOException e) {
				logger.Error(e);
			}
		}

		/// <summary>
		/// 人数更新受信時に実行されるイベントです。
		/// </summary>
		/// <param name="count"></param>
		private void UpdateMember(Int32 count) {
			this.Listener = count;
		}

		/// <summary>
		/// BANやコメント非表示などコメントの状態が変更される通知を受け取った時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void CommentStatusChange(Lib.Message message) {
			if (this.commentClient.JoinedRoomSummary == null) {
				return;
			}

			var newMessage = new Message(this.commentClient.JoinedRoomSummary, message);
			newMessage.OnBanUser += this.BanUser;
			newMessage.OnUnBanUser += this.UnBanUser;
			newMessage.OnShowId += this.ShowId;
			newMessage.OnHideId += this.HideId;
			newMessage.OnShowComment += this.ShowComment;
			newMessage.OnHideComment += this.HideComment;
			newMessage.OnAllowInstantMessage += this.AllowInstantMessage;
			var index = this.MessageList.ToList().FindIndex(m => m.Number == newMessage.Number);
			if (index < 0) {
				return;
			}

			this.MessageList[index] = newMessage;
		}

		/// <summary>
		/// インスタントメッセージ受信時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void NotifyInstantMessage(String message) {
			uiDispatcher.BeginInvoke(new Action(() => {
				var instantMessageBox = new InstantMessageBox();
				var viewModel = new InstantMessageBoxViewModel(message);
				instantMessageBox.DataContext = viewModel;
				instantMessageBox.Show();
			}));
		}

		/// <summary>
		/// 管理者メッセージ通知時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void NotifyAdminShout(String message) {
			SystemSounds.Asterisk.Play();
			MessageBox.Show(message, "管理者メッセージ", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
		}

		/// <summary>
		/// 配信終了通知時に実行されるイベントです。
		/// </summary>
		/// <param name="liveEntry"></param>
		private void NotifyLiveClose(Lib.LiveNotification liveEntry) {
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
		private void LogError(Exception e) {
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

		private async void LogoutCavetube() {
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

			try {
				var isSuccess = await CavetubeAuth.LogoutAsync(userId, password);
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

		private async void ShowStartBroadcast() {
			try {
				var window = new StartBroadcast();
				var viewModel = await StartBroadcastViewModel.CreateInstance();
				viewModel.OnStreamStart += roomId => {
					uiDispatcher.BeginInvoke(new Action(() => {
						window.Close();
						if (String.IsNullOrWhiteSpace(roomId)) {
							return;
						}
						this.JoinRoom(roomId);
					}));
				};
				window.Closed += (sender, e) => {
					viewModel.Dispose();
				};
				window.DataContext = viewModel;
				window.ShowDialog();
			} catch (CommentException e) {
				logger.Error(e);
				return;
			}
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

		public String Icon {
			get { return this.message.Icon;  }
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

		public Boolean IsHide {
			get { return this.message.IsHide; }
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
		public event Action<Message> OnShowComment;
		public event Action<Message> OnHideComment;
		public event Action<Message> OnAllowInstantMessage;

		public ICommand CopyCommentCommand { get; private set; }
		public ICommand BanUserCommand { get; private set; }
		public ICommand UnBanUserCommand { get; private set; }
		public ICommand ShowIdCommand { get; private set; }
		public ICommand HideIdCommand { get; private set; }
		public ICommand ShowCommentCommand { get; private set; }
		public ICommand HideCommentCommand { get; private set; }
		public ICommand MarkCommand { get; private set; }
		public ICommand AllowInstantMessageCommand { get; private set; }

		public Message(Lib.Summary summary, Lib.Message message) {
			var uiDispatcher = Dispatcher.CurrentDispatcher; ;
			this.summary = summary;
			this.message = message;

			this.CopyCommentCommand = new RelayCommand(p => {
				if (String.IsNullOrEmpty(this.Comment)) {
					return;
				}

				uiDispatcher.BeginInvoke(new Action(() => {
					Clipboard.SetText(this.Comment, TextDataFormat.UnicodeText);
				}));
			});

			this.BanUserCommand = new RelayCommand(p => {
				this.OnBanUser?.Invoke(this);
			});

			this.UnBanUserCommand = new RelayCommand(p => {
				this.OnUnBanUser?.Invoke(this);
			});

			this.ShowIdCommand = new RelayCommand(p => {
				this.OnShowId?.Invoke(this);
			});

			this.HideIdCommand = new RelayCommand(p => {
				this.OnHideId?.Invoke(this);
			});

			this.ShowCommentCommand = new RelayCommand(p => {
				this.OnShowComment?.Invoke(this);
			});

			this.HideCommentCommand = new RelayCommand(p => {
				this.OnHideComment?.Invoke(this);
			});

			this.AllowInstantMessageCommand = new RelayCommand(p => {
				this.OnAllowInstantMessage?.Invoke(this);
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

				this.OnMarkListener?.Invoke(this);
			});
		}

		public override bool Equals(object obj) {
			var other = obj as Message;
			if (other == null) {
				return false;
			}

			var isNumberSame = this.Number == other.Number;
			var isBanSame = this.IsBan == other.IsBan;
			var isHideSame = this.IsHide == other.IsHide;
			var isPostTimeSame = this.PostTime == other.PostTime;
			return isNumberSame && isBanSame && isHideSame && isPostTimeSame;
		}

		public override int GetHashCode() {
			return this.Number.GetHashCode() ^ this.IsBan.GetHashCode() ^ this.IsHide.GetHashCode() ^ this.PostTime.GetHashCode();
		}
	}
}