using System.Windows;
using System.Windows.Controls;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.Ui
{
    /// <summary>
    /// Interaction logic for FilterConfigurationWindow.xaml
    /// </summary>
    public partial class FilterConfigurationWindow : Window
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dc"> Main view model from main window </param>
        public FilterConfigurationWindow(MainViewModel dc)
        {
            InitializeComponent();
            DataContext = dc;
            SwapLabels();

            dc.OnFilterChange();
        }

        /// <summary>
        /// Swap labels when changing language
        /// </summary>
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
            SpHoleLBL.ToolTip = lc.SpHoleLBLToolTip;
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

        /// <summary>
        /// Changed hole filter method
        /// </summary>
        /// <param name="sender"> Sender </param>
        /// <param name="e"> Arguments </param>
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
            dc.FiltData.HoleMethod = val;

            HoleDropdown.IsOpen = false;
            HoleDropdown.Content = content;
        }

        /// <summary>
        /// Change persistency method
        /// </summary>
        /// <param name="sender"> Sender </param>
        /// <param name="e"> Arguments </param>
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
            dc.FiltData.PersIndex = val;

            PersistencyDropdown.IsOpen = false;
            PersistencyDropdown.Content = content;
        }
    }

}
