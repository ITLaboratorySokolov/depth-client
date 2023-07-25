using System;
using System.Security.Cryptography.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZCU.TechnologyLab.DepthClient.DataModel;
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
        public ConsoleWindow(MainViewModel mvm)
        {
            InitializeComponent();

            DataContext = mvm;
            mvm.ConsData.Console = consoleTXT;

            /*
            ConsoleViewModel cvm = DataContext as ConsoleViewModel;
            cvm.ConsoleData = cd;
            cd.Console = consoleTXT;
            */

        }


    }
}
