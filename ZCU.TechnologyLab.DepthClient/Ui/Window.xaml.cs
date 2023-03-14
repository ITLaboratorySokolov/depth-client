using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ZCU.TechnologyLab.DepthClient.Ui;
using ZCU.TechnologyLab.DepthClient.ViewModels;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace Intel.RealSense
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class ProcessingWindow : Window
    {
        /// <summary> Token source </summary>
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        /// <summary> Freeze depth frame </summary>
        static volatile bool freezeDepth = false;
        /// <summary> Processing window </summary>
        public static ProcessingWindow InstanceWindow;
        /// <summary> Is advanced setting opened </summary>
        private bool settingsOpened;
        /// <summary> Filter configuration window </summary>
        private FilterConfigurationWindow confWindow;
        /// <summary> Allow exiting app </summary>
        private bool allowExit = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public ProcessingWindow()
        {
            InstanceWindow = this;
            // InitializeComponent();

            try
            {
                //var updateDepth = UpdateImageDepth();
                var updateColor = UpdateImageColor();

                var token = tokenSource.Token;

                // start a task
                var t = Task.Factory.StartNew(() =>
                {
                    RealS.DepthFrameBuffer buffer = new();

                    // main loop
                    while (!token.IsCancellationRequested)
                    {
                        if (!RealS.Started)
                            continue;
                        
                        // get depth frame
                        using var frames = RealS.DepthFrame.Obtain(buffer);
                        if (!frames.HasValue)
                            continue;
                        var frame = frames.Value;

                        // render depth color
                        Dispatcher.Invoke(DispatcherPriority.Render, (Action<int, int>)ResizeImageSrc, frame.Width, frame.Height);
                        Dispatcher.Invoke(DispatcherPriority.Render, updateColor, frame);
                    }

                    // close window properly
                    Action action = () => Close();
                    Dispatcher.Invoke(DispatcherPriority.Input, action);
                }, token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Shutdown();
            }

            InitializeComponent();
            LanguageSwap_Click(null, null);
        }

        /// <summary>
        /// Resize image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void ResizeImageSrc(int width, int height)
        {
            if (imgColor1.Source != null && Math.Abs(imgColor1.Source.Width - width) < 0.01)
                return;
            imgColor1.Source = new WriteableBitmap(width, height, 96d, 96d, PixelFormats.Rgb24, null);
        }

        /// <summary>
        /// Update coloured depth image
        /// </summary>
        /// <returns></returns>
        Action<RealS.DepthFrame> UpdateImageColor()
        {
            return frame =>
            {
                if (freezeDepth)
                    return;

                var target = imgColor1.Source as WriteableBitmap;
                var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                target.WritePixels(rect, frame.ColorData, frame.ColorStride, 0);
            };
        }

        public static void FreezeBuffer(bool b)
        {
            freezeDepth = b;
        }

        /// <summary>
        /// Close window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_Closing(object sender, CancelEventArgs e)
        {
            if (allowExit)
                return;

            e.Cancel = true;

            //main video feed has not been yet finished
            if (!tokenSource.IsCancellationRequested)
            {
                //schedule video feed closing and cancel the closing event
                tokenSource.Cancel();
                return;
            }

            var t = new Thread(() =>
            {
                //video feed has finished we can close connection and window
                RealS.Exit();
                allowExit = true;

                Action action = () => Close();
                Dispatcher.Invoke(DispatcherPriority.Normal, action);
                Console.WriteLine("Exit");
            });
            t.Start();
        }

        /// <summary>
        /// Change server URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerURL_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var ee = sender as TextBox;
            MainViewModel.ServerUrl = ee.Text;
        }

        /// <summary>
        /// Reset view on 3D preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(Hell.Camera.Position);
            Console.WriteLine(Hell.Camera.LookDirection);
            Hell.SetView(new Point3D(18, -0.8, 0.8), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1), 1000);
        }

        /// <summary>
        /// Turn on/off auto send
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoMenu_OnClick(object sender, RoutedEventArgs e)
        {
            var dc = DataContext as MainViewModel;
            dc.OnChangeAuto();
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]+$");
            e.Handled = regex.IsMatch(e.Text);
            if (e.Handled && int.Parse(e.Text) == 0)
                e.Handled = false;
        }

        /// <summary>
        /// Changing auto send interval
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumericUpDown_OnValueChanged(object? sender, EventArgs e)
        {
            var dc = DataContext as MainViewModel;
            var s = sender as NumericUpDown;

            dc.AutoInterval = (int)s.Value;
        }

        /// <summary>
        /// User code text changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CodeBlock_TextChanged(object sender, EventArgs e)
        {
            // TextRange textRange = new TextRange( CodeBlock.Document.ContentStart, CodeBlock.Document.ContentEnd);

            var dc = DataContext as MainViewModel;
            dc.UserCode = CodeBlock.Text; // textRange.Text;
        }

        /// <summary>
        /// Open setting client name dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetName_Click(object sender, RoutedEventArgs e)
        {
            var dc = DataContext as MainViewModel;
            var langCont = dc.LangContr;

            NameDialog inputDialog = new NameDialog(langCont.NameQuestion, dc.ClientName);
            if (inputDialog.ShowDialog() == true)
            {
                dc.ClientName = inputDialog.Answer;
                dc.Message = langCont.NameChange + dc.ClientName;
            }
        }

        /// <summary>
        /// Swap languages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageSwap_Click(object sender, RoutedEventArgs e)
        {
            var dc = DataContext as MainViewModel;
            var langCont = dc.LangContr;
            langCont.SwapLanguage();

            ConnectMNI.Header = langCont.GetConnectText(ConnectMNI.Header.ToString());

            FileMN.Header = langCont.FileHeader;
            BagMNI.Header = langCont.OpenBAG;
            CameraMNI.Header = langCont.OpenCamera;
            PlyMNI.Header = langCont.SavePLY;

            SendMeshMNI.Header = langCont.SendMeshMNI;
            DeleteMeshMNI.Header = langCont.DeleteMeshMNI;
            DwnldMeshMNI.Header = langCont.DwnldMeshMNI;

            PythonPathMNI.Header = langCont.PythonPathMNI;

            SettingsMN.Header = langCont.SettingsMN;
            LanguageMNI.Header = langCont.LanguageMNI;
            NameMNI.Header = langCont.SetNameMNI;

            ServerMN.Header = langCont.ServerMN;

            SettingsMN.Header = langCont.SettingsMN;

            ApplyCodeBT.Content = langCont.ApplyCodeBT;
            SnapshotBT.Content = langCont.SnapshotBT;
            ResetBT.Content = langCont.ResetBT;

            DecimateLBL.Content = langCont.DecimateLBL;
            ThresholdLBL.Content = langCont.ThresholdLBL;
            
            SmoothingLBL.Content = langCont.Smoothing;
            SpatialLBL.Content = langCont.SpatialLBL;
            TemporalLBL.Content = langCont.TemporalLBL;
            VerticesLBL.Content = langCont.VerticesLBL;

            TriangleThLBL.Content = langCont.TriangleThLBL;
            FilterSettingsBT.Content = langCont.FilterSettingsBT;
            HoleLBL.Content = langCont.HoleLBL;

            DecimateLBL.ToolTip = langCont.DecimateLBLTooltip;
            ThresholdLBL.ToolTip = langCont.ThresholdLBLTooltip;
            HoleLBL.ToolTip = langCont.HoleLBLTooltip;
            SpatialLBL.ToolTip = langCont.SpatialLBLToolTip;
            TemporalLBL.ToolTip = langCont.TemporalLBLToolTip;
            TriangleThLBL.ToolTip = langCont.TriangleThLBLToolTip;

            if (confWindow != null)
                confWindow.SwapLabels();
        }

        /// <summary>
        /// Open advanced filter settings window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterSettingsBT_Click(object sender, RoutedEventArgs e)
        {
            if (settingsOpened)
                return;

            settingsOpened = true;
            var dc = DataContext as MainViewModel;
            var langCont = dc.LangContr;

            confWindow = new FilterConfigurationWindow(dc);
            confWindow.Show();
            confWindow.Closing += ClosingSettings;
        }

        /// <summary>
        /// Reaction to closing advanced filter settings window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClosingSettings(object sender, CancelEventArgs e)
        {
            settingsOpened = false;
            confWindow = null;
        }
    }
}