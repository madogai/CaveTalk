namespace CaveTube.CaveTalk.View {

	using System;
	using System.Diagnostics;
	using System.Windows;

	/// <summary>
	/// Version.xaml の相互作用ロジック
	/// </summary>
	public partial class AboutBox : Window {

		public AboutBox() {
			InitializeComponent();
			this.DataContext = this;
			this.FileVersionInfo =
				FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]);
		}

		public FileVersionInfo FileVersionInfo {
			get { return (FileVersionInfo)GetValue(FileVersionInfoProperty); }
			set { SetValue(FileVersionInfoProperty, value); }
		}

		public static readonly DependencyProperty FileVersionInfoProperty =
			DependencyProperty.Register("FileVersionInfo", typeof(FileVersionInfo), typeof(AboutBox), new UIPropertyMetadata(null));
	}
}