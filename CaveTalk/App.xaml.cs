namespace CaveTube.CaveTalk {

	using System;
	using System.Windows;
	using CaveTube.CaveTalk.ViewModel;
	using NLog;
	using CaveTube.CaveTalk.View;

	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {
		private Logger logger = LogManager.GetLogger("App");

		protected override void OnStartup(StartupEventArgs e) {
			try {
				base.OnStartup(e);

				var model = new MainWindowViewModel();

				var window = new MainWindow {
					DataContext = model,
				};

				window.Loaded += (e2, args) => {
					model.Initialize();
				};

				window.Show();
			} catch(Exception ex) {
				logger.Error(ex.ToString());
				throw;
			}
		}
	}
}