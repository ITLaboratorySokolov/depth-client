using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Microsoft.Win32;
using ZCU.TechnologyLab.DepthClient.ViewModels;

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


        private void ResizeImageSrc(int width, int height)
        {
            if (imgColor1.Source != null && Math.Abs(imgColor1.Source.Width - width) < 0.01)
                return;
            imgColor1.Source = new WriteableBitmap(width, height, 96d, 96d, PixelFormats.Rgb24, null);
            imgDepth1.Source = new WriteableBitmap(width, height, 96d, 96d, PixelFormats.Gray16, null);
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

                var target = imgDepth1.Source as WriteableBitmap;
                var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                target.WritePixels(rect, frame.Data, frame.Stride, 0);
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
                    RealS.DepthFrameBuffer buffer=new();

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

                Action action = () => Close(); ;
                Dispatcher.Invoke(DispatcherPriority.Normal,action);
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
    }
}