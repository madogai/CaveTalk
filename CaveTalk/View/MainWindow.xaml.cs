namespace CaveTube.CaveTalk.View {

	using System.Windows.Controls.Primitives;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Control;
	using CaveTube.CaveTalk.ViewModel;
	using Microsoft.Windows.Controls.Ribbon;
	using System.Windows.Media;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Documents;
	using CaveTube.CaveTalk.Model;

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

				context.OnMessage += (message, config) => {
					var commentState = config.CommentPopupType;
					if (commentState == Config.CommentPopupDisplayType.None) {
						return;
					}

					if (commentState == Config.CommentPopupDisplayType.Minimize && this.Root.WindowState != System.Windows.WindowState.Minimized) {
						return;
					}

					if (message.IsBan == true) {
						return;
					}

					var balloon = new MessageBalloon();
					balloon.DataContext = message;
					var timeout = config.CommentPopupTime * 1000;
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

		private void OpenUrl(object sender, RoutedEventArgs e) {
			var hyperlink = (Hyperlink)e.Source;
			Process.Start(hyperlink.NavigateUri.AbsoluteUri);
		}
	}
}