namespace CaveTube.CaveTalk.Behavior {

	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Interactivity;

	public abstract class ExecCommandKeyDownBehavior<T> : Behavior<T> where T : Control {

		public String Command {
			get {
				return (String)GetValue(CommandProperty);
			}
			set {
				SetValue(CommandProperty, value);
			}
		}

		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command", typeof(String), typeof(ExecCommandKeyDownBehavior<T>));

		public OptionKey CommandOption {
			get { return (OptionKey)GetValue(CommandOptionProperty); }
			set { SetValue(CommandOptionProperty, value); }
		}

		public static readonly DependencyProperty CommandOptionProperty =
			DependencyProperty.Register("CommandOption", typeof(OptionKey), typeof(ExecCommandKeyDownBehavior<T>), new UIPropertyMetadata(OptionKey.None));



		protected override void OnAttached() {
			base.OnAttached();
			this.AssociatedObject.PreviewKeyDown += OnKeyDown;
		}

		protected override void OnDetaching() {
			base.OnDetaching();
			this.AssociatedObject.PreviewKeyDown -= OnKeyDown;
		}

		public void OnKeyDown(Object sender, KeyEventArgs e) {
			var isFireEvent = this.IsFire(e);
			if (isFireEvent == false) {
				return;
			}

			e.Handled = true;
			var path = this.Command;
			var dataContext = AssociatedObject.DataContext;
			var command = dataContext.GetType().GetProperty(path).GetValue(dataContext, null) as ICommand;

			if (command != null && command.CanExecute(this.AssociatedObject)) {
				command.Execute(this.AssociatedObject);
			}
		}

		protected abstract Boolean IsFire(KeyEventArgs e);

		[Flags]
		public enum OptionKey {
			None,
			Ctrl,
			Shift,
			Alt,
		}
	}

	public sealed class ExecCommandOnEnterKeyDownBehavior : ExecCommandKeyDownBehavior<ComboBox> {

		protected override Boolean IsFire(KeyEventArgs e) {
			var isEneter = new[] { Key.Return, Key.Enter }.Contains(e.Key);
			return isEneter;
		}
	}

	public sealed class ExecCommandOnCtrlOrShiftAndEnterKeyDownBehavior : ExecCommandKeyDownBehavior<TextBox> {

		protected override Boolean IsFire(KeyEventArgs e) {
			var isEneter = new[] { Key.Return, Key.Enter }.Contains(e.Key);
			var isPressCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
			var isPressShift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
			return isEneter && (isPressCtrl || isPressShift);
		}
	}
}