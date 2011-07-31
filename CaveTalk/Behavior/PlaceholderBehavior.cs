namespace CaveTube.CaveTalk.Behavior {

	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Interactivity;
	using System.Windows.Media;

	public sealed class TextBoxPlaceholderBehavior : Behavior<TextBox> {

		public String Placeholder {
			get { return (String)GetValue(PlaceholderProperty); }
			set { SetValue(PlaceholderProperty, value); }
		}

		public static readonly DependencyProperty PlaceholderProperty =
			DependencyProperty.Register("Placeholder", typeof(String), typeof(TextBoxPlaceholderBehavior));

		protected override void OnAttached() {
			base.OnAttached();
			this.AssociatedObject.Initialized += this.OnInitialized;
			this.AssociatedObject.GotFocus += this.OnGotFocus;
			this.AssociatedObject.LostFocus += this.OnLostFocus;
		}

		protected override void OnDetaching() {
			base.OnDetaching();
			this.AssociatedObject.Initialized -= this.OnInitialized;
			this.AssociatedObject.GotFocus -= this.OnGotFocus;
			this.AssociatedObject.LostFocus -= this.OnLostFocus;
		}

		private void OnInitialized(Object sender, EventArgs e) {
			var textBox = sender as TextBox;
			if (textBox == null) {
				return;
			}
			textBox.Background = CreateVisualBrush(this.Placeholder);
		}

		private void OnGotFocus(Object sender, RoutedEventArgs e) {
			var textBox = sender as TextBox;
			if (textBox == null) {
				return;
			}
			textBox.Background = new SolidColorBrush(Colors.Transparent);
		}

		private void OnLostFocus(Object sender, EventArgs e) {
			var textBox = sender as TextBox;
			if (textBox == null) {
				return;
			}
			if (String.IsNullOrEmpty(textBox.Text) == false) {
				return;
			}
			textBox.Background = CreateVisualBrush(this.Placeholder);
		}

		private VisualBrush CreateVisualBrush(string placeHolder) {
			var visual = new Label() {
				Content = placeHolder,
				Padding = new Thickness(5, 1, 1, 1),
				Foreground = new SolidColorBrush(Colors.LightGray),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
			};
			return new VisualBrush(visual) {
				Stretch = Stretch.None,
				TileMode = TileMode.None,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Center,
			};
		}
	}

	public sealed class ComboBoxPlaceholderBehavior : Behavior<ComboBox> {

		public String Placeholder {
			get { return (String)GetValue(PlaceholderProperty); }
			set { SetValue(PlaceholderProperty, value); }
		}

		public static readonly DependencyProperty PlaceholderProperty =
			DependencyProperty.Register("Placeholder", typeof(String), typeof(ComboBoxPlaceholderBehavior));

		protected override void OnAttached() {
			base.OnAttached();
			this.AssociatedObject.Initialized += this.OnInitialized;
			this.AssociatedObject.GotFocus += this.OnGotFocus;
			this.AssociatedObject.LostFocus += this.OnLostFocus;
		}

		protected override void OnDetaching() {
			base.OnDetaching();
			this.AssociatedObject.Initialized -= this.OnInitialized;
			this.AssociatedObject.GotFocus -= this.OnGotFocus;
			this.AssociatedObject.LostFocus -= this.OnLostFocus;
		}

		private void OnInitialized(Object sender, EventArgs e) {
			var comboBox = sender as ComboBox;
			if (comboBox == null) {
				return;
			}
			comboBox.Background = CreateVisualBrush(this.Placeholder);
		}

		private void OnGotFocus(Object sender, RoutedEventArgs e) {
			var comboBox = sender as ComboBox;
			if (comboBox == null) {
				return;
			}
			comboBox.Background = new SolidColorBrush(Colors.Transparent);
		}

		private void OnLostFocus(Object sender, EventArgs e) {
			var comboBox = sender as ComboBox;
			if (comboBox == null) {
				return;
			}
			if (String.IsNullOrEmpty(comboBox.Text) == false) {
				return;
			}
			comboBox.Background = CreateVisualBrush(this.Placeholder);
		}

		private VisualBrush CreateVisualBrush(string placeHolder) {
			var visual = new Label() {
				Content = placeHolder,
				Padding = new Thickness(5, 1, 1, 1),
				Foreground = new SolidColorBrush(Colors.LightGray),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
			};
			return new VisualBrush(visual) {
				Stretch = Stretch.None,
				TileMode = TileMode.None,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Center,
			};
		}
	}
}