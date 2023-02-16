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

        private void HoleFilterBT_Click(object sender, RoutedEventArgs e)
        {
            string content = (sender as Button).Content.ToString();
            MainViewModel dc = (DataContext as MainViewModel);

            int val = 0;
            switch (content) {
                case "fill__from__left":
                    val = 0;
                    break;
                case "farest__from__around":
                    val = 1;
                    break;
                case "nearest__from__around":
                    val = 2;
                    break;
            }

            dc.HoleMethod = val;

            HoleDropdown.IsOpen = false;
            HoleDropdown.Content = content;
        }


        private void PersistencyIndexBT_Click(object sender, RoutedEventArgs e)
        {
            string content = (sender as Button).Content.ToString();
            MainViewModel dc = (DataContext as MainViewModel);

            int val = 0;
            switch (content)
            {
                case "Disabled":
                    val = 0;
                    break;
                case "Valid in 8/8":
                    val = 1;
                    break;
                case "Valid in 2/last 3":
                    val = 2;
                    break;
                case "Valid in 2/last 4":
                    val = 3;
                    break;
                case "Valid in 2/8":
                    val = 4;
                    break;
                case "Valid in 1/last 2":
                    val = 5;
                    break;
                case "Valid in 1/last 5":
                    val = 6;
                    break;
                case "Valid in 1/last 8":
                    val = 7;
                    break;
                case "Persist Indefinitely":
                    val = 8;
                    break;
            }

            dc.PersIndex = val;

            PersistencyDropdown.IsOpen = false;
            PersistencyDropdown.Content = content;

        }
    }

}
