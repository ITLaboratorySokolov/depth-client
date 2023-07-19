using System.Security.Cryptography.Xml;
using System.Windows;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.Ui
{
    /// <summary>
    /// Interakční logika pro ConsoleWindow.xaml
    /// </summary>
    public partial class ConsoleWindow : Window
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dc"> Main view model </param>
        public ConsoleWindow(MainViewModel dc)
        {
            InitializeComponent();

            DataContext = dc;
            dc.ConsData.Console = consoleTXT;
        }

    }
}
