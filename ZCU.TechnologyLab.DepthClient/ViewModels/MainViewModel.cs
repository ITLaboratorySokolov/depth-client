using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using Intel.RealSense;
using Microsoft.Win32;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Connections;
using ZCU.TechnologyLab.Common.Connections.Data;
using ZCU.TechnologyLab.Common.Connections.Session;
using ZCU.TechnologyLab.Common.Serialization;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public class MainViewModel : NotifyingClass
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();



        private const string MESSAGE_PROPERTY = "Message";

        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";

        public const string MESH_NAME = "DepthMesh";
        public const string PLY_NAME = "DepthPly";
        public const string THRESHOLD = "ThresholdSlider";
        public const string MODEL = "Model";
        private Model3D _model;


        private bool _inConnectProcess = false;
        private bool _savePlyBtnEnable;
        private readonly DispatcherTimer timer;
        private double _angle;

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
            get => this._savePlyBtnEnable;
            set
            {
                this._savePlyBtnEnable = value;
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
            get => _model;
            set
            {
                _model = value;
                RaisePropertyChanged(MODEL);
            }
        }

        private double _thresholdSlider;

        public void UpdateSliderModel()
        {
            if (meshBuffer != null)
            {
                Stopwatch wa = Stopwatch.StartNew();
                GenerateFaces(meshBuffer, (float)_thresholdSlider);

                meshGeo.TriangleIndices.Clear();
                Console.WriteLine(wa.ElapsedMilliseconds);

                meshGeo.TriangleIndices = new Int32Collection(meshBuffer.TempFaces);


                Console.WriteLine(wa.ElapsedMilliseconds);


                RaisePropertyChanged(MODEL);
            }
        }

        public double ThresholdSlider
        {
            get => _thresholdSlider;
            set
            {
                _thresholdSlider = value;
                UpdateSliderModel();
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
            DispatcherTimer timer;
            AllocConsole();

            // Console.WriteLine("before initn");

            // RealSenseWrapper.Start();
            /*RealSenseWrapper.Start("d435i_walk_around.bag");

           
            RealSenseWrapper.Exit();*/
            //   Console.WriteLine("after initn");

            this._thresholdSlider = 0.2;

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

        private RealS.MeshFrameBuffer meshBuffer;

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
            UpdateSliderModel();
        }

        private Material blueMat = MaterialHelper.CreateMaterial(Colors.Blue);

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var mo = (Model3DGroup)Model;
            if (mo != null && mo.Children.Count != 0)
            {
                var m = ((Model3DGroup)Model).Children[0];
                if (this._angle >= 360)
                    this._angle = 0;

                this._angle += 1;
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

        public static void GenerateFaces(RealS.MeshFrameBuffer frame, float threshold)
        {
            threshold *= threshold;

            Array.Copy(frame.Faces, frame.TempFaces, frame.Faces.Length);

            for (int i = 0; i < frame.TempFaces.Length; i += 3)
            {
                Vector3 v0 = new(new ReadOnlySpan<float>(frame.Vertices, frame.TempFaces[i + 0] * 3, 3));
                Vector3 v1 = new(new ReadOnlySpan<float>(frame.Vertices, frame.TempFaces[i + 1] * 3, 3));
                Vector3 v2 = new(new ReadOnlySpan<float>(frame.Vertices, frame.TempFaces[i + 2] * 3, 3));

                if ((v0 - v1).LengthSquared() > threshold
                    || (v1 - v2).LengthSquared() > threshold
                    || (v2 - v0).LengthSquared() > threshold)
                {
                    frame.TempFaces[i + 0] = 0;
                    frame.TempFaces[i + 1] = 0;
                    frame.TempFaces[i + 2] = 0;
                }
            }
        }


        private void BuildModelFromFrame(RealS.MeshFrame frame)
        {
            meshBuffer = new RealS.MeshFrameBuffer(frame);
            GenerateFaces(meshBuffer, (float)ThresholdSlider);

            Stopwatch watch = new Stopwatch();
            MeshGeometry3D d = new MeshGeometry3D();

            var vertexI = 0;
            for (int i = 0; i < frame.Vertices.Length; i += 3)
            {
                Point3D p = new(
                    frame.Vertices[i],
                    frame.Vertices[i + 1],
                    frame.Vertices[i + 2]);
                Point uv = new Point(frame.UVs[vertexI * 2], frame.UVs[vertexI * 2 + 1]);

                d.Positions.Add(p);
                d.TextureCoordinates.Add(uv);

                vertexI++;
            }


            d.TriangleIndices = new Int32Collection(meshBuffer.TempFaces);

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

        private MeshGeometry3D meshGeo;
        private ServerDataConnection dataConnection;
        private SignalRSession sessionClient;
        private ServerSessionConnection sessionConnection;

        private void PrepareModel(MeshGeometry3D mesh, Material frontMaterial)
        {
            meshGeo = mesh;
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


        private void OnFilterChange()
        {
            Console.WriteLine("Updating filters");
            RealS.updateFilters(filters);
        }


        /// <summary>
        /// Connects connection to a server.
        /// </summary>
        private async void OnConnect()
        {
            if (_inConnectProcess)
                return;
            _inConnectProcess = true;
            if (!ServerUrl.EndsWith("/"))
                ServerUrl += "/";
            if (!this.connected)
            {
                try
                {
                    Message = "Connecting";

                    var restClient = new RestDataClient(ServerUrl);
                    this.dataConnection = new ServerDataConnection(restClient);

                    var signalrClient = new SignalRSession(ServerUrl, "virtualWorldHub");
                    signalrClient.Disconnected += SessionClient_Disconnected;
                    this.sessionClient = signalrClient;

                    this.sessionConnection = new ServerSessionConnection(signalrClient);

                    if (sessionClient is { SessionState: SessionState.Connected })
                        await sessionClient.StopSessionAsync();

                    await this.sessionClient.StartSessionAsync();
                    this.Message = "Connected to server: " + ServerUrl;
                    this.ConnectBtnLbl = "Disconnect";
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
                    this.ConnectBtnLbl = "Connect";
                    Message = "Disconnected";

                }
                catch (Exception e)
                {
                    this.Message = "Cannot disconnect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }

            if (sessionClient == null)
            {
                _inConnectProcess = false;
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
                    await dataConnection.GetAllWorldObjectsAsync();
                }
            }

            _inConnectProcess = false;
        }

        private void SessionClient_Disconnected(object sender, Exception e)
        {
            //server disconnected 
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


            using (var frames = RealS.MeshFrame.Obtain())
            {
                if (!frames.HasValue)
                {
                    Message = "No mesh available";
                    return;
                }

                var frame = frames.Value;
                BuildModelFromFrame(frame);

                //send ply
                {
                    var p = new Dictionary<string, byte[]>
                    {
                        ["data"] = frame.Ply
                    };
                    var w = new WorldObjectDto
                    {
                        Name = PLY_NAME,
                        Type = "PlyFile",
                        Properties = p
                    };
                    try
                    {
                        //if object already on server wanna update instead
                        await dataConnection.AddWorldObjectAsync(w);
                    }
                    catch (Exception e)
                    {
                        await dataConnection.UpdateWorldObjectAsync(w);
                    }
                }

                //send mesh
                {
                    var properties =
                        new MeshSerializer().SerializeProperties(frame.Vertices, meshBuffer.TempFaces, "Triangle");
                    var w = new WorldObjectDto
                    {
                        Name = MESH_NAME,
                        Type = "Mesh",
                        Position = new RemoteVectorDto(),
                        Properties = properties,
                        Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                        Rotation = new RemoteVectorDto()
                    };
                    try
                    {
                        //if object already on server wanna update instead
                        await dataConnection.AddWorldObjectAsync(w);
                    }
                    catch (Exception e)
                    {
                        await dataConnection.UpdateWorldObjectAsync(w);
                    }
                }

                Message = "Mesh & Ply File Sent to server as " + MESH_NAME + "," + PLY_NAME;
            }
        }

        private async void OnRemoveImage()
        {
            try
            {
                await this.dataConnection.RemoveWorldObjectAsync(MESH_NAME);
                await this.dataConnection.RemoveWorldObjectAsync(PLY_NAME);
                this.Message = "Ply & Mesh removed from server";
            }
            catch (Exception ex)
            {
                this.Message = "No Ply & Mesh found on server";
            }
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