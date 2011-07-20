namespace CaveTalk {

	using System;
	using System.Windows;
	using CaveTalk.ViewModel;
	using NLog;

	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {
		private Logger logger = LogManager.GetLogger("App");

		protected override void OnStartup(StartupEventArgs e) {
			try {
				base.OnStartup(e);

				var window = new MainWindow {
					DataContext = new MainWindowViewModel(),
				};
				window.Show();
			} catch(Exception ex) {
				logger.Error(ex.ToString());
				throw;
			}
		}
	}
}