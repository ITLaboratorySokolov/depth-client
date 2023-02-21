﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Microsoft.Win32;
using ZCU.TechnologyLab.DepthClient.Ui;
using ZCU.TechnologyLab.DepthClient.ViewModels;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

// This demo showcases some of the more advanced API concepts:
// a. Post-processing and stream alignment
// b. Using callbacks
// c. Defining custom processing blocks
// d. Using FramesReleaser to help manage frames lifetime
namespace Intel.RealSense
{
    /// <summary>
    /// Interaction logic for Window.xaml
    /// </summary>
    public partial class ProcessingWindow : Window
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        static volatile bool freezeDepth = false;
        public static ProcessingWindow InstanceWindow;
        private bool settingsOpened;
        private FilterConfigurationWindow confWindow;

        private void ResizeImageSrc(int width, int height)
        {
            if (imgColor1.Source != null && Math.Abs(imgColor1.Source.Width - width) < 0.01)
                return;
            imgColor1.Source = new WriteableBitmap(width, height, 96d, 96d, PixelFormats.Rgb24, null);
            // imgDepth1.Source = new WriteableBitmap(width, height, 96d, 96d, PixelFormats.Gray16, null);
        }

        private static void MultiplyBitmap(WriteableBitmap wbmp)
        {
            var rect = new Int32Rect(0, 0, wbmp.PixelWidth, wbmp.PixelHeight);

            unsafe
            {
                unchecked
                {
                    wbmp.Lock();

                    ushort* ss = (ushort*)wbmp.BackBuffer;

                    for (int i = 0; i < wbmp.PixelHeight * wbmp.PixelWidth; i++)
                    {
                        //*ss = (ushort)((*ss<<3));
                        ss++;
                    }
                }

                wbmp.AddDirtyRect(rect);
                wbmp.Unlock();
            }
        }


        Action<RealS.DepthFrame> UpdateImageDepth()
        {
            return frame =>
            {
                if (freezeDepth)
                    return;

                // var target = imgDepth1.Source as WriteableBitmap;
                //  var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                // target.WritePixels(rect, frame.Data, frame.Stride, 0);
                //MultiplyBitmap(target);
            };
        }

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

        public ProcessingWindow()
        {
            InstanceWindow = this;
            InitializeComponent();


            try
            {
                var updateDepth = UpdateImageDepth();
                var updateColor = UpdateImageColor();

                var token = tokenSource.Token;

                var t = Task.Factory.StartNew(() =>
                {
                    RealS.DepthFrameBuffer buffer = new();

                    while (!token.IsCancellationRequested)
                    {
                        if (!RealS.Started)
                            continue;
                        using var frames = RealS.DepthFrame.Obtain(buffer);


                        if (!frames.HasValue)
                            continue;

                        var frame = frames.Value;

                        Dispatcher.Invoke(DispatcherPriority.Render, (Action<int, int>)ResizeImageSrc, frame.Width,
                            frame.Height);

                        // Invoke custom processing block
                        Dispatcher.Invoke(DispatcherPriority.Render, updateDepth, frame);
                        Dispatcher.Invoke(DispatcherPriority.Render, updateColor, frame);
                    }

                    //close window properly
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
        }

        private bool allowExit = false;


        private void control_Closing(object sender, CancelEventArgs e)
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
                Console.WriteLine("Exit");
                allowExit = true;

                Action action = () => Close();
                ;
                Dispatcher.Invoke(DispatcherPriority.Normal, action);
            });
            t.Start();
        }


        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var ee = sender as TextBox;
            MainViewModel.ServerUrl = ee.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(Hell.Camera.Position);
            Console.WriteLine(Hell.Camera.LookDirection);
            Hell.SetView(new Point3D(18, -0.8, 0.8), new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1), 1000);
        }

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

        private void NumericUpDown_OnValueChanged(object? sender, EventArgs e)
        {
            var dc = DataContext as MainViewModel;
            var s = sender as NumericUpDown;

            dc.AutoInterval = (int)s.Value;
        }

        private void CodeBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextRange textRange = new TextRange( CodeBlock.Document.ContentStart, CodeBlock.Document.ContentEnd);

            var dc = DataContext as MainViewModel;
            dc.UserCode = textRange.Text;
        }

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

        private void LanguageSwap_Click(object sender, RoutedEventArgs e)
        {
            var dc = DataContext as MainViewModel;
            var langCont = dc.LangContr;
            langCont.SwapLanguage();

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

        public void ClosingSettings(object sender, CancelEventArgs e)
        {
            settingsOpened = false;
            confWindow = null;
        }
    }
}