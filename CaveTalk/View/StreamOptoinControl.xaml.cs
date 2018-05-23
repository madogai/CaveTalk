using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CaveTube.CaveTalk.View {
	/// <summary>
	/// YouTubeOptoin.xaml の相互作用ロジック
	/// </summary>
	public partial class StreamOptoinControl : UserControl {
		public StreamOptoinControl() {
			InitializeComponent();
		}

		private void OpenUrl(Object sender, RoutedEventArgs e) {
			var hyperlink = (Hyperlink)e.Source;
			Process.Start(hyperlink.NavigateUri.AbsoluteUri);
		}
	}
}
