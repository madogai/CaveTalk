namespace CaveTube.CaveTalk.Behavior {

	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Interactivity;
	using System.Windows.Media;

	public abstract class PlaceHolderBehaviorBase<T> : Behavior<T> where T : Control {

		public String Placeholder {
			get { return (String)GetValue(PlaceholderProperty); }
			set { SetValue(PlaceholderProperty, value); }
		}

		public static readonly DependencyProperty PlaceholderProperty =
			DependencyProperty.Register("Placeholder", typeof(String), typeof(PlaceHolderBehaviorBase<T>));

		protected abstract String GetContent(T control);

		protected Brush defaultBackground;

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
			var control = sender as T;
			if (control == null) {
				return;
			}

			this.defaultBackground = control.Background;
			control.Background = this.CreateVisualBrush(this.Placeholder);
		}

		private void OnGotFocus(Object sender, RoutedEventArgs e) {
			var control = sender as T;
			if (control == null) {
				return;
			}
			control.Background = this.defaultBackground;
		}

		private void OnLostFocus(Object sender, EventArgs e) {
			var control = sender as T;
			if (control == null) {
				return;
			}
			var content = this.GetContent(control);
			if (String.IsNullOrEmpty(content) == false) {
				return;
			}
			control.Background = this.CreateVisualBrush(this.Placeholder);
		}

		private VisualBrush CreateVisualBrush(string placeHolder) {
			var visual = new Label {
				Content = placeHolder,
				Padding = new Thickness(5, 1, 1, 1),
				Foreground = new SolidColorBrush(Colors.LightGray),
				Background = this.defaultBackground,
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

	public sealed class TextBoxPlaceholderBehavior : PlaceHolderBehaviorBase<TextBox> {
		protected override void OnAttached() {
			base.OnAttached();
			this.AssociatedObject.TextChanged += OnTextChanged;
		}

		protected override void OnDetaching() {
			base.OnDetaching();
			this.AssociatedObject.TextChanged -= OnTextChanged;
		}

		private void OnTextChanged(object sender, TextChangedEventArgs e) {
			var control = sender as TextBox;
			if (control == null) {
				return;
			}

			if (control.IsFocused == false) {
				control.Background = this.defaultBackground;
			}
		}

		protected override string GetContent(TextBox control) {
			return control.Text;
		}
	}

	public sealed class ComboBoxPlaceholderBehavior : PlaceHolderBehaviorBase<ComboBox> {
		protected override string GetContent(ComboBox control) {
			return control.Text;
		}
	}
}