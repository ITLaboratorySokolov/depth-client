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
            SwapLabels();

        }

        public void SwapLabels()
        {
            LanguageController lc = (DataContext as MainViewModel).LangContr;

            // assign text to labels
            DisparityLBL.Content = lc.DisparityLBL;

            DecimateLBL.Content = lc.DecFilt;
            DecLinScaleLBL.Content = lc.LinSc;

            ThreshLBL.Content = lc.DeptFilt;
            ThMinLBL.Content = lc.Min;
            ThMaxLBL.Content = lc.Max;

            DisparityLBL.Content = lc.DisparityLBL;

            SpatialLBL.Content = lc.SpatFilt;
            SpIterLBL.Content = lc.It;
            SpAlphaLBL.Content = lc.AlphaSp;
            SpDeltaLBL.Content = lc.DeltaSp;
            SpHoleLBL.Content = lc.HoleSp;

            TemporalLBL.Content = lc.TempFilt;
            TempAlphaLBL.Content = lc.AlphaTemp;
            TempDeltaLBL.Content = lc.DeltaTemp;

            TempPersLBL.Content = lc.Pers;

            HoleLBL.Content = lc.HoleFilt;
            HoleTypeLBL.Content = lc.Method;

            DecLinScaleLBL.ToolTip = lc.DecLinScaleLBLToolTip;
            DisparityLBL.ToolTip = lc.DisparityLBLToolTip;
            SpAlphaLBL.ToolTip = lc.SpAlphaLBLToolTip;
            SpDeltaLBL.ToolTip = lc.SpDeltaLBLToolTip;
            TempAlphaLBL.ToolTip = lc.TempAlphaLBLToolTip;
            TempDeltaLBL.ToolTip = lc.TempDeltaLBLToolTip;
            TempPersLBL.ToolTip = lc.TempPersLBLToolTip;
            PersIndex0.ToolTip = lc.PersIndex0ToolTip;
            PersIndex1.ToolTip = lc.PersIndex1ToolTip;
            PersIndex2.ToolTip = lc.PersIndex2ToolTip;
            PersIndex3.ToolTip = lc.PersIndex3ToolTip;
            PersIndex4.ToolTip = lc.PersIndex4ToolTip;
            PersIndex5.ToolTip = lc.PersIndex5ToolTip;
            PersIndex6.ToolTip = lc.PersIndex6ToolTip;
            PersIndex7.ToolTip = lc.PersIndex7ToolTip;
            PersIndex8.ToolTip = lc.PersIndex8ToolTip;
            HoleMethod0.ToolTip = lc.HoleMethod0ToolTip;
            HoleMethod1.ToolTip = lc.HoleMethod1ToolTip;
            HoleMethod2.ToolTip = lc.HoleMethod2ToolTip;

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
