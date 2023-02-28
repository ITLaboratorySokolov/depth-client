using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using Intel.RealSense;
using Microsoft.Win32;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Connections.Client.Session;
using ZCU.TechnologyLab.Common.Serialization.Mesh;
using ZCU.TechnologyLab.DepthClient.DataModel;
using ZCU.TechnologyLab.Common.Serialization.Properties;
using System.Text.RegularExpressions;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public class MainViewModel : NotifyingClass
    {
        /* [DllImport("kernel32.dll", SetLastError = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         static extern bool AllocConsole();*/

        #region Constants
        // property names
        private const string POINTS_PROPERTY = "Points";
        public const string MODELTF_PROPERTY = "ModelTF";
        private const string MESSAGE_PROPERTY = "Message";
        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";
        private const string EN_APPLY_PROPERTY = "EnabledApply";
        private const string AUTO_PROPERTY = "AutoEnabledLbl";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";
        public const string MODEL_PROPERTY = "Model";
        public const string FRAME_PROPERTY = "Frame";
        public const string USER_CODE = "UserCode";

        private string clientName = "DepthClient1";

        // labels
        private string _connectBtnLbl = "Connect";
        private string _message;

        // object names
        public const string MESH_NAME = "DepthMesh";
        public const string MESH_TEXTURE_NAME = "DepthMeshTexture";
        public const string PLY_NAME = "DepthPly";

        #endregion

        // signifiers
        private bool _savePlyBtnEnable;
        private bool _enabledButtons;
        private bool _enabledApply;

        // filters
        private readonly bool[] _filters = Enumerable.Repeat(true, 6).ToArray();
        private bool _pointFilter = true;
        
        private double _thresholdSlider = 0.2;

        private int _linScaleFac = 2;
        private float _minValueTh = 0.15f;
        private float _maxValueTh = 2f;
        private int _iterationsSpat = 2;
        private float _alphaSpat = 0.5f; // 1-0.5
        private int _deltaSpat = 20;
        private int _holeSpat = 0;
        private float _alphaTemp = 0.6f; // 1-0.4
        private int _deltaTemp = 20;
        private int _persIndex = 3;
        private int _holeMethod = 1;

        private DispatcherTimer timer;

        // mesh
        private Model3D _model;
        private Transform3D _modelTF;
        private RealS.MeshFrame? _meshBuffer;
        private MeshGeometry3D _meshGeo;

        // last snapped frame
        private RealS.MeshFrame _frame;

        // points
        private Point3DCollection _pointsStorage;
        private Point3DCollection _points;

        // user code
        private UserCodeProcessor ucp;
        private string _userCode;
        private string _pythonPath;

        // networking
        ConnectionHandler connection;
        private string _autoLbl="AutoSend: OFF";
        private bool _autoEnable;


        LanguageController langContr;

        #region PublicProperties

        public static string ServerUrl { get; set; } = "https://localhost:57370/";

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

        public bool Filter5
        {
            get => _filters[5];
            set
            {
                this._filters[5] = value;
                OnFilterChange();
            }
        }

        public bool PointFilter
        {
            get => _pointFilter;
            set
            {
                this._pointFilter = value;
                OnPointFilterChange();
            }
        }

        public bool EnabledApply
        {
            get => this._enabledApply;
            set
            {
                this._enabledApply = value;
                RaisePropertyChanged(EN_APPLY_PROPERTY);
            }
        }

        public string PythonPath { get => _pythonPath; set => _pythonPath = value; }


        public ICommand Connect { get; private set; }
        public ICommand SendMesh { get; private set; }
        public ICommand RemoveMesh { get; private set; }
        public ICommand DownloadMesh { get; private set; }
        public ICommand OpenRealSenseFile { get; private set; }
        public ICommand ConnectCamera { get; private set; }
        public ICommand SavePly { get; private set; }
        public ICommand ReloadMesh { get; private set; }
        public ICommand EditPointcloud { get; private set; }
        public ICommand SetPythonPath { get; private set; }
        public LanguageController LangContr { get => langContr; set => langContr = value; }
        public string ClientName { get => clientName; set => clientName = value; }
        
        
        public float MinValueТh { get => _minValueTh;
            set
            { 
                _minValueTh = value;
                OnFilterChange();
            }
        }

        public float MaxValueTh { get => _maxValueTh;
            set
            {
                _maxValueTh = value;
                OnFilterChange();
            }
        }

        public float AlphaSpat { get => _alphaSpat; 
            set
            {
                _alphaSpat = value;
                OnFilterChange();
            }
        }

        public int DeltaSpat { get => _deltaSpat; 
            set
            {
                _deltaSpat = value;
                OnFilterChange();
            }
        }

        public float AlphaTemp { get => _alphaTemp; 
            set
            {
                _alphaTemp = value;
                OnFilterChange();
            }
        }

        public int DeltaTemp { get => _deltaTemp; 
            set
            {
                _deltaTemp = value;
                OnFilterChange();
            }
        }

        public int LinScaleFac { get => _linScaleFac;
            set
            {
                _linScaleFac = value;
                OnFilterChange();
            }
        }

        public int IterationsSpat { get => _iterationsSpat;
            set
            {
                _iterationsSpat = value; 
                OnFilterChange();
            }
        }

        public int HoleSpat { get => _holeSpat;
            set 
            { 
                _holeSpat = value;
                OnFilterChange();
            }
        }

        public int PersIndex { get => _persIndex; 
            set 
            { 
                _persIndex = value; 
                OnFilterChange();
            } }

        public int HoleMethod { get => _holeMethod;
            set 
            {
                _holeMethod = value; 
                OnFilterChange();
            }
        }



        #endregion


        public MainViewModel()
        {
            this.langContr = new LanguageController();
            connection = new ConnectionHandler();

            // TODO jinak - přes config!!
            LoadConfig();
            ucp = new UserCodeProcessor(_pythonPath);
            EnabledApply = true;

            // Create reactions
            this.Connect = new Command(this.OnConnect);
            this.SendMesh = new Command(this.OnSendMesh);
            this.RemoveMesh = new Command(this.OnRemoveImage);
            this.DownloadMesh = new Command(this.OnMeshDownloaded);
            this.OpenRealSenseFile = new Command(this.OnOpenRealSenseFile);
            this.ConnectCamera = new Command(this.OnConnectCamera);
            this.SavePly = new Command(this.OnSavePly);
            this.ReloadMesh = new Command(this.OnSnapshot);
            this.EditPointcloud = new Command(this.OnApplyClicked);
            this.SetPythonPath = new Command(this.OnSetPythonPath);

            // Set up default view
            OnConnectCamera();
            // BuildMeshDefault();

            // Set up timer
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        private void LoadConfig()
        {
            string cfpth = "./config.txt";
            if (File.Exists(cfpth))
            {
                var lines = File.ReadAllLines(cfpth);
                _pythonPath = lines[0];
                if (lines.Length > 1)
                    clientName = lines[1].Trim();
                else
                    clientName = "DefaultClient";
            }
        }

        /// <summary>
        /// Set path to python dll
        /// Create new User code processor
        /// </summary>
        private void OnSetPythonPath()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "dll files (*.dll)|*.dll|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.InitialDirectory = "";
            dialog.Title = langContr.PyDialogTitle;

            var success = dialog.ShowDialog();
            if (success.HasValue && success.Value)
            {
                Message = langContr.PySuccess;
                ucp = new UserCodeProcessor(dialog.FileName);
            }
        }

        /// <summary>
        /// Reaction to Apply button clicked
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void OnApplyClicked()
        {
            if (Frame.Vertices == null)
            {
                Message = langContr.NoPointcloud;
                return;
            }    

            // Test of editing
            unsafe
            {
                Message = langContr.Processing;
                EnabledApply = false;
                RunUserCode();
            }
        }

        async void RunUserCode()
        {
            Dictionary<string, object> vars = new Dictionary<string, object>();
            
            vars.Add("points", Frame.Vertices);
            vars.Add("uvs", Frame.UVs);
            vars.Add("faces", Frame.TempFaces);

            PointCloud res = await ucp.ExecuteUserCode(UserCode, vars);
            if (res == null)
            {
                string erMsg = ucp.ERROR_MSG;

                MatchCollection strs = Regex.Matches(erMsg, @"line \d+");
                foreach (Match l in strs)
                {
                    string resultString = Regex.Match(l.Value, @"\d+").Value;
                    int val = Int32.Parse(resultString) - 1;
                    erMsg = Regex.Replace(erMsg, l.Value, "line " + val);
                }
                Message = erMsg;
            }
            else
            {
                var frame = Frame;

                frame.Vertices = res.points;
                frame.UVs = res.uvs;
                frame.TempFaces = res.faces;
                frame.Faces = new int[res.faces.Length];
                Array.Copy(res.faces, frame.Faces, frame.Faces.Length);

                Frame = frame;
                Message = langContr.CodeExec;

                BuildMesh(Frame);
            }

            EnabledApply = true;
        }

        /// <summary>
        ///  Update Update label
        /// </summary>
        /// <param name="remaining"> Time until auto update </param>
        private void UpdateAutoLabel(int remaining)
        {
            if (_autoEnable)
                AutoEnabledLbl = "AutoSend: ON, " + $"{remaining,2}";
            else
                AutoEnabledLbl = "AutoSend: OFF";
        }

        /// <summary>
        /// Reaction to change of auto update settings
        /// </summary>
        public void OnChangeAuto()
        {
            if ((!RealS.Started|| !connection._connected)&&!_autoEnable)
            {
                Message = langContr.NoCamOrSer;
                return;
            }

            _autoEnable = !_autoEnable;
            nextAuto = AutoInterval;
            UpdateAutoLabel(nextAuto);
        }

        /// <summary>
        /// Update model according to a new value of slider
        /// </summary>
        public void UpdateSliderModel()
        {
            if (!_meshBuffer.HasValue) return;

            MeshProcessor.GenerateFaces(_meshBuffer.Value, (float)ThresholdSlider);

            _meshGeo.TriangleIndices = new Int32Collection(_meshBuffer.Value.TempFaces);
            RaisePropertyChanged(MODEL_PROPERTY);
        }

        /// <summary>
        /// Reaction to snapshot of depth camera view being taken
        /// - take mesh frame (depth + texture) snapshot
        /// - create a new mesh to display
        /// </summary>
        private void OnSnapshot()
        {
            if (!RealS.Started)
            {
                Message = langContr.NoCam;
                return;
            }

            var frames = RealS.MeshFrame.Obtain();
            if (!frames.HasValue)
            {
                Message = langContr.NoMesh;
                return;
            }

            _frame = frames.Value;
            BuildMesh(_frame); // frames.Value
            // Message = "New mesh with " + (_frame.Vertices.Length/3) + " v and " + (_frame.Faces.Length / 3) + " triangles";

        }

        private int nextAuto = 0;

        /// <summary>
        /// Timer tick
        /// - update label
        /// - make a new snapshot
        /// - send mesh to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                // Console.WriteLine("Sending mesh");
                OnSnapshot();
                OnSendMesh();
            }
            UpdateAutoLabel(nextAuto);
        }

        /// <summary>
        /// Build new objects to display from Mesh frame
        /// - create a mesh
        /// - create a point cloud
        /// </summary>
        /// <param name="frame"> Mesh frame </param>
        /// <exception cref="Exception"></exception>
        private void BuildMesh(RealS.MeshFrame frame)
        {
            _meshBuffer = frame;
            MeshProcessor.GenerateFaces(frame, (float)ThresholdSlider);

            Stopwatch watch = new Stopwatch();

            watch.Start();
            MeshGeometry3D d = MeshProcessor.CreateMeshFromFrame(frame);
            watch.Stop();

            // Console.WriteLine("Elapsed for mesh build " + watch.ElapsedMilliseconds);

            Material mat;
            var m = BitmapProcessor.BuildBitmap(frame.Colors, frame.Width, frame.Height, 3);
            mat = MaterialHelper.CreateImageMaterial(BitmapProcessor.Bitmap2Image(m), 1);

            SetModel(d, mat);
        }



        /// <summary>
        /// Build default mesh
        /// </summary>
        private void BuildMeshDefault()
        {
            var mesh = MeshProcessor.CreateDefaultMesh();
            SetModel(mesh, MaterialHelper.CreateMaterial(Colors.Green));
        }

        /// <summary>
        /// Set model to display
        /// </summary>
        /// <param name="mesh"> Mesh </param>
        /// <param name="frontMaterial"> Material to use </param>
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
            this._pointsStorage = pc;
            if (PointFilter)
                this.Points = pc;
        }

        /// <summary>
        /// Filters changed
        /// </summary>
        private void OnFilterChange()
        {
            if (!RealS.Started)
            {
                Message = langContr.NoCam;
                return;
            }

            // Console.WriteLine("Updating filters");
            float[] filterData = new float[] {
                // Decimation filter
                _linScaleFac,
                
                // Threshold filter
                _minValueTh,
                _maxValueTh,
                
                // Spatial smoothing
                _iterationsSpat,
                1-_alphaSpat,
                _deltaSpat,
                _holeSpat,
                
                // Temporal smoothing
                1-_alphaTemp,
                _deltaTemp,
                _persIndex,

                // Hole filling
                _holeMethod
            };

            RealS.updateFilters(_filters, filterData);
        }

        /// <summary>
        /// Set up connection to a server.
        /// </summary>
        private async void OnConnect()
        {
            // Currently connecting
            if (connection._inConnectProcess)
                return;

            connection._inConnectProcess = true;
            if (!ServerUrl.EndsWith("/"))
                ServerUrl += "/";

            // Not yet connected
            if (!connection._connected)
            {
                Message = langContr.Connecting;
                bool res = await connection.Connect(ServerUrl, SessionClient_Disconnected);
                if (res)
                {
                    this.Message = langContr.ConnectedToSer + ServerUrl;
                    this.ConnectBtnLbl = langContr.Disconnect;
                }
                else
                {
                    this.ConnectBtnLbl = langContr.Connect;
                    this.Message = langContr.CantConnect + connection.ErrorMessage;
                }

            }
            else
            {
                _autoEnable = false;
                UpdateAutoLabel(0);

                bool res = await connection.DisConnect();
                if (res)
                {
                    this.ConnectBtnLbl = langContr.Connect;
                    Message = langContr.Disconnected;
                }
                else
                {
                    this.Message = langContr.CantConnect + connection.ErrorMessage;
                }

            }

            // TODO does this do anything??
            /*
            if (_sessionClient == null)
            {
                connection._inConnectProcess = false;
                return;
            }
            */

            // Update menu items
            UpdateMenuItems();
            connection._inConnectProcess = false;
        }

        private void UpdateMenuItems()
        {
            // Update menu items
            if (connection.IsClientDisconnected())
            {
                connection._connected = !connection._connected;

                this.ConnectBtnLbl = connection._connected ? langContr.Disconnect : langContr.Connect;
                EnabledButtons = connection._connected;
            }
           
        }

        /// <summary>
        /// If client has disconnected / has been disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SessionClient_Disconnected(object sender, Exception e)
        {
            // If disconnected automatically - change buttons
            UpdateMenuItems();
            Message = langContr.Disconnected;
        }

        /// <summary>
        /// Downloading mesh + texture from server
        /// </summary>
        private async void OnMeshDownloaded()
        {
            if (connection.GetSessionState() != SessionState.Connected)
                return;

            WorldObjectDto wo = await connection.GetWorldObject(MESH_NAME);
            // WorldObjectDto tex = await connection.GetWorldObject(MESH_TEXTURE_NAME);

            if (wo == null) // || tex == null)
            {
                Message = langContr.MeshNotOnSer;
                return;
            }

            var mesh = MeshProcessor.CreateMeshFrameFromWO(wo);
            BuildMesh(mesh);
        }

        /// <summary>
        /// Send mesh to server
        /// </summary>
        private async void OnSendMesh()
        {
            if (!_meshBuffer.HasValue)
            {
                Message = langContr.NoMesh;
                return;
            }

            var frame = _meshBuffer.Value;
            // BuildMesh(frame);
            bool resT, resM = false, resP = false;

            //send ply
            if (frame.Ply!=null){
                var p = new Dictionary<string, byte[]>
                {
                    ["Data"] = frame.Ply
                };
                var w = new WorldObjectDto
                {
                    Name = PLY_NAME + "_" + clientName,
                    Type = "PlyFile",
                    Properties = p,
                    Position = new RemoteVectorDto(),
                    Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                    Rotation = new RemoteVectorDto()
                };
                w.Position.X = 0;
                w.Position.Y = 0;
                w.Position.Z = 0;

                try
                {
                    resP = await connection.SendWorldObject(w);
                }
                catch (Exception ex)
                {
                    Message = langContr.SerUnavail;
                    UpdateMenuItems();
                    return;
                }
            }

            //send mesh
            {
                // var fc = MeshProcessor.GenerateFaces(frame, (float)_thresholdSlider);
                // Obj o = new Obj(frame.Vertices, frame.TempFaces, new float[] { });
                // o.WriteObjFile("D:/moje/test.obj", null);

                byte[] texFormat = new StringSerializer("TextureFormat").Serialize("RGB");
                byte[] texSize = new ArraySerializer<int>("TextureSize", sizeof(int)).Serialize(new int[] { frame.Width, frame.Height });

                var properties = new RawMeshSerializer().Serialize(frame.Vertices, frame.TempFaces, "Triangle", MESH_TEXTURE_NAME, frame.UVs);
                properties.Add("TextureFormat", texFormat);
                properties.Add("TextureSize", texSize);
                properties.Add("Texture", frame.Colors);

                var w = new WorldObjectDto
                {
                    Name = MESH_NAME + "_" + clientName,
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
                    resM = await connection.SendWorldObject(w);
                }
                catch (Exception ex)
                {
                    Message = "Server currently unavailible";
                    UpdateMenuItems();
                    return;
                }
            }

            /*
            //send texture
            {
                var properties =
                    new BitmapSerializerFactory().RawBitmapSerializer.Serialize(frame.Width, frame.Height, "RGB", frame.Colors);

                var w = new WorldObjectDto
                {
                    Name = MESH_TEXTURE_NAME + "_" + clientName,
                    Type = "Bitmap",
                    Properties = properties,
                    Position = new RemoteVectorDto(),
                    Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                    Rotation = new RemoteVectorDto()
                };
                w.Position.X = 0;
                w.Position.Y = 0;
                w.Position.Z = 0;

                resT = await connection.SendWorldObject(w);
            }
            */

            if (resM && resP)
                Message = langContr.MeshSent + MESH_NAME + "," + PLY_NAME + ", " + MESH_TEXTURE_NAME;
            else
                Message = langContr.MeshNotSent;

        }

        /// <summary>
        /// Remove mesh from server
        /// </summary>
        private async void OnRemoveImage()
        {
            bool mRes = await connection.RemoveWorldObject(MESH_NAME + "_" + clientName);
            bool pRes = await connection.RemoveWorldObject(PLY_NAME + "_" + clientName);
            // bool tMes = await connection.RemoveWorldObject(MESH_TEXTURE_NAME);

            if (mRes && pRes) // && tMes)
                this.Message = langContr.MeshRemoved;
            else
                this.Message = langContr.MeshAndPlyNotFound;
        }

        /// <summary>
        /// Open realsense file
        /// </summary>
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
                        ? langContr.OpenedRSFile
                        : langContr.CouldNotOpen;

                    SavePlyBtnEnable = success;

                    Console.WriteLine(Message + ": " + openDialog.FileName);
                });
                t.Start();
            }

            // freezeDepth = false;
        }

        /// <summary>
        /// Camera connected
        /// </summary>
        private void OnConnectCamera()
        {
            var t = new Thread(() =>
            {
                var success = RealS.Init("");
                Message = success ? langContr.CamConn : langContr.CamNotFound;
                SavePlyBtnEnable = success;
                OnFilterChange();
            });
            t.Start();

        }

        /// <summary>
        /// Save snapshot to a ply file
        /// </summary>
        private void OnSavePly()
        {
            if (!RealS.Started)
                return;

            if (!_meshBuffer.HasValue)
            {
                Message = langContr.NoSnap;
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

            Message = langContr.PlySaved;
            // Console.WriteLine("Saved ply file to " + saveDialog.FileName);

            ProcessingWindow.FreezeBuffer(false);
            saveDialog.Reset();
        }

        /// <summary>
        /// Change point filters
        /// </summary>
        private void OnPointFilterChange()
        {
            // if true - set to points
            if (_pointFilter)
            {
                this.Points = _pointsStorage;
            }
            // if untrue - unset points
            else
            {
                this.Points = new Point3DCollection();
            }

            RaisePropertyChanged(POINTS_PROPERTY);
        }

    }
}