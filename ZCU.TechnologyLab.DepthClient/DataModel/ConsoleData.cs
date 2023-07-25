using System.Windows.Controls;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    public class ConsoleData : NotifyingClass
    {
        TextBox console;
        string consoleText = "";

        public TextBox Console { get => console;
                                 set => console = value; }

        public string ConsoleText { get => consoleText; 
                                    set { consoleText = value; RaisePropertyChanged("ConsoleText"); } }

        public void AddToConsole(string text)
        {
            if ((ConsoleText.Length > 0) && !ConsoleText.EndsWith("\n"))
                text = "\n" + text.Trim();
            ConsoleText += text;
        }

        public void ClearConsole()
        {
            ConsoleText = string.Empty;
            Console.Dispatcher.Invoke(() =>
            {
                Console.Clear();
                Console.Select(Console.Text.Length, 0);
            });
        }

    }
}
