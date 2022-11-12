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
using ZCU.TechnologyLab.DepthClient.ViewModels.ZCU.TechnologyLab.Common.Serialization;
using _3DTools;
using System.Windows.Controls;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public class MainViewModel : NotifyingClass
    {
        /* [DllImport("kernel32.dll", SetLastError = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         static extern bool AllocConsole();*/

        // property names
        private const string POINTS_PROPERTY = "Points";
        public const string MODELTF_PROPERTY = "ModelTF";
        private const string MESSAGE_PROPERTY = "Message";
        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";
        private const string AUTO_PROPERTY = "AutoEnabledLbl";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";
        public const string MODEL_PROPERTY = "Model";
        public const string FRAME_PROPERTY = "Frame";
        public const string USER_CODE = "UserCode";

        public const string MESH_NAME = "DepthMesh";
        public const string MESH_TEXTURE_NAME = "DepthMeshTexture";
        public const string PLY_NAME = "DepthPly";


        private string _connectBtnLbl = "Connect";
        private bool _inConnectProcess;
        private bool _savePlyBtnEnable;
        private double _thresholdSlider = 0.2;
        private DispatcherTimer timer;
        private readonly bool[] _filters = Enumerable.Repeat(true, 5).ToArray();
        private string _message;
        private bool _enabledButtons;
        private bool _connected;

        // mesh
        private Model3D _model;
        private RealS.MeshFrame? _meshBuffer;
        private MeshGeometry3D _meshGeo;

        // last snapped frame
        private RealS.MeshFrame _frame;

        private Point3DCollection _points;
        private Transform3D _modelTF;

        // user code
        private string _userCode;

        // networking
        private ServerDataConnection _dataConnection;
        private SignalRSession _sessionClient;
        private ServerSessionConnection _sessionConnection;
        private string _autoLbl="AutoSend: OFF";
        private bool _autoEnable;


        #region PublicProperties

        public Point3DCollection Points
        {
            get => this._points;
            set
            {
                this._points = value;
                RaisePropertyChanged(POINTS_PROPERTY);
            }
        }

        public RealS.MeshFrame Frame
        {
            get => this._frame;
            set
            {
                this._frame = value;
                RaisePropertyChanged(FRAME_PROPERTY);
            }
        }

        public string UserCode
        {
            get => this._userCode;
            set
            {
                this._userCode = value;
                RaisePropertyChanged(USER_CODE);
            }
        }

        public string Message
        {
            get => this._message;
            set
            {
                this._message = value;
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
            get => this._connectBtnLbl;
            set
            {
                this._connectBtnLbl = value;
                RaisePropertyChanged(CNTCBTLB_PROPERTY);
            }
        }

        public Model3D Model
        {
            get => _model;
            set
            {
                _model = value;
                RaisePropertyChanged(MODEL_PROPERTY);
            }
        }

        public Transform3D ModelTF
        {
            get => _modelTF;
            set
            {
                _modelTF = value;
                RaisePropertyChanged(MODELTF_PROPERTY);
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

        public string AutoEnabledLbl
        {
            get => this._autoLbl;
            set
            {
                this._autoLbl = value;
                RaisePropertyChanged(AUTO_PROPERTY);
            }
        }
        public bool EnabledButtons
        {
            get => this._enabledButtons;
            set
            {
                this._enabledButtons = value;
                RaisePropertyChanged(EN_BTN_PROPERTY);
            }
        }

        public int AutoInterval { get; set; } = 10;

        public bool Filter0
        {
            get => _filters[0];
            set
            {
                this._filters[0] = value;
                OnFilterChange();
            }
        }

        public bool Filter1
        {
            get => _filters[1];
            set
            {
                this._filters[1] = value;
                OnFilterChange();
            }
        }

        public bool Filter2
        {
            get => _filters[2];
            set
            {
                this._filters[2] = value;
                OnFilterChange();
            }
        }

        public bool Filter3
        {
            get => _filters[3];
            set
            {
                this._filters[3] = value;
                OnFilterChange();
            }
        }

        public bool Filter4
        {
            get => _filters[4];
            set
            {
                this._filters[4] = value;
                OnFilterChange();
            }
        }

        public ICommand Connect { get; private set; }
        public ICommand SendMesh { get; private set; }
        public ICommand RemoveMesh { get; private set; }
        public ICommand DownloadMesh { get; private set; }
        public ICommand OpenRealSenseFile { get; private set; }
        public ICommand ConnectCamera { get; private set; }
        public ICommand SavePly { get; private set; }
        public ICommand ReloadMesh { get; private set; }
        public ICommand EditPointcloud { get; private set; }



        #endregion


        public MainViewModel()
        {
            //  AllocConsole();
            this.Connect = new Command(this.OnConnect);
            this.SendMesh = new Command(this.OnSendMesh);
            this.RemoveMesh = new Command(this.OnRemoveImage);
            this.DownloadMesh = new Command(this.OnMeshDownloaded);
            this.OpenRealSenseFile = new Command(this.OnOpenRealSenseFile);
            this.ConnectCamera = new Command(this.OnConnectCamera);
            this.SavePly = new Command(this.OnSavePly);
            this.ReloadMesh = new Command(this.OnSnapshot);
            this.EditPointcloud = new Command(this.OnApplyClicked);

            OnConnectCamera();
            BuildMeshDefault();

            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        private void OnApplyClicked()
        {
            // no captured frame
            if (Frame.Vertices == null)
            {
                Message = "No pointcloud available";
                return;
            }

            string code = UserCode;
            var frm = Frame;

            /*
            Frame.Faces;
            Frame.TempFaces;
            Frame.Faces.Length;
            Frame.Vertices;
            frame.UVs.Length;
            */

            Message = "Processing...";
            int prev = Frame.Vertices.Length;

            // Test of editing
            {
                // sanity check before unsafe
                if (Frame.Vertices.Length / 3 != Frame.UVs.Length / 2)
                    throw new Exception("wrong array dimensions");

                unsafe
                {
                    Thread t = new Thread(() =>
                    {
                        for (int i = 0; i < Frame.Vertices.Length; i += 3)
                        {
                            Frame.Vertices[i] = Frame.Vertices[i] + 0.5f;
                            Frame.Vertices[i + 1] = Frame.Vertices[i + 1] + 0.5f;
                            Frame.Vertices[i + 2] = Frame.Vertices[i + 2] + 0.5f;
                        }

                        DeletePoint(Frame.Vertices.Length / 3 - 1);
                        Message = "Code executed";
                    });

                    // TODO didnt help myself here did i
                    t.Start();
                    t.Join();
                }
            }

            // edit pointcloud according to code
            //      - need to change vertices, UV coords and faces
            //      - data from realsense is taken as meshframe
            // reset mesh according to the pointcloud change
            //      - create a new mesh with same texture but different points

            // TODO mesh on init - do i want to save it and potentialy edit it too?
            // TODO how is it sent to server?

            // rebuild mesh
            BuildMesh(Frame);
        }


        private void DeletePoint(int index)
        {
            RealS.MeshFrame frame = Frame;

            // copy of vertices array but without the point
            // copy of UVs array but without the point
            float[] newUV = new float[frame.UVs.Length - 2];
            float[] newVerts = new float[frame.Vertices.Length - 3];

            int newi = 0;
            for (int i = 0; i < frame.Vertices.Length / 3; i++)
            {
                if (i == index)
                    continue;

                newUV[newi * 2] = frame.UVs[i * 2];
                newUV[newi * 2 + 1] = frame.UVs[i * 2 + 1];

                newVerts[newi * 3] = frame.Vertices[i * 3];
                newVerts[newi * 3 + 1] = frame.Vertices[i * 3 + 1];
                newVerts[newi * 3 + 2] = frame.Vertices[i * 3 + 2];
                newi++;
            }

            // faces that contain that point have to be removed
            List<int> newFaces = new List<int>();
            List<int> newTempFaces = new List<int>();

            for (int i = 0; i < frame.TempFaces.Length; i += 3)
            {
                if (frame.TempFaces[i] == index || frame.TempFaces[i + 1] == index || frame.TempFaces[i + 2] == index)
                    continue;

                int newV0 = frame.TempFaces[i] > index ? (frame.TempFaces[i] - 1) : frame.TempFaces[i];
                int newV1 = (frame.TempFaces[i + 1] > index) ? (frame.TempFaces[i + 1] - 1) : frame.TempFaces[i + 1];
                int newV2 = (frame.TempFaces[i + 2] > index) ? (frame.TempFaces[i + 2] - 1) : frame.TempFaces[i + 2];

                newTempFaces.Add(newV0);
                newTempFaces.Add(newV1);
                newTempFaces.Add(newV2);

                newV0 = (frame.Faces[i] > index) ? (frame.Faces[i] - 1) : frame.Faces[i];
                newV1 = (frame.Faces[i + 1] > index) ? (frame.Faces[i + 1] - 1) : frame.Faces[i + 1];
                newV2 = (frame.Faces[i + 2] > index) ? (frame.Faces[i + 2] - 1) : frame.Faces[i + 2];
                
                newFaces.Add(newV0);
                newFaces.Add(newV1);
                newFaces.Add(newV2);
            }

            frame.TempFaces = newTempFaces.ToArray();
            frame.Faces = newFaces.ToArray();
            frame.Vertices = newVerts;
            frame.UVs = newUV;

            // apply changes
            Frame = frame;
        }


        private void UpdateAutoLabel(int remaining)
        {
            if (_autoEnable)
            {
                AutoEnabledLbl = "AutoSend: ON, " + $"{remaining,2}";
            }
            else
            {
                AutoEnabledLbl = "AutoSend: OFF";
            }
        }
        public void OnChangeAuto()
        {
            if ((!RealS.Started|| !_connected)&&!_autoEnable)
            {
                Message = "No Camera or Server connected";
                return;
            }

            _autoEnable = !_autoEnable;
            nextAuto = AutoInterval;
            UpdateAutoLabel(nextAuto);
        }

        public void UpdateSliderModel()
        {
            if (!_meshBuffer.HasValue) return;

            GenerateFaces(_meshBuffer.Value, (float)ThresholdSlider);
            _meshGeo.TriangleIndices = new Int32Collection(_meshBuffer.Value.TempFaces);
            RaisePropertyChanged(MODEL_PROPERTY);
        }

        private void OnSnapshot()
        {
            if (!RealS.Started)
            {
                Message = "No camera connected";
                return;
            }

            var frames = RealS.MeshFrame.Obtain();
            if (!frames.HasValue)
            {
                Message = "No mesh available";
                return;
            }

            _frame = frames.Value;
            BuildMesh(frames.Value);
        }

        private int nextAuto = 0;
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_autoEnable)
                return;

            if (!RealS.Started)
            {
                _autoEnable = false;
                UpdateAutoLabel(0);
                return;
            }



            if (nextAuto > 0)
                nextAuto--;
            else
            {
                nextAuto = AutoInterval;
                Console.WriteLine("Sending mesh");
                OnSnapshot();
                OnSendMesh();
            }
            UpdateAutoLabel(nextAuto);

        }

        public static BitmapImage Bitmap2Image(BitmapSource bitmapSource)
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

        public static void GenerateFaces(RealS.MeshFrame frame, float threshold)
        {
            threshold *= threshold;

            Array.Copy(frame.Faces, frame.TempFaces, frame.Faces.Length);

            Stopwatch w = new();
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

            Console.WriteLine("generate faces took " + w.ElapsedMilliseconds);
        }


        private void BuildMesh(RealS.MeshFrame frame)
        {
            _meshBuffer = frame;
            GenerateFaces(frame, (float)ThresholdSlider);

            Stopwatch watch = new Stopwatch();
            MeshGeometry3D d = new MeshGeometry3D();

            d.Positions = new Point3DCollection(frame.Vertices.Length / 3);

            {
                // sanity check before unsafe
                if (frame.Vertices.Length / 3 != frame.UVs.Length / 2)
                    throw new Exception("wrong array dimensions");

                d.TextureCoordinates = new PointCollection(frame.UVs.Length / 2);

                unsafe
                {
                    fixed (float* vertex = &frame.Vertices[0], uv = &frame.UVs[0])
                    {
                        float* vert = vertex;
                        float* tex = uv;

                        while (vert != vertex + frame.Vertices.Length)
                        {
                            Point3D p = new(
                                *vert++,
                                *vert++,
                                *vert++);

                            Point u = new Point(
                                *tex++,
                                *tex++);

                            d.Positions.Add(p);
                            d.TextureCoordinates.Add(u);
                        }
                    }
                }
            }
          

            d.TriangleIndices = new Int32Collection(frame.TempFaces);

            Console.WriteLine("Elapsed for mesh build " + watch.ElapsedMilliseconds);

            Material mat;

            var m = BuildBitmap(frame.Colors, frame.Width, frame.Height, 3);

            mat = MaterialHelper.CreateImageMaterial(Bitmap2Image(m), 1);

            SetModel(d, mat);
        }

        private void BuildMeshDefault()
        {
            // Create a mesh builder and add a box to it
            var meshBuilder = new MeshBuilder(false, false);
            meshBuilder.AddBox(new Point3D(0, 0, 1), 1, 2, 0.5);
            meshBuilder.AddBox(new Rect3D(0, 0, 1.2, 0.5, 1, 0.4));
            // Create a mesh from the builder (and freeze it)
            var mesh = meshBuilder.ToMesh(true);

            SetModel(mesh, MaterialHelper.CreateMaterial(Colors.Green));
        }

        public static BitmapSource BuildBitmap(byte[] data, int w, int h, int ch)
        {
            PixelFormat format = PixelFormats.Default;

            if (ch == 1) format = PixelFormats.Gray8; //grey scale image 0-255
            if (ch == 3) format = PixelFormats.Bgr24; //RGB
            if (ch == 4) format = PixelFormats.Bgr32; //RGB + alpha


            WriteableBitmap wbm = new WriteableBitmap(w, h, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, w, h), data, ch * w, 0);

            return wbm;
        }


        private void SetModel(MeshGeometry3D mesh, Material frontMaterial)
        {
            _meshGeo = mesh;
            var modelGroup = new Model3DGroup();

            var insideMaterial = MaterialHelper.CreateMaterial(Colors.CadetBlue);

            //if(!mesh.IsFrozen)
            //     mesh.Normals = mesh.CalculateNormals();

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

            var model = new GeometryModel3D
            { 
                Geometry = mesh,
                Material = frontMaterial,
                BackMaterial = insideMaterial,
                Transform = transforms
            };
            modelGroup.Children.Add(model);

            var pc = new Point3DCollection(mesh.Positions);
            ModelTF = transforms;

            this.Model = modelGroup;
            this.Points = pc;
        }


        private void OnFilterChange()
        {
            Console.WriteLine("Updating filters");
            RealS.updateFilters(_filters);
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
            if (!this._connected)
            {
                try
                {
                    Message = "Connecting";

                    var restClient = new RestDataClient(ServerUrl);
                    this._dataConnection = new ServerDataConnection(restClient);

                    var signalrClient = new SignalRSession(ServerUrl, "virtualWorldHub");
                    signalrClient.Disconnected += SessionClient_Disconnected;
                    this._sessionClient = signalrClient;

                    this._sessionConnection = new ServerSessionConnection(signalrClient);

                    if (_sessionClient is { SessionState: SessionState.Connected })
                        await _sessionClient.StopSessionAsync();

                    await this._sessionClient.StartSessionAsync();
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
                _autoEnable = false;
                UpdateAutoLabel(0);
                try
                {
                    await this._sessionClient.StopSessionAsync();
                    this.ConnectBtnLbl = "Connect";
                    Message = "Disconnected";
                }
                catch (Exception e)
                {
                    this.Message = "Cannot disconnect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }

            if (_sessionClient == null)
            {
                _inConnectProcess = false;
                return;
            }

            if ((!this._connected && this._sessionClient.SessionState == SessionState.Connected)
                || (this._connected && this._sessionClient.SessionState == SessionState.Closed)
               )
            {
                this._connected = !this._connected;

                this.ConnectBtnLbl = this._connected ? "Disconnect" : "Connect";
                EnabledButtons = this._connected;
            }

            _inConnectProcess = false;
        }

        private void SessionClient_Disconnected(object sender, Exception e)
        {
            Message = "Disconnected from server";
        }

        private async void OnMeshDownloaded()
        {
            if (_sessionClient.SessionState != SessionState.Connected)
                return;

            WorldObjectDto wo;
            WorldObjectDto tex;
            try
            {
                wo = await _dataConnection.GetWorldObjectAsync(MESH_NAME);
                tex = await _dataConnection.GetWorldObjectAsync(MESH_TEXTURE_NAME);
            }
            catch
            {
                Message = "Mesh with texture not found on server";
                return;
            }

            var mesh = new RealS.MeshFrame();
            mesh.Faces = new TexturedMeshSerializer().DeserializeIndices(wo.Properties);
            mesh.TempFaces = new int[mesh.Faces.Length];
            mesh.Vertices = new TexturedMeshSerializer().DeserializeVertices(wo.Properties);
            mesh.UVs = new TexturedMeshSerializer().DeserializeUVs(wo.Properties);
            mesh.Colors = new BitmapSerializer().DeserializePixels(tex.Properties);
            mesh.Width = new BitmapSerializer().DeserializeWidth(tex.Properties);
            BuildMesh(mesh);
        }

        private async void OnSendMesh()
        {
            Console.WriteLine("Parsing mesh");


            if (!_meshBuffer.HasValue)
            {
                Message = "No mesh available";
                return;
            }

            var frame = _meshBuffer.Value;
            BuildMesh(frame);

            //send ply
            if(frame.Ply!=null){
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
                    await _dataConnection.AddWorldObjectAsync(w);
                }
                catch
                {
                    await _dataConnection.UpdateWorldObjectAsync(w);
                }
            }

            //send mesh
            {
                GenerateFaces(frame, (float)_thresholdSlider);

                var properties =
                    new TexturedMeshSerializer().SerializeProperties(frame.Vertices,frame.UVs, frame.TempFaces, "Triangle",MESH_TEXTURE_NAME);
                var w = new WorldObjectDto
                {
                    Name = MESH_NAME,
                    Type = "Mesh",
                    Position = new RemoteVectorDto(),
                    Properties = properties,
                    Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                    Rotation = new RemoteVectorDto()
                };
                w.Position.X = 0;
                w.Position.Y = 0;
                w.Position.Z = 0;
                try
                {
                    //if object already on server wanna update instead
                    await _dataConnection.AddWorldObjectAsync(w);
                }
                catch
                {
                    await _dataConnection.UpdateWorldObjectAsync(w);
                }
            }
            //send texture
            {
                var properties =
                    new BitmapSerializer().SerializeRawBitmap(frame.Width, frame.Height, "RGB", frame.Colors);

                var w = new WorldObjectDto
                {
                    Name = MESH_TEXTURE_NAME,
                    Type = "Bitmap",
                    Properties = properties,
                };
                try
                {
                    //if object already on server wanna update instead
                    await _dataConnection.AddWorldObjectAsync(w);
                }
                catch
                {
                    await _dataConnection.UpdateWorldObjectAsync(w);
                }
            }
            Message = "Mesh & Ply File Sent to server as " + MESH_NAME + "," + PLY_NAME+", "+MESH_TEXTURE_NAME;
        }

        private async void OnRemoveImage()
        {
            try
            {
                await this._dataConnection.RemoveWorldObjectAsync(MESH_NAME);
                await this._dataConnection.RemoveWorldObjectAsync(PLY_NAME);
                await this._dataConnection.RemoveWorldObjectAsync(MESH_TEXTURE_NAME);
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

            if (!_meshBuffer.HasValue)
            {
                Message = "No snapshot taken yet";
                return;
            }
            var frame = _meshBuffer.Value;

            BuildMesh(frame);

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