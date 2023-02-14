using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.Ui
{
    /// <summary>
    /// Interakční logika pro FilterConfigurationWindow.xaml
    /// </summary>
    public partial class FilterConfigurationWindow : Window
    {
        public FilterConfigurationWindow(MainViewModel dc)
        {
            InitializeComponent();
            DataContext = dc;
        }
    }
}
