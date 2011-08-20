namespace CaveTube.CaveTalk.ViewModel {

	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Net;
	using System.Runtime.Remoting;
	using System.Text.RegularExpressions;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Utils;
	using FNF.Utility;
	using NLog;
	using System.Windows.Threading;
	using System.Threading.Tasks;
	using Hardcodet.Wpf.TaskbarNotification;
	using CaveTube.CaveTalk.Control;
	using System.Windows.Controls.Primitives;
	using CaveTube.CaveTalk.CaveTubeClient;
	using CaveTube.CaveTalk.View;

	public sealed class MainWindowViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private CavetubeClient cavetubeClient;
		private BouyomiChanClient bouyomiClient;
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

		private Int32 pageView;

		public Int32 PageView {
			get { return this.pageView; }
			set {
				this.pageView = value;
				base.OnPropertyChanged("PageView");
			}
		}

		public Boolean bouyomiStatus;

		public Boolean BouyomiStatus {
			get { return this.bouyomiStatus; }
			set {
				this.bouyomiStatus = value;
				base.OnPropertyChanged("BouyomiStatus");
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

		public Boolean notifyStatus;

		public Boolean NotifyStatus {
			get { return this.notifyStatus; }
			set {
				this.notifyStatus = value;
				base.OnPropertyChanged("NotifyStatus");

				CaveTalk.Properties.Settings.Default.Notify = value;
				CaveTalk.Properties.Settings.Default.Save();
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
		/// 棒読みちゃんに接続します。
		/// </summary>
		public ICommand ConnectBouyomiCommand { get; private set; }

		/// <summary>
		/// 棒読みちゃんから切断します。
		/// </summary>
		public ICommand DisconnectBouyomiCommand { get; private set; }

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

		#endregion

		public MainWindowViewModel() {
			this.MessageList = new SafeObservable<Message>();
			this.uiDispatcher = Dispatcher.CurrentDispatcher;

			#region Command

			this.ConnectBouyomiCommand = new RelayCommand(p => this.ConnectBouyomi());
			this.DisconnectBouyomiCommand = new RelayCommand(p => this.DisconnectBouyomi());
			this.JoinRoomCommand = new RelayCommand(p => JoinRoom(this.LiveUrl));
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
			this.CopyCommentCommand = new RelayCommand(p => {
				var text = this.MessageList[this.MessageIndex].Comment;
				Clipboard.SetText(text);
			});
			this.EnableNotifyCommand = new RelayCommand(p => this.NotifyStatus = true);
			this.DisableNotifyCommand = new RelayCommand(p => this.NotifyStatus = false);
			this.AboutBoxCommand = new RelayCommand(p => this.ShowVersion());

			#endregion

			this.cavetubeClient = new CavetubeClient();
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
				if (this.OnNotifyLive == null || this.NotifyStatus == false) {
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

			this.NotifyStatus = CaveTalk.Properties.Settings.Default.Notify;
			this.PostName = CaveTalk.Properties.Settings.Default.UserId;
			this.ConnectBouyomi();
		}

		/// <summary>
		/// 画面が閉じるときに呼ばれます。
		/// オブジェクトを破棄します。
		/// </summary>
		protected override void OnDispose() {
			if (this.cavetubeClient != null) {
				this.cavetubeClient.Dispose();
			}

			if (this.bouyomiClient != null) {
				this.bouyomiClient.Dispose();
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
			// コメント取得時のリスナー数がずれてるっぽいので一時的に封印
			// this.Listener = summary.Listener;
			this.PageView = summary.PageView;

			if (message.IsBan) {
				return;
			}

			this.MessageList.Insert(0, message);

			try {
				if (this.BouyomiStatus) {
					this.bouyomiClient.AddTalkTask(message.Comment);
				}
			} catch (RemotingException) {
				this.BouyomiStatus = false;
				MessageBox.Show("棒読みちゃんに接続できませんでした。");
			}
		}

		/// <summary>
		/// コメントを追加します。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="messages"></param>
		private void AddMessage(Summary summary, IEnumerable<Message> messages) {
			base.OnPropertyChanged("ConnectingStatus");
			this.Listener = summary.Listener;
			this.PageView = summary.PageView;
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
			this.PageView = 0;
			this.MessageList.Clear();
		}

		/// <summary>
		/// コメント部屋に接続します。
		/// </summary>
		/// <param name="liveUrl"></param>
		private void JoinRoom(String liveUrl) {
			Mouse.OverrideCursor = Cursors.Wait;

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

		#region 棒読みちゃん

		/// <summary>
		/// 棒読みちゃんに接続します。
		/// </summary>
		private void ConnectBouyomi() {
			this.DisconnectBouyomi();

			try {
				this.bouyomiClient = new BouyomiChanClient();
				var count = this.bouyomiClient.TalkTaskCount;
				this.BouyomiStatus = true;
			} catch (RemotingException) {
				MessageBox.Show("棒読みちゃんに接続できませんでした。\n後から棒読みちゃんを起動する場合は、リボンの棒読みアイコンを押してください。");
			}
		}

		/// <summary>
		/// 棒読みちゃんから切断します。
		/// </summary>
		private void DisconnectBouyomi() {
			if (this.bouyomiClient != null) {
				this.bouyomiClient.Dispose();
				this.BouyomiStatus = false;
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

		private void ShowVersion() {
			new AboutBox().ShowDialog();
		}

		private String ParseUrl(String url) {
			var pattern = @"(http://gae.cavelis.net/[a-z]+/)?([0-9A-Z]{32})";
			var match = Regex.Match(url, pattern);
			if (match.Success) {
				return match.Groups[2].Value;
			} else {
				return String.Empty;
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

	public sealed class ConnectingStatusConverter : IValueConverter {
		#region IValueConverter メンバー

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return String.Empty;
			}

			var isConnect = (Boolean)value;
			var text = isConnect ? "ON" : "OFF";

			return text;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}