namespace CaveTube.CaveTalk.View {

	using System;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Configuration;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using CaveTube.CaveTalk.Control;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.ViewModel;

	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow {
		public static readonly ICommand RestoreWindowCommand = new RoutedCommand("RestoreWindow", typeof(MainWindow));
		private String previewServer;

		public MainWindow() {
			InitializeComponent();

			Focus();

			this.previewServer = ConfigurationManager.AppSettings["ss_server"] ?? "http://ss.cavelis.net:3001";

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

			this.Loaded += (sender, e) => {
				var messages = (CollectionView)CollectionViewSource.GetDefaultView(this.CommentGrid.Items);
				((INotifyCollectionChanged)messages).CollectionChanged += (sender2, e2) => {
					if (this.CommentNoColumn.SortDirection != ListSortDirection.Ascending) {
						return;
					}

					if (this.CommentGrid.Items.Count == 0) {
						return;
					}

					var border = VisualTreeHelper.GetChild(this.CommentGrid, 0) as Decorator;
					if (border == null) {
						return;
					}

					var scroll = border.Child as ScrollViewer;
					if (scroll == null) {
						return;
					}

					scroll.ScrollToEnd();
				};
			};
		}

		private void RestoreWindowExecuted(Object sender, ExecutedRoutedEventArgs e) {
			this.Root.WindowState = System.Windows.WindowState.Normal;
		}

		// RoutedCommandが上手くいかなかったのでとりあえずイベントハンドラで登録します。
		private void ResotreWindowButtonClick(Object sender, System.Windows.RoutedEventArgs e) {
			this.Root.WindowState = System.Windows.WindowState.Normal;
		}

		private void OpenUrl(Object sender, RoutedEventArgs e) {
			var hyperlink = (Hyperlink)e.Source;
			Process.Start(hyperlink.NavigateUri.AbsoluteUri);
		}

		private void LoadPreview(Object sender, ToolTipEventArgs e) {
			var hyperlink = (Hyperlink)e.Source;
			if (hyperlink.ToolTip is Image) {
				return;
			}

			var uri = $"{previewServer}/?url={hyperlink.NavigateUri.AbsoluteUri}";
			var image = new Image();
			image.Source = new BitmapImage(new Uri(uri));
			hyperlink.ToolTip = image;
		}

		private void Close(object sender, RoutedEventArgs e) {
			this.Close();
		}

		private void Ribbon_Loaded(object sender, RoutedEventArgs e) {
			var child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
			if (child != null) {
				child.RowDefinitions[0].Height = new GridLength(0);
			}
		}
	}
}