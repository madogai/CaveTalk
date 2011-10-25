namespace CaveTube.CaveTalk.View {

	using System.Windows.Controls.Primitives;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Control;
	using CaveTube.CaveTalk.Properties;
	using CaveTube.CaveTalk.ViewModel;
	using Microsoft.Windows.Controls.Ribbon;
	using System.Media;
	using System.IO;
	using System.Windows.Media;
	using System;

	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : RibbonWindow {
		public static readonly ICommand RestoreWindowCommand = new RoutedCommand("RestoreWindow", typeof(MainWindow));

		private MediaPlayer player;

		public MainWindow() {
			InitializeComponent();

			Focus();

			this.player = new MediaPlayer();

			this.Loaded += (sender, e) => {
				var context = (MainWindowViewModel)this.DataContext;
				context.OnNotifyLive += liveInfo => {
					var notifyState = (NotifyPopupStateEnum)Settings.Default.NotifyState;
					if (notifyState == NotifyPopupStateEnum.False) {
						return;
					}

					var balloon = new NotifyBalloon();
					balloon.OnClose += () => this.MyNotifyIcon.CloseBalloon();
					balloon.DataContext = liveInfo;
					var timeout = Settings.Default.NotifyPopupTime * 1000;
					this.MyNotifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, timeout);
				};
				context.OnNotifyLive += liveInfo => {
					var soundFilePath = Settings.Default.NotifySoundFilePath;
					if (File.Exists(soundFilePath) == false) {
						return;
					}
					this.player.Open(new Uri(soundFilePath, UriKind.Absolute));
					this.player.MediaEnded += (sender2, e2) => this.player.Close();
					this.player.Play();
				};

				context.OnMessage += message => {
					var commentState = (CommentPopupStateEnum)Settings.Default.CommentPopup;
					if (commentState == CommentPopupStateEnum.Disable) {
						return;
					}

					if (commentState == CommentPopupStateEnum.Minimum && this.Root.WindowState != System.Windows.WindowState.Minimized) {
						return;
					}

					if (message.IsBan == true) {
						return;
					}

					var balloon = new MessageBalloon();
					balloon.DataContext = message;
					var timeout = Settings.Default.CommentPopupTime * 1000;
					this.MyNotifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, timeout);
				};
			};
		}

		private void RestoreWindowExecuted(object sender, ExecutedRoutedEventArgs e) {
			this.Root.WindowState = System.Windows.WindowState.Normal;
		}

		// RoutedCommandが上手くいかなかったのでとりあえずイベントハンドラで登録します。
		private void ResotreWindowButtonClick(object sender, System.Windows.RoutedEventArgs e) {
			this.Root.WindowState = System.Windows.WindowState.Normal;
		}
	}
}