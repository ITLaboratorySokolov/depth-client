using System;
using System.Windows.Input;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// Generic command class, it takes action without arguments and a function that says if the action can be executed.
    /// </summary>
    public class Command : ICommand
    {
        /// <summary>
        /// The action which will be executed.
        /// </summary>
        private Action execute;

        /// <summary>
        /// The function that says if the action can be executed.
        /// </summary>
        private Func<bool> enabled;

        /// <summary>
        /// Creates a new instance of the command with the given action and function.
        /// </summary>
        /// <param name="execute">The action which will be executed.</param>
        /// <param name="enabled">The fuction that says if the action can be executed.</param>
        public Command(Action execute, Func<bool> enabled = null)
        {
            this.execute = execute;
            this.enabled = enabled;
        }

        /// <summary>
        /// The event handler that handles changes of the "can execute action" function. 
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Returns true if this command can be executed, false otherwise.
        /// </summary>
        /// <param name="parameter">Generic parameter (not used).</param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return enabled == null ? true : enabled.Invoke();
        }

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="parameter">Generic parameter (not used).</param>
        public void Execute(object parameter)
        {
            execute?.Invoke();
        }
    }
}
