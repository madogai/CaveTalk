using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using CaveTalk.ViewModel;

namespace CaveTalk {
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			var window = new MainWindow {
				DataContext = new MainWindowViewModel(),
			};
			window.Show();
		}
	}
}
