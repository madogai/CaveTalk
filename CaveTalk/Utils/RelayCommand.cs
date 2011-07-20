namespace CaveTalk.Utils {

	using System;
	using System.Diagnostics;
	using System.Windows.Input;

	public class RelayCommand : ICommand {
		readonly Action<object> execute;
		readonly Predicate<object> canExecute;

		#region Constructors

		/// <summary>
		/// Creates a new command that can always execute.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		public RelayCommand(Action<object> execute)
			: this(execute, null) {
		}

		/// <summary>
		/// Creates a new command.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic.</param>
		public RelayCommand(Action<object> execute, Predicate<object> canExecute) {
			if (execute == null) {
				throw new ArgumentNullException("execute");
			}

			this.execute = execute;
			this.canExecute = canExecute;
		}

		#endregion Constructors

		#region ICommand Members

		[DebuggerStepThrough]
		public bool CanExecute(object parameter) {
			return canExecute == null ? true : canExecute(parameter);
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter) {
			execute(parameter);
		}

		#endregion ICommand Members
	}
}