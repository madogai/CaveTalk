using System;
using System.Windows.Controls;
using System.Windows.Input;
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
			this.OnClose?.Invoke();
		}

		private void OpenExecuted(object sender, ExecutedRoutedEventArgs e) {
			var roomId = e.Parameter as String;
			if (roomId == null) {
				return;
			}

			Process.Start($"https://www.cavelis.net/view/{roomId}");
		}
	}
}
