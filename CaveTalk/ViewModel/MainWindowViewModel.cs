namespace CaveTalk.ViewModel {

	using System;
	using System.Collections.Generic;
	using System.Runtime.Remoting;
	using System.Text.RegularExpressions;
	using System.Windows;
	using System.Windows.Input;
	using CaveTalk.Utils;
	using FNF.Utility;
	using System.Net;

	public sealed class MainWindowViewModel : ViewModelBase {
		private CavetubeClient cavetubeClient;
		private BouyomiChanClient bouyomiClient;

		public String ConnectingStatus {
			get {
				// 面倒なのでコンバータを使わず、直接変換します。
				return this.cavetubeClient.IsConnect ? "ON" : "OFF";
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

		public IList<Message> MessageList { get; private set; }

		public ICommand ConnectCavetubeCommand { get; private set; }
		public ICommand SwitchBouyomiCommand { get; private set; }
		public ICommand AboutBoxCommand { get; private set; }

		public MainWindowViewModel() {
			this.MessageList = new SafeObservable<Message>();

			this.cavetubeClient = new CavetubeClient();
			this.cavetubeClient.OnMessage += (sender, summary, message) => {
				// コメント取得時のリスナー数がずれてるっぽいので一時的に封印
				// this.Listener = summary.Listener;
				this.PageView = summary.PageView;
				this.MessageList.Insert(0, message);
				try {
					if (this.BouyomiStatus) {
						this.bouyomiClient.AddTalkTask(message.Comment);
					}
				} catch (RemotingException) {
					this.BouyomiStatus = false;
					MessageBox.Show("棒読みちゃんに接続できませんでした。");
				}
			};
			this.cavetubeClient.OnUpdateMember += (sender, count) => {
				this.Listener = count;
			};
			this.cavetubeClient.OnConnect += (summary, messages) => {
				base.OnPropertyChanged("ConnectingStatus");
				this.Listener = summary.Listener;
				this.PageView = summary.PageView;
				foreach (var message in messages) {
					this.MessageList.Insert(0, message);
				}
			};
			this.cavetubeClient.OnClose += (obj, e) => {
				base.OnPropertyChanged("ConnectingStatus");
				this.Listener = 0;
				this.PageView = 0;
			};

			this.SwitchBouyomiCommand = new RelayCommand(param => {
				// CommandはClickなので、このイベントが走る時点で既にCheckedは切り替わっています。
				if (this.BouyomiStatus) {
					this.ConnectBouyomi();
				} else {
					this.DisconnectBouyomi();
				}
			});
			this.ConnectCavetubeCommand = new RelayCommand(ConnectCavetube);
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

		private String ParseUrl(String url) {
			var pattern = @"(http://gae.cavelis.net/[a-z]+/)?([0-9A-Z]{32})";
			var match = Regex.Match(url, pattern);
			if (match.Success) {
				return match.Groups[2].Value;
			} else {
				return String.Empty;
			}
		}

		private void ConnectCavetube(object param) {
			var url = param as String;
			if (url == null) {
				return;
			}

			if (this.cavetubeClient.IsConnect) {
				this.cavetubeClient.Close();
			}
			this.MessageList.Clear();

			var roomId = this.ParseUrl(url);
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

		private void ShowVersion(object param) {
			new AboutBox().ShowDialog();
		}
	}
}