namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using System.Net;
	using System.Runtime.InteropServices;
	using System.Windows;
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

				return String.IsNullOrEmpty(this.commentClient.RoomId) == false;
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

			#endregion

			this.PostName = this.config.UserId;
		}

		/// <summary>
		/// 初期化を行います。
		/// </summary>
		public void Initialize() {
			#region バージョン確認

			//using (var client = new WebClient()) {
			//    var version = client.DownloadString(ConfigurationManager.AppSettings["version_check_url"]);
			//    var fileInfo = FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]);
			//}

			#endregion

			#region 読み上げソフト

			this.ConnectSpeakApplication();

			#endregion
		}

		/// <summary>
		/// 画面が閉じるときに呼ばれます。
		/// オブジェクトを破棄します。
		/// </summary>
		protected override void OnDispose() {
			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			if (this.speechClient != null) {
				this.speechClient.Dispose();
			}
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
				this.commentClient.OnError += this.OnError;
				this.commentClient.Connect();

				var room = this.commentClient.GetRoom(liveUrl);
				var roomId = room.Summary.RoomId;
				if (String.IsNullOrWhiteSpace(roomId)) {
					Mouse.OverrideCursor = null;
					MessageBox.Show("不正なURLです。");
					return;
				}

				// URLを部屋名に更新します。
				this.LiveUrl = roomId;

				room.Messages.Select(m => new Message(m, this.BanUser, this.UnBanUser, this.MarkListener)).ForEach(m => {
					this.MessageList.Insert(0, m);
				});

				this.commentClient.JoinRoom(roomId);
			}
			catch (CommentException e) {
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

			if (this.config.UserId != this.commentClient.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			this.commentClient.BanListener(commentNum, this.config.ApiKey);

			try {

				base.OnPropertyChanged("MessageList");
			}
			catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BANできませんでした。");
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

			if (this.config.UserId != this.commentClient.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				this.commentClient.UnBanListener(commentNum, this.config.ApiKey);
			}
			catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BAN解除できませんでした。");
			}
		}

		/// <summary>
		/// Id付きのリスナーに色を付けます。
		/// </summary>
		/// <param name="id"></param>
		private void MarkListener(Int32 commentNum, String id) {
			var comment = this.MessageList.FirstOrDefault(m => m.Number == commentNum);
			if (comment == null) {
				return;
			}

			var solidBrush = comment.Color as SolidColorBrush;
			if (solidBrush.Color != Colors.White) {
				comment.Color = Brushes.White;
			}
			else {
				var random = new Random();
				// 暗い色だと文字が見えなくなるので、96以上とします。
				var red = (byte)random.Next(96, 255);
				var green = (byte)random.Next(96, 255);
				var blue = (byte)random.Next(96, 255);
				comment.Color = new SolidColorBrush(Color.FromRgb(red, green, blue));
			}

			this.MessageList.Refresh();
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
				base.OnPropertyChanged("SpeakApplicationStatus");
			}
			catch (ConnectionException e) {
				MessageBox.Show(e.Message);
			}
		}

		/// <summary>
		/// 読み上げソフトを切断します。
		/// </summary>
		private void DisconnectSpeakApplication() {
			this.speechClient.Disconnect();
			base.OnPropertyChanged("SpeakApplicationStatus");
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
		/// <param name="mes"></param>
		private void OnReceiveMessage(Lib.Message mes) {
			// コメントを追加
			var message = new Message(mes, this.BanUser, this.UnBanUser, this.MarkListener);
			this.MessageList.Insert(0, message);

			// コメントの読み上げ
			if (this.speechClient != null || this.speechClient.IsConnect == false) {
				var isSpeech = this.speechClient.Speak(mes);
				if (isSpeech == false) {
					base.OnPropertyChanged("SpeakApplicationStatus");
					MessageBox.Show("読み上げに失敗しました。");
				}
			}

			// コードビハインドのイベントを実行
			if (this.OnMessage != null) {
				uiDispatcher.BeginInvoke(new Action(() => {
					this.OnMessage(mes, this.config);
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
			var target = this.MessageList.FirstOrDefault(m => m.Number == message.Number);
			target.IsBan = true;
		}

		/// <summary>
		/// BAN解除通知時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void OnUnBanUser(Lib.Message message) {
			var target = this.MessageList.FirstOrDefault(m => m.Number == message.Number);
			target.IsBan = false;
		}

		/// <summary>
		/// CaveTubeClientから何かしらのエラーが通知されたときに実行されるイベントです。
		/// </summary>
		/// <param name="e"></param>
		private void OnError(Exception e) {
			MessageBox.Show(e.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
					this.config.ApiKey = null;
					this.config.UserId = null;
					this.config.Password = null;
					this.config.Save();

					base.OnPropertyChanged("LoginStatus");
				}
			}
			catch (WebException) {
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
	}

	public sealed class Message : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();
		private Lib.Message message;

		public Int32 Number {
			get { return this.message.Number; }
			set {
				this.message.Number = value;
				base.OnPropertyChanged("Number");
			}
		}

		public String ListenerId {
			get {
				if (this.message.ListenerId == null) {
					return null;
				}
				return this.message.ListenerId;
			}
			set {
				if (this.message.ListenerId == null) {
					return;
				}

				this.message.ListenerId = value;
				base.OnPropertyChanged("ListenerId");
			}
		}

		public String Name {
			get { return this.message.Name; }
			set {
				this.message.Name = value;
				base.OnPropertyChanged("Name");
			}
		}

		public String Comment {
			get { return this.message.Comment; }
			set {
				this.message.Comment = value;
				base.OnPropertyChanged("Comment");
			}
		}

		public DateTime PostTime {
			get { return this.message.PostTime; }
			set {
				this.message.PostTime = value;
				base.OnPropertyChanged("PostTime");
			}
		}

		public Boolean IsAuth {
			get { return this.message.IsAuth; }
			set {
				this.message.IsAuth = value;
				base.OnPropertyChanged("IsAuth");
			}
		}

		public Boolean IsBan {
			get { return this.message.IsBan; }
			set {
				this.message.IsBan = value;
				base.OnPropertyChanged("IsBan");
			}
		}

		public Boolean IsAsciiArt {
			get {
				return this.message.IsAsciiArt;
			}
		}

		public Brush Color {
			get {
				if (this.message.ListenerId == null) {
					return new SolidColorBrush(Colors.White);
				}

				var listener = Model.Listener.GetListener(this.message.ListenerId);
				if (listener == null || String.IsNullOrWhiteSpace(listener.Color)) {
					return new SolidColorBrush(Colors.White);
				}
				return (Brush)new BrushConverter().ConvertFrom(listener.Color);
			}
			set {
				if (this.message.ListenerId == null) {
					return;
				}

				var listener = Model.Listener.GetListener(this.message.ListenerId);
				if (listener == null) {
					return;
				}

				var color = value.ToString();

				var account = listener.Account;
				// アカウントが存在しない場合はリスナーの色のみ変えます。
				if (account == null) {
					listener.Color = color;
					Model.Listener.UpdateListener(listener);
					base.OnPropertyChanged("Color");
					return;
				}

				// 全コメントの色を変更します。
				account.Color = color;
				var listeners = account.Listeners.Select(l => {
					l.Color = color;
					return l;
				});
				Model.Listener.UpdateListener(listeners);
				base.OnPropertyChanged("Color");
			}
		}

		public event Action<Int32> OnBanUser;
		public event Action<Int32> OnUnBanUser;
		public event Action<Int32, String> OnMarkListener;

		public ICommand CopyCommentCommand { get; private set; }
		public ICommand BanUserCommand { get; private set; }
		public ICommand UnBanUserCommand { get; private set; }
		public ICommand MarkCommand { get; private set; }

		public Message(Lib.Message message, Action<Int32> OnBan, Action<Int32> OnUnBan, Action<Int32, String> OnMarkListener) {
			this.message = message;

			this.OnBanUser += OnBan;
			this.OnUnBanUser += OnUnBan;
			this.OnMarkListener += OnMarkListener;

			this.CopyCommentCommand = new RelayCommand(p => {
				try {
					Clipboard.SetText(this.Comment);
				}
				catch (ExternalException e) {
					MessageBox.Show("クリップボードへのコピーに失敗しました。");
					logger.Error("クリップボードのコピーへの失敗しました。", e);
				}
				catch (ArgumentException e) {
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
			this.MarkCommand = new RelayCommand(p => {
				if (this.OnMarkListener != null && String.IsNullOrWhiteSpace(this.ListenerId) == false) {
					this.OnMarkListener(this.Number, this.ListenerId);
				}
			});
		}
	}
}