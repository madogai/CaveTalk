namespace CaveTube.CaveTalk.ViewModel {

	using System;
	using System.Collections.Generic;
	using System.Runtime.Remoting;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Utils;
	using FNF.Utility;
	using System.Net;
	using System.Windows.Data;
	using System.Collections.Specialized;
	using System.Windows.Media;

	public sealed class MainWindowViewModel : ViewModelBase {
		private CavetubeClient cavetubeClient;
		private BouyomiChanClient bouyomiClient;

		public IList<Message> MessageList { get; private set; }

		public Boolean ConnectingStatus {
			get {
				return this.cavetubeClient.IsConnect;
			}
		}

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

		public ICommand ConnectCavetubeCommand { get; private set; }
		public ICommand PostCommentCommand { get; private set; }
		public ICommand SwitchBouyomiCommand { get; private set; }
		public ICommand AboutBoxCommand { get; private set; }

		public MainWindowViewModel() {
			this.MessageList = new SafeObservable<Message>();

			this.cavetubeClient = new CavetubeClient();
			this.cavetubeClient.OnMessage += (sender, summary, message) => this.AddMessage(summary, message);
			this.cavetubeClient.OnUpdateMember += (sender, count) => this.UpdateListenerCount(count);
			this.cavetubeClient.OnConnect += (sender, summary, messages) => this.AddMessage(summary, messages);
			this.cavetubeClient.OnClose += (sender, e) => this.ResetStatus();

			this.SwitchBouyomiCommand = new RelayCommand(SwitchBouyomi);
			this.ConnectCavetubeCommand = new RelayCommand(ConnectCavetube);
			this.PostCommentCommand = new RelayCommand(PostComment);
			this.AboutBoxCommand = new RelayCommand(ShowVersion);

			this.ConnectBouyomi();
		}

		protected override void OnDispose() {
			if (this.cavetubeClient != null) {
				this.cavetubeClient.Dispose();
			}

			if (this.bouyomiClient != null) {
				this.bouyomiClient.Dispose();
			}
		}

		private void UpdateListenerCount(Int32 count) {
			this.Listener = count;
		}

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

		private void ResetStatus() {
			base.OnPropertyChanged("ConnectingStatus");
			this.Listener = 0;
			this.PageView = 0;
		}

		private void ConnectCavetube(Object param) {
			if (this.cavetubeClient.IsConnect) {
				this.cavetubeClient.Close();
			}
			this.MessageList.Clear();

			var roomId = this.ParseUrl(this.LiveUrl);
			if (String.IsNullOrEmpty(roomId)) {
				return;
			}

			this.LiveUrl = roomId;
			try {
				this.cavetubeClient.Connect(roomId);
			} catch (WebException) {
				MessageBox.Show("Cavetubeに接続できませんでした。");
			}
		}

		private void PostComment(Object param) {
			if (this.cavetubeClient.IsConnect == false) {
				return;
			}

			this.cavetubeClient.PostComment(this.PostName, this.PostMessage);
			this.PostMessage = String.Empty;
		}

		private void SwitchBouyomi(Object param) {
			// CommandはClick時に実行されるので、このイベントが走る時点で既にCheckedは切り替わっています。
			if (this.BouyomiStatus) {
				this.ConnectBouyomi();
			} else {
				this.DisconnectBouyomi();
			}
		}

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

		private void DisconnectBouyomi() {
			if (this.bouyomiClient != null) {
				this.bouyomiClient.Dispose();
				this.BouyomiStatus = false;
			}
		}

		private void ShowVersion(Object param) {
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

	public sealed class ConnectingStatusConverter : IValueConverter {

		#region IValueConverter メンバー

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return String.Empty;
			}

			var isConnect = (Boolean)value;
			return isConnect ? "ON" : "OFF";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion
	}

	public sealed class NameColorConverter : IValueConverter {

		#region IValueConverter メンバー

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return Brushes.Black;
			}

			var isAuth = (Boolean)value;
			return isAuth ? Brushes.DarkGreen : Brushes.Black;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion
	}
}