using System;
using System.Collections.Generic;
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
using System.Diagnostics;

namespace CaveTube.CaveTalk.Control {
	/// <summary>
	/// NotifyBalloon.xaml の相互作用ロジック
	/// </summary>
	public partial class NotifyBalloon : UserControl {
		public event Action OnClose;

		public NotifyBalloon() {
			InitializeComponent();
		}

		private void CloseExecuted(object sender, ExecutedRoutedEventArgs e) {
			if (this.OnClose != null) {
				this.OnClose();
			}
		}

		private void OpenExecuted(object sender, ExecutedRoutedEventArgs e) {
			var roomId = e.Parameter as String;
			if (roomId == null) {
				return;
			}

			Process.Start(String.Format("http://gae.cavelis.net/view/{0}", roomId));
		}
	}
}
