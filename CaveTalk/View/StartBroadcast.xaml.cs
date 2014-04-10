namespace CaveTube.CaveTalk.View {
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Documents;
	using System.Windows.Navigation;

	/// <summary>
	/// StartBroadcast.xaml の相互作用ロジック
	/// </summary>
	public partial class StartBroadcast : Window {
		public StartBroadcast() {
			InitializeComponent();
		}

		private void OpenUrl(object sender, RequestNavigateEventArgs e) {
			var hyperlink = (Hyperlink)e.Source;
			Process.Start(hyperlink.NavigateUri.AbsoluteUri);
		}
	}
}
