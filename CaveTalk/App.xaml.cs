namespace CaveTube.CaveTalk {

	using System;
	using System.Windows;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTalk.View;
	using CaveTube.CaveTalk.ViewModel;
	using NLog;

	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {
		private Logger logger = LogManager.GetCurrentClassLogger();

		protected override void OnStartup(StartupEventArgs e) {
			try {
				base.OnStartup(e);

				// 保存用テーブルの作成
				this.CreateTables();

				var model = new MainWindowViewModel();

				var window = new MainWindow {
					DataContext = model,
				};

				window.Loaded += (e2, args) => {
					model.Initialize();
				};

				window.Closed += (e2, args) => {
					model.Dispose();
				};

				window.Show();
			} catch(Exception ex) {
				logger.Error(ex.ToString());
				throw;
			}
		}

		protected override void OnExit(ExitEventArgs e) {
			DapperUtil.Vacuum();
			base.OnExit(e);
		}

		private void CreateTables() {
			Account.CreateTable();
			Config.CreateTable();
			Listener.CreateTable();
			Model.Message.CreateTable();
			Room.CreateTable();
		}
	}
}