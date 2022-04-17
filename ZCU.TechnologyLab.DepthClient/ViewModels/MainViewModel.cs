using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using Intel.RealSense;
using Microsoft.Win32;
using Newtonsoft.Json;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Connections;
using ZCU.TechnologyLab.Common.Connections.Session;
using ZCU.TechnologyLab.Common.Serialization;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public class MainViewModel : NotifyingClass
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();


        private ServerConnection serverConnection;

        private ISessionClient sessionClient;


        private const string MESSAGE_PROPERTY = "Message";

        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";

        public const string MESH_NAME = "DepthMesh";
        public const string MESH_TYPE = "Mesh";
        public const string PLY_NAME = "DepthPly";
        public const string PLY_TYPE = "PlyFile";
        public const string MODEL = "Model";


        /// <summary>
        /// Message.
        /// </summary>
        private string message;

        private string connectBtnLbl = "Connect";
        private bool enabledButtons = false;

        /// <summary>
        /// Multiple start calls throw an error so check if it is already connected.
        /// </summary>
        private bool connected;

        /// <summary>
        /// Initialize commands and variables.
        /// </summary>
        public MainViewModel()
        {
            AllocConsole();

            // Console.WriteLine("before initn");

            // RealSenseWrapper.Start();
            /*RealSenseWrapper.Start("d435i_walk_around.bag");

           
            RealSenseWrapper.Exit();*/
            //   Console.WriteLine("after initn");


            this.Connect = new Command(this.OnConnect);
            this.SendMesh = new Command(this.OnSendMesh);
            this.RemoveMesh = new Command(this.OnRemoveImage);
            this.OpenRealSenseFile = new Command(this.OnOpenRealSenseFile);
            this.ConnectCamera = new Command(this.OnConnectCamera);
            this.SavePly = new Command(this.OnSavePly);
            this.ReloadMesh = new Command(this.OnReloadMesh);

            OnConnectCamera();
            PrepareModel();

            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        private void OnReloadMesh()
        {
            if (!RealS.Started)
                return;

            using var frames = RealS.MeshFrame.Obtain();
            if (!frames.HasValue)
            {
                Message = "No mesh available";
                return;
            }

            var frame = frames.Value;
            BuildModelFromFrame(frame);
        }

        private Material blueMat = MaterialHelper.CreateMaterial(Colors.Blue);

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var mo = (Model3DGroup)Model;
            if (mo != null && mo.Children.Count != 0)
            {
                var m = ((Model3DGroup)Model).Children[0];
                if (this.angle >= 360)
                    this.angle = 0;

                this.angle += 1;
                //You can adapt the code if you have many children 
                GeometryModel3D geometryModel3D = (GeometryModel3D)m;
                if (geometryModel3D.Transform is RotateTransform3D rotateTransform3 &&
                    rotateTransform3.Rotation is AxisAngleRotation3D rotation)
                {
                    // rotation.Angle = this.angle;
                }
                else
                {
                    Transform3DGroup transforms = new();

                    transforms.Children.Add(new RotateTransform3D()
                    {
                        Rotation = new AxisAngleRotation3D()
                        {
                            Axis = new Vector3D(0, 1, 0),
                            Angle = -90
                        },
                        CenterX = 0,
                        CenterY = 0,
                        CenterZ = 0.5
                    });
                    transforms.Children.Add(new RotateTransform3D()
                    {
                        Rotation = new AxisAngleRotation3D()
                        {
                            Axis = new Vector3D(1, 0, 0),
                            Angle = -90
                        },
                        CenterX = 0,
                        CenterY = 0,
                        CenterZ = 0.5
                    });
                    transforms.Children.Add(new ScaleTransform3D(3, 3, 3, 0, 0, 0.5));
                    geometryModel3D.Transform = transforms;
                }
            }
        }

        public static BitmapImage ConvertBitmapSourceToBitmapImage(
            BitmapSource bitmapSource)
        {
            // before encoding/decoding, check if bitmapSource is already a BitmapImage

            if (!(bitmapSource is BitmapImage bitmapImage))
            {
                bitmapImage = new BitmapImage();

                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                }
            }

            return bitmapImage;
        }

        private void BuildModelFromFrame(RealS.MeshFrame frame)
        {
            Stopwatch watch = new Stopwatch();
            watch.Restart();
            MeshGeometry3D d = new MeshGeometry3D();
            for (int i = 0; i < frame.Vertices.Length; i += 3)
            {
                Point3D p = new(
                    frame.Vertices[i],
                    frame.Vertices[i + 1],
                    frame.Vertices[i + 2]);
                d.Positions.Add(p);
                d.TextureCoordinates.Add(new Point(frame.UVs[i / 3 * 2], frame.UVs[i / 3 * 2 + 1]));
            }

            foreach (int t in frame.Faces)
                d.TriangleIndices.Add(t);


            Console.WriteLine("Elapsed for mesh build " + watch.ElapsedMilliseconds);

            var m = FromArray(
                frame.Colors,
                frame.Width,
                frame.Colors.Length / 3 / frame.Width,
                3);


            // Create some materials
            var texture = MaterialHelper.CreateImageMaterial(ConvertBitmapSourceToBitmapImage(m), 1);
            PrepareModel(d, texture);
        }

        private void PrepareModel()
        {
            // Create a mesh builder and add a box to it
            var meshBuilder = new MeshBuilder(false, false);
            meshBuilder.AddBox(new Point3D(0, 0, 1), 1, 2, 0.5);
            meshBuilder.AddBox(new Rect3D(0, 0, 1.2, 0.5, 1, 0.4));
            // Create a mesh from the builder (and freeze it)
            var mesh = meshBuilder.ToMesh(true);

            PrepareModel(mesh, MaterialHelper.CreateMaterial(Colors.Green));
            return;

            var modelGroup = new Model3DGroup();


            // Create some materials
            var greenMaterial = MaterialHelper.CreateMaterial(Colors.Green);
            var redMaterial = MaterialHelper.CreateMaterial(Colors.Red);
            var blueMaterial = MaterialHelper.CreateMaterial(Colors.Blue);
            var insideMaterial = MaterialHelper.CreateMaterial(Colors.Yellow);

            // Add 3 models to the group (using the same mesh, that's why we had to freeze it)
            modelGroup.Children.Add(new GeometryModel3D
                { Geometry = mesh, Material = greenMaterial, BackMaterial = insideMaterial });
            modelGroup.Children.Add(new GeometryModel3D
            {
                Geometry = mesh, Transform = new TranslateTransform3D(-2, 0, 0), Material = redMaterial,
                BackMaterial = insideMaterial
            });
            modelGroup.Children.Add(new GeometryModel3D
            {
                Geometry = mesh, Transform = new TranslateTransform3D(2, 0, 0), Material = blueMaterial,
                BackMaterial = insideMaterial
            });
            // Set the property, which will be bound to the Content property of the ModelVisual3D (see MainWindow.xaml)

            ModelImporter importer = new ModelImporter();
            modelGroup = importer.Load(@"C:/Users/minek/Desktop/killme.obj");
            // Console.WriteLine("Updatred model");
            this.Model = modelGroup;
        }

        public static BitmapSource FromArray(byte[] data, int w, int h, int ch)
        {
            PixelFormat format = PixelFormats.Default;

            if (ch == 1) format = PixelFormats.Gray8; //grey scale image 0-255
            if (ch == 3) format = PixelFormats.Bgr24; //RGB
            if (ch == 4) format = PixelFormats.Bgr32; //RGB + alpha


            WriteableBitmap wbm = new WriteableBitmap(w, h, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, w, h), data, ch * w, 0);

            return wbm;
        }

        private void PrepareModel(MeshGeometry3D mesh, Material frontMaterial)
        {
            var modelGroup = new Model3DGroup();


            var insideMaterial = MaterialHelper.CreateMaterial(Colors.CadetBlue);

            //if(!mesh.IsFrozen)
            //     mesh.Normals = mesh.CalculateNormals();

            // Add 3 models to the group (using the same mesh, that's why we had to freeze it)
            modelGroup.Children.Add(new GeometryModel3D
                { Geometry = mesh, Material = frontMaterial, BackMaterial = insideMaterial });

            // Set the property, which will be bound to the Content property of the ModelVisual3D (see MainWindow.xaml)
            this.Model = modelGroup;
        }


        private Model3D model;

        /// <summary>
        /// Gets and sets a message.
        /// </summary>
        public string Message
        {
            get => this.message;
            set
            {
                this.message = value;
                RaisePropertyChanged(MESSAGE_PROPERTY);
            }
        }

        public bool SavePlyBtnEnable
        {
            get => this.savePlyBtnEnable;
            set
            {
                this.savePlyBtnEnable = value;
                RaisePropertyChanged(SAVE_PLY_BTN_PROPERTY);
            }
        }

        public string ConnectBtnLbl
        {
            get => this.connectBtnLbl;
            set
            {
                this.connectBtnLbl = value;
                RaisePropertyChanged(CNTCBTLB_PROPERTY);
            }
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public Model3D Model
        {
            get => model;
            set
            {
                model = value;
                RaisePropertyChanged(MODEL);
            }
        }

        public static string ServerUrl { get; set; } = "https://localhost:49153/";

        public bool EnabledButtons
        {
            get => this.enabledButtons;
            set
            {
                this.enabledButtons = value;
                RaisePropertyChanged(EN_BTN_PROPERTY);
            }
        }

        /// <summary>
        /// Connect to server command
        /// </summary>
        public ICommand Connect { get; private set; }

        public ICommand SendMesh { get; private set; }
        public ICommand RemoveMesh { get; private set; }
        public ICommand OpenRealSenseFile { get; private set; }
        public ICommand ConnectCamera { get; private set; }

        public ICommand SavePly { get; private set; }
        public ICommand ReloadMesh { get; private set; }


        private bool[] filters = Enumerable.Repeat(true, 5).ToArray();

        public bool Filter0
        {
            get => filters[0];
            set
            {
                this.filters[0] = value;
                OnFilterChange();
            }
        }

        public bool Filter1
        {
            get => filters[1];
            set
            {
                this.filters[1] = value;
                OnFilterChange();
            }
        }

        public bool Filter2
        {
            get => filters[2];
            set
            {
                this.filters[2] = value;
                OnFilterChange();
            }
        }

        public bool Filter3
        {
            get => filters[3];
            set
            {
                this.filters[3] = value;
                OnFilterChange();
            }
        }

        public bool Filter4
        {
            get => filters[4];
            set
            {
                this.filters[4] = value;
                OnFilterChange();
            }
        }


        private void OnFilterChange()
        {
            Console.WriteLine("Updating filters");
            RealS.updateFilters(filters);
        }


        private bool inConnectProcess = false;
        private bool savePlyBtnEnable;
        private readonly DispatcherTimer timer;
        private double angle;

        /// <summary>
        /// Connects connection to a server.
        /// </summary>
        private async void OnConnect()
        {
            if (inConnectProcess)
                return;
            inConnectProcess = true;
            if (!ServerUrl.EndsWith("/"))
                ServerUrl += "/";
            if (!this.connected)
            {
                this.ConnectBtnLbl = "Connecting";
                try
                {
                    if (sessionClient is { SessionState: SessionState.Connected })
                        await sessionClient.StopSessionAsync();
                    sessionClient = null;


                    sessionClient = new SignalRSession(ServerUrl, "virtualWorldHub");
                    if (sessionClient == null)
                        throw new Exception();
                    serverConnection = new ServerConnection(this.sessionClient);

                    serverConnection.AllWorldObjectsReceived += OnAllWorldObjectsReceived;

                    await this.sessionClient.StartSessionAsync();
                    this.Message = "Connected to server: " + ServerUrl;
                }
                catch (Exception e)
                {
                    this.ConnectBtnLbl = "Connect";
                    this.Message = "Cannot connect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                //ProcessingWindow.FreezeBuffer(false);

                try
                {
                    await this.sessionClient.StopSessionAsync();
                }
                catch (Exception e)
                {
                    this.Message = "Cannot disconnect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }

            if (sessionClient == null)
            {
                inConnectProcess = false;
                return;
            }

            if ((!this.connected && this.sessionClient.SessionState == SessionState.Connected)
                || (this.connected && this.sessionClient.SessionState == SessionState.Closed)
               )
            {
                this.connected = !this.connected;

                this.ConnectBtnLbl = this.connected ? "Disconnect" : "Connect";
                EnabledButtons = this.connected;

                if (this.connected)
                {
                    // Console.WriteLine("Asking for worldobjects");
                    await serverConnection.GetAllWorldObjectsAsync();
                }
            }

            inConnectProcess = false;
        }

        /// Changes message when the SignalR client is disconnected.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Exception that caused the disconnection.</param>
        private void OnAllWorldObjectsReceived(List<WorldObjectDto> list)
        {
            // this.Message = "Disconnected: " + e.Message;
        }

        private async void OnSendMesh()
        {
            Console.WriteLine("Parsing mesh");
            WorldObjectDto w = new()
            {
                Name = MESH_NAME,
                Type = MESH_TYPE
            };

            {
                using var frames = RealS.MeshFrame.Obtain();
                if (!frames.HasValue)
                {
                    Message = "No mesh available";
                    return;
                }

                var frame = frames.Value;

                BuildModelFromFrame(frame);
                w.Properties =
                    new MeshSerializer().SerializeProperties(frame.Vertices, frame.Faces, "Triangle");


                // adding mesh file

                //    await this.serverConnection.AddWorldObjectAsync(w);

                Console.WriteLine("Mesh not sent Sent");


                //adding ply file to server
                w = new()
                {
                    Name = PLY_NAME,
                    Type = PLY_TYPE
                };
                w.Properties = new Dictionary<string, string>
                {
                    ["data"] = Convert.ToBase64String(frame.Ply)
                };

                //await this.serverConnection.AddWorldObjectAsync(w);
                Message = "Mesh & Ply File Sent to server as " + MESH_NAME + "," + PLY_NAME;
            }
        }

        private async void OnRemoveImage()
        {
            await this.serverConnection.RemoveWorldObjectAsync(MESH_NAME);
            await this.serverConnection.RemoveWorldObjectAsync(PLY_NAME);
            this.Message = "Ply & Mesh removed from server";
        }

        private void OnOpenRealSenseFile()
        {
            // freezeDepth = true;


            OpenFileDialog openDialog = new OpenFileDialog();

            openDialog.Filter = "bag files (*.bag)|*.bag|All files (*.*)|*.*";
            openDialog.FilterIndex = 1;
            //openDialog.RestoreDirectory = true;
            openDialog.CheckFileExists = true;
            openDialog.InitialDirectory = "";
            var success = openDialog.ShowDialog();
            if (success != null && success.Value)
            {
                if (!File.Exists(openDialog.FileName))
                    return;
                var t = new Thread(() =>
                {
                    var success = RealS.Init(openDialog.FileName);
                    Message = success
                        ? "Opened RealSense file"
                        : "Could not open file";

                    SavePlyBtnEnable = success;

                    Console.WriteLine(Message + ": " + openDialog.FileName);
                });
                t.Start();
            }

            // freezeDepth = false;
        }

        private void OnConnectCamera()
        {
            var t = new Thread(() =>
            {
                var success = RealS.Init("");
                Message = success ? "Camera connected" : "Camera not found";
                SavePlyBtnEnable = success;
            });
            t.Start();
        }

        private void OnSavePly()
        {
            if (!RealS.Started)
                return;

            ProcessingWindow.FreezeBuffer(true);

            using var frames = RealS.MeshFrame.Obtain();
            if (!frames.HasValue)
                return;
            var frame = frames.Value;


            BuildModelFromFrame(frame);

            SaveFileDialog saveDialog = new SaveFileDialog();

            saveDialog.Filter = "ply files (*.ply)|*.ply|All files (*.*)|*.*";
            saveDialog.FilterIndex = 1;
            saveDialog.InitialDirectory = "";


            var success = saveDialog.ShowDialog();
            if (success.HasValue && success.Value)
            {
                using var myStream = saveDialog.OpenFile();
                myStream.Write(frame.Ply);
            }

            Message = "Ply file saved";
            Console.WriteLine("Saved ply file to " + saveDialog.FileName);

            ProcessingWindow.FreezeBuffer(false);
            saveDialog.Reset();
        }
    }
}