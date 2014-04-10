namespace CaveTube.CaveTalk.View {

	using System;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Documents;

	/// <summary>
	/// InstantMessageBox.xaml の相互作用ロジック
	/// </summary>
	public partial class InstantMessageBox : Window {

		public InstantMessageBox() {
			InitializeComponent();
		}

		private void CloseWindow(Object sender, RoutedEventArgs e) {
			this.Close();
		}
	}
}