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
	using System.Windows.Media;
	using System.Windows.Threading;
	using CaveTube.CaveTalk.Lib;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Properties;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTalk.View;
	using CaveTube.CaveTubeClient;
	using Microsoft.Win32;
	using NLog;
using CaveTube.CaveTalk.Logic;

	public sealed class MainWindowViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		internal CavetubeClient cavetubeClient;
		private ICommentClient commentClient;
		private Dispatcher uiDispatcher;
		private Model.Room room;
		private CaveTalkContext context;

		private SpeechLogic speechLogic;

		public event Action<LiveNotification> OnNotifyLive;
		public event Action<Model.Message> OnMessage;

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

		public Boolean ReadingApplicationStatus {
			get { return this.speechLogic.SpeechStatus; }
			set {
				this.speechLogic.SpeechStatus = value;
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
			this.context = new CaveTalkContext();
			this.context.Database.CreateIfNotExists();

			this.speechLogic = new SpeechLogic();

			#region Commandの登録

			this.ConnectReadingApplicationCommand = new RelayCommand(p => this.speechLogic.Connect());
			this.DisconnectReadingApplicationCommand = new RelayCommand(p => this.speechLogic.Disconnect());
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

			this.speechLogic.Connect();

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

			if (this.speechLogic != null) {
				this.speechLogic.Dispose();
			}
		}

		/// <summary>
		/// メッセージをDBに保存します。
		/// </summary>
		/// <param name="messages"></param>
		private void SaveMessage(IEnumerable<Model.Message> messages) {
			var dbMessages = this.context.Messages.Where(message => message.Room.RoomId == room.RoomId);

			messages.Where(m => dbMessages.All(dm => dm.Number != m.Number && dm.PostTime != m.PostTime)).ForEach(m => {
				this.context.Messages.Add(m);
			});

			this.context.SaveChanges();
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

				var dbRoom = this.context.Rooms.Find(roomId);
				if (dbRoom == null) {
					dbRoom = new Model.Room {
						RoomId = room.Summary.RoomId,
						Title = room.Summary.Title,
						Author = room.Summary.Author,
						StartTime = room.Summary.StartTime,
					};
					this.context.Rooms.Add(dbRoom);
					this.context.SaveChanges();
				}

				this.commentClient.OnJoin += this.OnJoin;
				this.commentClient.OnMessage += this.OnReceiveMessage;
				this.commentClient.OnUpdateMember += this.OnUpdateMember;
				this.commentClient.OnBan += this.OnBanUser;
				this.commentClient.OnUnBan += this.OnUnBanUser;

				if (String.IsNullOrWhiteSpace(roomId) == false) {
					this.LiveUrl = roomId;
				}

				this.room = dbRoom;

				var dbMessages = room.Messages.Select(this.ConvertMessage);

				// DBに保存
				this.SaveMessage(dbMessages);

				// ビューモデルを追加
				var messages = dbMessages.Select(m => new Message(m, this.BanUser, this.UnBanUser, this.MarkListener));
				foreach (var message in messages) {
					this.MessageList.Insert(0, message);
				}

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

			if (Settings.Default.UserId != this.room.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				var isSuccess = this.commentClient.BanListener(commentNum, Settings.Default.ApiKey);
				if (isSuccess == false) {
					MessageBox.Show("BANに失敗しました。");
				}

				base.OnPropertyChanged("MessageList");
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

			if (Settings.Default.UserId != this.room.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				var isSuccess = this.commentClient.UnBanListener(commentNum, Settings.Default.ApiKey);
				if (isSuccess == false) {
					MessageBox.Show("BANに失敗しました。");
				}

				base.OnPropertyChanged("MessageList");

			} catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BAN解除できませんでした。");
			} catch (WebException) {
				logger.Error("CaveTubeとの通信に失敗しました。");
				MessageBox.Show("BAN解除に失敗しました。");
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
			} else {
				var random = new Random();
				// 暗い色だと文字が見えなくなるので、96以上とします。
				var red = (byte)random.Next(96, 255);
				var green = (byte)random.Next(96, 255);
				var blue = (byte)random.Next(96, 255);
				comment.Color = new SolidColorBrush(Color.FromRgb(red, green, blue));
			}

			this.context.SaveChanges();

			this.MessageList.Refresh();
		}

		/// <summary>
		/// CavetubeClientから受け取ったメッセージをモデルに変換します。<br />
		/// 必要に応じてリスナー登録も行います。
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private Model.Message ConvertMessage(Lib.Message message) {
			var listener = this.context.Listener.Find(message.Id);

			if (message.Id != null) {
				var account = message.Auth ? this.context.Account.Find(message.Name) : null;

				// リスナーの登録
				if (listener == null) {
					this.context.Listener.Add(new Model.Listener {
						ListenerId = message.Id,
						Name = message.Name,
						Author = this.room.Author,
						BackgroundColor = account != null ? account.BackgroundColor : Brushes.White,
						Account = account,
					});
					this.context.SaveChanges();
				} else if (message.Auth && listener.Account == null) {
					listener.Account = account ?? new Model.Account {
						AccountName = message.Name,
						BackgroundColor = Brushes.White,
					};

					// アカウントの登録
					this.context.Listener.Where(l => l.ListenerId == listener.ListenerId).ForEach(l => {
						l.Account = account;
						l.Color = account.Color;
					});

					this.context.SaveChanges();
				}
			}

			var dbMessage = new Model.Message {
				Room = this.room,
				Number = message.Number,
				Name = message.Name,
				Comment = message.Comment,
				PostTime = message.Time,
				IsBan = message.IsBan,
				IsAuth = message.Auth,
				Listener = listener,
			};
			return dbMessage;
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
			var dbMessage = this.ConvertMessage(mes);

			// DBに保存
			this.SaveMessage(new[] { dbMessage });

			// ビューモデルを追加
			var message = new Message(dbMessage, this.BanUser, this.UnBanUser, this.MarkListener);
			this.MessageList.Insert(0, message);

			// コメントの読み上げ
			if (this.ReadingApplicationStatus) {
				var isSpeech = this.speechLogic.Speak(dbMessage);
				if (isSpeech == false) {
					MessageBox.Show("読み上げに失敗しました。");
					this.ReadingApplicationStatus = false;
				}
			}

			// コードビハインドのイベントを実行
			if (this.OnMessage != null) {
				uiDispatcher.BeginInvoke(new Action(() => {
					this.OnMessage(dbMessage);
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
			this.context.SaveChanges();
		}

		/// <summary>
		/// BAN解除通知時に実行されるイベントです。
		/// </summary>
		/// <param name="message"></param>
		private void OnUnBanUser(Lib.Message message) {
			var target = this.MessageList.FirstOrDefault(m => m.Number == message.Number);
			target.IsBan = false;
			this.context.SaveChanges();
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

			this.speechLogic.Connect();
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

	public sealed class Message : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();
		private Model.Message message;

		public Int32 Number {
			get { return this.message.Number; }
			set {
				this.message.Number = value;
				base.OnPropertyChanged("Number");
			}
		}

		public String ListenerId {
			get {
				if (this.message.Listener == null) {
					return null;
				}
				return this.message.Listener.ListenerId;
			}
			set {
				if (this.message.Listener == null) {
					return;
				}

				this.message.Listener.ListenerId = value;
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
				if (this.message.Listener == null) {
					return new SolidColorBrush(Colors.White);
				}

				return this.message.Listener.BackgroundColor;
			}
			set {
				if (this.message.Listener == null) {
					return;
				}

				if (this.message.Listener.Account != null) {
					this.message.Listener.Account.BackgroundColor = value;
					// Context経由で取得しないと更新できないのでとりあえずこうしています。
					foreach (var listener in this.message.Listener.Account.Listeners) {
						listener.BackgroundColor = value;
					}
				} else {
					this.message.Listener.BackgroundColor = value;
				}


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

		public Message(Model.Message message, Action<Int32> OnBan, Action<Int32> OnUnBan, Action<Int32, String> OnMarkListener) {
			this.message = message;

			this.OnBanUser += OnBan;
			this.OnUnBanUser += OnUnBan;
			this.OnMarkListener += OnMarkListener;

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
			this.MarkCommand = new RelayCommand(p => {
				if (this.OnMarkListener != null && String.IsNullOrWhiteSpace(this.ListenerId) == false) {
					this.OnMarkListener(this.Number, this.ListenerId);
				}
			});
		}
	}
}