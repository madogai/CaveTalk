namespace CaveTube.CaveTalk.View {

	using System;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Documents;

	/// <summary>
	/// NotifyUpdateBox.xaml の相互作用ロジック
	/// </summary>
	public partial class NotifyUpdateBox : Window {

		public NotifyUpdateBox() {
			InitializeComponent();
		}

		private void OpenUrl(Object sender, RoutedEventArgs e) {
			var hyperlink = (Hyperlink)e.Source;
			Process.Start(hyperlink.NavigateUri.AbsoluteUri);
		}

		private void CloseWindow(Object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}