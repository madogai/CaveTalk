namespace CaveTube.CaveTalk.Behavior {

	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Interactivity;

	public sealed class ExecCommandDoubleClickBehavior : Behavior<DataGrid> {
		private const int DoubleClickDureationMilliSeconds = 250;
		private LastClickInfo lastClickInfo = LastClickInfo.Empty;

		public String Command {
			get {
				return (String)GetValue(CommandProperty);
			}
			set {
				SetValue(CommandProperty, value);
			}
		}

		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command", typeof(String), typeof(ExecCommandDoubleClickBehavior));

		protected override void OnAttached() {
			base.OnAttached();

			this.AssociatedObject.LoadingRow += this.AssociatedObjectLoadingRow;
		}

		protected override void OnDetaching() {
			base.OnDetaching();

			this.AssociatedObject.LoadingRow -= this.AssociatedObjectLoadingRow;
			this.AssociatedObject.UnloadingRow -= this.AssociatedObjectUnloadingRow;
		}

		private void AssociatedObjectLoadingRow(object sender, DataGridRowEventArgs e) {
			DataGridRow row = e.Row;

			// DataGridRowにダブルクリック検知用イベントハンドラを追加する。
			row.MouseLeftButtonUp -= this.RowMouseLeftButtonUp;
			row.MouseLeftButtonUp += this.RowMouseLeftButtonUp;
		}

		private void AssociatedObjectUnloadingRow(object sender, DataGridRowEventArgs e) {
			DataGridRow row = e.Row;
			row.MouseLeftButtonUp -= new MouseButtonEventHandler(this.RowMouseLeftButtonUp);
		}

		private void RowMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			DateTime now = DateTime.Now;
			DateTime lastClickTime = this.lastClickInfo.Time;
			DataGridRow lastClickRow = this.lastClickInfo.Row;

			// 直前にクリックされた行が今回クリックされた行と一致し、
			// かつ、クリックされた間隔が一定時間以内に収まっているか?
			if (lastClickRow == sender
				&& (now - lastClickTime).TotalMilliseconds < DoubleClickDureationMilliSeconds) {
				// 直前にクリックされた情報を初期化。
				this.lastClickInfo = LastClickInfo.Empty;

				// ダブルクリックイベントを発行
				this.RaiseDoubleClick(sender as DataGridRow);
			} else {
				// クリックした行情報を保持しておく
				this.lastClickInfo = new LastClickInfo(now, sender as DataGridRow);
			}
		}

		private void RaiseDoubleClick(DataGridRow row) {
			var path = this.Command;
			var dataContext = row.DataContext;
			var command = dataContext.GetType().GetProperty(path).GetValue(dataContext, null) as ICommand;

			if (command != null && command.CanExecute(this.AssociatedObject)) {
				command.Execute(this.AssociatedObject);
			}
		}

		/// <summary> クリックした行情報を保持する。 </summary>
		private class LastClickInfo {
			public static readonly LastClickInfo Empty = new LastClickInfo(DateTime.MinValue, null);

			public LastClickInfo(DateTime time, DataGridRow row) {
				this.Time = time;
				this.Row = row;
			}

			/// <summary> クリックされた時間を取得する。 </summary>
			public DateTime Time { get; private set; }

			/// <summary> クリックされた行を取得する。 </summary>
			public DataGridRow Row { get; private set; }
		}
	}
}