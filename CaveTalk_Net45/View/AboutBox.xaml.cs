namespace CaveTube.CaveTalk.View {

	using System.Windows;
	using CaveTube.CaveTalk.ViewModel;

	/// <summary>
	/// AboutBox.xaml の相互作用ロジック
	/// </summary>
	public partial class AboutBox : Window {

		public AboutBox() {
			InitializeComponent();
			this.DataContext = new AboutBoxViewModel();
		}
	}
}