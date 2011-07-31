namespace CaveTube.CaveTalk.Behavior {

	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Interactivity;

	public class ExecuteCommandOnCtrlAndEnterKeyDownBehavior : Behavior<TextBox> {

		public string Command {
			get {
				return (String)GetValue(CommandProperty);
			}
			set {
				SetValue(CommandProperty, value);
			}
		}

		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command", typeof(String), typeof(ExecuteCommandOnCtrlAndEnterKeyDownBehavior));

		protected override void OnAttached() {
			base.OnAttached();
			this.AssociatedObject.KeyDown += OnKeyDown;
		}

		protected override void OnDetaching() {
			base.OnDetaching();
			this.AssociatedObject.KeyDown -= OnKeyDown;
		}

		public void OnKeyDown(Object sender, KeyEventArgs e) {
			var isEneter = new[] { Key.Return, Key.Enter }.Contains(e.Key);
			var isPressCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

			if ((isEneter && isPressCtrl) == false) {
				return;
			}

			var path = this.Command;
			var dataContext = AssociatedObject.DataContext;
			var command = dataContext.GetType().GetProperty(path).GetValue(dataContext, null) as ICommand;

			if (command != null && command.CanExecute(this.AssociatedObject)) {
				command.Execute(this.AssociatedObject);
			}
		}
	}
}