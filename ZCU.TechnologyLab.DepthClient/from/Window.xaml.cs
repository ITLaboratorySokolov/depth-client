using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        private Pipeline pipeline = new Pipeline();
        private Colorizer colorizer = new Colorizer();
        private Align align = new Align(Stream.Color);
        private CustomProcessingBlock block;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public static ushort[] depth_buffer = new ushort[640 * 360];
        public static ushort[] depth_buffer_live = new ushort[640 * 360];
        public static float DepthScale { get; private set; }
        public static int DepthWidth { get; private set; }
        public static int DepthHeight { get; private set; }

        static Action<VideoFrame> UpdateImageRGB(Image img)
        {
            var wbmp = img.Source as WriteableBitmap;
            return frame =>
            {
                var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                wbmp.WritePixels(rect, frame.Data, frame.Stride * frame.Height, frame.Stride);
            };
        }

        static bool freezeDepth=false;

        private static void MultiplyBitmap(WriteableBitmap wbmp)
        {
            var rect = new Int32Rect(0, 0, wbmp.PixelWidth, wbmp.PixelHeight);

            unsafe
            {

                wbmp.Lock();

                ushort* ss = (ushort*)wbmp.BackBuffer;

                for (int i = 0; i < wbmp.PixelHeight * wbmp.PixelWidth; i++)
                {
                    *ss = (ushort)((*ss) * 10);
                    ss++;
                }

                wbmp.AddDirtyRect(rect);
                wbmp.Unlock();

            }
        }

        static Action<VideoFrame> UpdateImageDepth(Image img)
        {
            var wbmp = img.Source as WriteableBitmap;
            Console.WriteLine("Killme " + wbmp.Format);
            return frame =>
            {
                //Console.WriteLine("Frame depth stride length in bytes + data si"+frame.Stride+" " + frame.DataSize);
                frame.CopyTo(depth_buffer_live);

                var rect = new Int32Rect(0, 0, frame.Width, frame.Height);
                
                if(freezeDepth)
                {
                    wbmp.WritePixels(rect, depth_buffer, frame.Stride, 0);
                    MultiplyBitmap(wbmp);
                    return;
                }

                wbmp.WritePixels(rect, frame.Data, frame.Stride * frame.Height, frame.Stride);
                MultiplyBitmap(wbmp);

                DepthWidth = frame.Width;
                DepthHeight = frame.Height;
            };
        }

        public static void FreezeBuffer(bool b)
        {
            freezeDepth = b;
        }

        public static ProcessingWindow InstanceWindow;

        public ProcessingWindow()
        {
            InstanceWindow = this;
            InitializeComponent();
            try
            {
                var cfg = new Config();
                cfg.EnableDeviceFromFile(@"C:/D/Uni/gymso/d435i_sample_data/d435i_walk_around.bag", true);

                using (var ctx = new Context())
                {
                    var devices = ctx.QueryDevices();
                    //var dev = devices[0];
                    var dev = PlaybackDevice.FromDevice(
                        ctx.AddDevice(@"C:/D/Uni/gymso/d435i_sample_data/d435i_walk_around.bag"));

                    Console.WriteLine("\nUsing device 0, an {0}", dev.Info[CameraInfo.Name]);
                    Console.WriteLine("    Serial number: {0}", dev.Info[CameraInfo.SerialNumber]);
                    Console.WriteLine("    Firmware version: {0}", dev.Info[CameraInfo.FirmwareVersion]);

                    var sensors = dev.QuerySensors();
                    var depthSensor = sensors[0];
                    var colorSensor = sensors[1];
                    DepthScale = depthSensor.DepthScale;

                    var depthProfile = depthSensor.StreamProfiles
                        .Where(p => p.Stream == Stream.Depth)
                        .OrderBy(p => p.Framerate)
                        .Select(p => p.As<VideoStreamProfile>()).First();

                    var colorProfile = colorSensor.StreamProfiles
                        .Where(p => p.Stream == Stream.Color)
                        .OrderBy(p => p.Framerate)
                        .Select(p => p.As<VideoStreamProfile>()).First();
                    Console.WriteLine("Format of depth " + depthProfile.Format);
                    cfg.EnableStream(Stream.Depth, depthProfile.Width, depthProfile.Height, depthProfile.Format,
                        depthProfile.Framerate);
                    cfg.EnableStream(Stream.Color, colorProfile.Width, colorProfile.Height, colorProfile.Format,
                        colorProfile.Framerate);
                }

                var pp = pipeline.Start(cfg);

                // Get the recommended processing blocks for the depth sensor
                var sensor = pp.Device.QuerySensors().First(s => s.Is(Extension.DepthSensor));
                var blocks = sensor.ProcessingBlocks.ToList();

                // Allocate bitmaps for rendring.
                // Since the sample aligns the depth frames to the color frames, both of the images will have the color resolution
                using (var p = pp.GetStream(Stream.Color).As<VideoStreamProfile>())
                {
                    imgColor.Source = new WriteableBitmap(p.Width, p.Height, 96d, 96d, PixelFormats.Rgb24, null);
                }

                using (var p = pp.GetStream(Stream.Depth).As<VideoStreamProfile>())
                {
                    Console.WriteLine("depth dimensions" +p.Width/2+" "+p.Height/2);
                    imgDepth1.Source = new WriteableBitmap(p.Width/2, p.Height/2, 96d, 96d, PixelFormats.Gray16, null);
                }

                var updateColor = UpdateImageRGB(imgColor);
                var updateDepth = UpdateImageDepth(imgDepth1);
                // Create custom processing block
                // For demonstration purposes it will:
                // a. Get a frameset
                // b. Run post-processing on the depth frame
                // c. Combine the result back into a frameset
                // Processing blocks are inherently thread-safe and play well with
                // other API primitives such as frame-queues, 
                // and can be used to encapsulate advanced operations.
                // All invocations are, however, synchronious so the high-level threading model
                // is up to the developer
                block = new CustomProcessingBlock((f, src) =>
                {
                    // We create a FrameReleaser object that would track
                    // all newly allocated .NET frames, and ensure deterministic finalization
                    // at the end of scope. 
                    using (var releaser = new FramesReleaser())
                    {
                        foreach (ProcessingBlock p in blocks)
                            f = p.Process(f).DisposeWith(releaser);

                        //f = f.ApplyFilter(align).DisposeWith(releaser);
                        //f = f.ApplyFilter(colorizer).DisposeWith(releaser);

                        var frames = f.As<FrameSet>().DisposeWith(releaser);

                        var colorFrame = frames[Stream.Color, Format.Rgb8].DisposeWith(releaser);
                        //var colorizedDepth = frames[Stream.Depth, Format.Rgb8].DisposeWith(releaser);
                        var colorizedDepth = frames[Stream.Depth, Format.Z16].DisposeWith(releaser);

                        // Combine the frames into a single result
                        var res = src.AllocateCompositeFrame(colorizedDepth, colorFrame).DisposeWith(releaser);
                        // Send it to the next processing stage
                        src.FrameReady(res);
                    }
                });

                // Register to results of processing via a callback:
                block.Start(f =>
                {
                    using (var frames = f.As<FrameSet>())
                    {
                        var colorFrame = frames.ColorFrame.DisposeWith(frames);
                        //var colorizedDepth = frames.First<VideoFrame>(Stream.Depth, Format.Rgb8).DisposeWith(frames);
                        var colorizedDepth = frames.DepthFrame.DisposeWith(frames);

                        Dispatcher.Invoke(DispatcherPriority.Render, updateDepth, colorizedDepth);
                        Dispatcher.Invoke(DispatcherPriority.Render, updateColor, colorFrame);
                    }
                });

                var token = tokenSource.Token;

                var t = Task.Factory.StartNew(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        using (var frames = pipeline.WaitForFrames())
                        {
                            // Invoke custom processing block
                            block.Process(frames);
                        }
                    }
                }, token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Shutdown();
            }

            InitializeComponent();
        }

        private void control_Closing(object sender, CancelEventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}