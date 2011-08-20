namespace CaveTube.CaveTalk.View {

	using System.Windows.Controls.Primitives;
	using CaveTube.CaveTalk.Control;
	using CaveTube.CaveTalk.ViewModel;
	using Microsoft.Windows.Controls.Ribbon;
	using System.Windows.Input;
	using System.Windows.Controls;

	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : RibbonWindow {
		public static readonly ICommand RestoreWindowCommand = new RoutedCommand("RestoreWindow", typeof(MainWindow));

		public MainWindow() {
			InitializeComponent();

			Focus();

			this.Loaded += (sender, e) => {
				var context = (MainWindowViewModel)this.DataContext;
				context.OnNotifyLive += liveInfo => {
					var balloon = new NotifyBalloon();
					balloon.OnClose += () => this.MyNotifyIcon.CloseBalloon();
					balloon.DataContext = liveInfo;
					this.MyNotifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 5000);
				};

				context.OnMessage += message => {
					if (this.Root.WindowState != System.Windows.WindowState.Minimized) {
						return;
					}

					if (message.IsBan == true) {
						return;
					}

					var balloon = new MessageBalloon();
					balloon.DataContext = message;
					this.MyNotifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 3000);
				};
			};
		}

		private void RestoreWindowExecuted(object sender, ExecutedRoutedEventArgs e) {
			this.Root.WindowState = System.Windows.WindowState.Normal;
		}

		// RoutedCommandが上手くいかなかったのでとりあえずイベントハンドらで登録します。
		private void ResotreWindowButtonClick(object sender, System.Windows.RoutedEventArgs e) {
			this.Root.WindowState = System.Windows.WindowState.Normal;
		}
	}
}