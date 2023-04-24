using System;
using System.Collections.Generic;
using System.IO;
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
using ZCU.TechnologyLab.DepthClient.DataModel;
using System.Text.RegularExpressions;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// Data model for windows, holds logic processing the data in the application
    /// </summary>
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
        private const string DIS_CONNECT_PROPERTY = "EnableConnect";
        private const string EN_APPLY_PROPERTY = "EnabledApply";
        private const string AUTO_PROPERTY = "AutoEnabledLbl";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";
        public const string MODEL_PROPERTY = "Model";
        public const string FRAME_PROPERTY = "Frame";
        public const string USER_CODE = "UserCode";
        public const string ENABLED_URL = "EnabledURL";
        public const string DLLENABLED = "DllEnabled";

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
        private bool _enableConnect;
        private bool _enabledApply;

        // filters
        FilterData filtData;

        // timer
        private DispatcherTimer timer;
        private int nextAuto = 0;

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
        private string _autoLbl = "AutoSend: OFF";
        private bool _autoEnable;
        private bool _enabledURL = true;
        private bool _dllEnabled = true;

        // language controller
        LanguageController langContr;

        #region PublicProperties

        public static string ServerUrl { get; set; } = "";

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
        public bool EnableConnect
        {
            get => _enableConnect;
            set
            {
                _enableConnect = value;
                RaisePropertyChanged(DIS_CONNECT_PROPERTY);
            }
        }


        public int AutoInterval { get; set; } = 10;

        public bool EnabledApply
        {
            get => this._enabledApply;
            set
            {
                this._enabledApply = value;
                RaisePropertyChanged(EN_APPLY_PROPERTY);
            }
        }

        public bool DllEnabled
        {
            get => _dllEnabled;
            set {
                _dllEnabled = value;
                RaisePropertyChanged(DLLENABLED);
            }
        }


        public string PythonPath { get => _pythonPath; set => _pythonPath = value; }
        public string ClientName { get => clientName; set => clientName = value; }
        public LanguageController LangContr { get => langContr; set => langContr = value; }
        public FilterData FiltData { get => filtData; set => filtData = value; }
        public bool EnabledURL { get => _enabledURL; 
            set { _enabledURL = value; RaisePropertyChanged(ENABLED_URL); } }


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
        public ICommand ResetFilters { get; private set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel()
        {
            FiltData = new FilterData(this);

            this.langContr = new LanguageController();
            connection = new ConnectionHandler();

            LoadConfig();
            ucp = new UserCodeProcessor(_pythonPath);
            EnabledApply = true;
            EnableConnect = true;

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
            this.ResetFilters = new Command(this.OnResetFilters);

            // Set up default view
            OnConnectCamera();

            // Set up timer
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        /// <summary>
        /// Load config file from default location
        /// </summary>
        private void LoadConfig()
        {
            string cfpth = "./config.txt";
            if (File.Exists(cfpth))
            {
                var lines = File.ReadAllLines(cfpth);
                _pythonPath = lines[0];
                ServerUrl = lines[1];
                if (lines.Length > 2)
                    clientName = lines[2].Trim();
                else
                    clientName = "DefaultClient";
            }
        }

        /// <summary>
        /// Set path to python dll
        /// Open file browser, load path and create new User code processor
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
        /// Decides whether run user code or not
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void OnApplyClicked()
        {
            if (Frame.Vertices == null)
            {
                Message = langContr.NoPointcloud;
                return;
            }
            
            if (UserCode.Trim().Length == 0)
            {
                Message = langContr.NoUserCode;
                return;
            }

            unsafe
            {
                Message = langContr.Processing;
                EnabledApply = false;
                RunUserCode();
            }
        }

        /// <summary>
        /// Run user code and display
        /// - error if code failed
        /// - new created mesh
        /// </summary>
        async void RunUserCode()
        {
            // parameter variables dictionary
            Dictionary<string, object> vars = new Dictionary<string, object>();
            
            // add variables - parameters
            vars.Add("points", Frame.Vertices);
            vars.Add("uvs", Frame.UVs);
            vars.Add("faces", Frame.TempFaces);

            // execute code
            PointMesh res = await ucp.ExecuteUserCode(UserCode, vars);
            if (res == null)
            {
                // error occured
                string erMsg = ucp.ERROR_MSG;

                // edit error to match displayed line numbering
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
                // set recieved data
                var frame = new RealS.MeshFrame
                {
                    Vertices = res.points,
                    UVs = res.uvs,
                    TempFaces = new int[res.faces.Length],
                    Faces = new int[res.faces.Length],
                    Ply = Frame.Ply,
                    Colors = Frame.Colors,
                    Width = Frame.Width
                };
                //Frame;
                
                Array.Copy(res.faces, frame.Faces, frame.Faces.Length);
                Array.Copy(res.faces, frame.TempFaces, frame.TempFaces.Length);

                Frame = frame;
                Message = langContr.CodeExec;

                BuildMesh(Frame);
            }

            if (ucp.GetStatusOfInit())
                DllEnabled = false;
            EnabledApply = true;
        }

        /// <summary>
        /// Update Auto send label
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
        /// Update model according to a new value of triangle thresholding slider
        /// </summary>
        public void UpdateSliderModel()
        {
            if (!_meshBuffer.HasValue) return;

            MeshProcessor.GenerateFaces(_meshBuffer.Value, (float)FiltData.ThresholdSlider);

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
            BuildMesh(_frame);
        }

        /// <summary>
        /// Timer tick
        /// If time ran out
        /// - make a new snapshot
        /// - send mesh to server
        /// Always
        /// - update label
        /// </summary>
        /// <param name="sender"> Event sender </param>
        /// <param name="e"> Event arguments </param>
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
                OnSnapshot();
                OnSendMesh();
            }

            UpdateAutoLabel(nextAuto);
        }

        /// <summary>
        /// Build new geometry to display from captured frame
        /// - create a mesh
        /// - create a point cloud
        /// </summary>
        /// <param name="frame"> Captured frame </param>
        private void BuildMesh(RealS.MeshFrame frame)
        {
            _meshBuffer = frame;
            
            // generate triangles from frame data
            MeshProcessor.GenerateFaces(frame, (float)FiltData.ThresholdSlider);

            // create mesh from frame
            MeshGeometry3D d = MeshProcessor.CreateMeshFromFrame(frame);

            // create material to display on mesh
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
        /// - model is rotated by -90 in y, and -90 in x and scaled 3x
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
            if (FiltData.PointFilter)
                this.Points = pc;
        }

        /// <summary>
        /// Reset filter values back to defaults
        /// </summary>
        internal void OnResetFilters()
        {
            if (!RealS.Started)
            {
                Message = langContr.NoCam;
                return;
            }

            FiltData.SetToDefault();
            float[] fD = new float[] {
                // Decimation filter
                FiltData.LinScaleFac,
                
                // Threshold filter
                FiltData.MinValueТh,
                FiltData.MaxValueTh,
                
                // Spatial smoothing
                FiltData.IterationsSpat,
                1-FiltData.AlphaSpat,
                FiltData.DeltaSpat,
                FiltData.HoleSpat,
                
                // Temporal smoothing
                1-FiltData.AlphaTemp,
                FiltData.DeltaTemp,
                FiltData.PersIndex,

                // Hole filling
                FiltData.HoleMethod
            };

            RealS.updateFilters(FiltData.Filters, fD);
        }

        /// <summary>
        /// Filters changed
        /// - create new array with filter data
        /// - send to realsense
        /// </summary>
        internal void OnFilterChange()
        {
            if (!RealS.Started)
            {
                Message = langContr.NoCam;
                return;
            }

            float[] fD = new float[] {
                // Decimation filter
                FiltData.LinScaleFac,
                
                // Threshold filter
                FiltData.MinValueТh,
                FiltData.MaxValueTh,
                
                // Spatial smoothing
                FiltData.IterationsSpat,
                1-FiltData.AlphaSpat,
                FiltData.DeltaSpat,
                FiltData.HoleSpat,
                
                // Temporal smoothing
                1-FiltData.AlphaTemp,
                FiltData.DeltaTemp,
                FiltData.PersIndex,

                // Hole filling
                FiltData.HoleMethod
            };

            RealS.updateFilters(FiltData.Filters, fD);
        }

        /// <summary>
        /// Set up connection to a server
        /// Or disconnect
        /// Depends on whether client already is connected to a server or not
        /// </summary>
        private async void OnConnect()
        {
            // Currently connecting
            if (connection._inConnectProcess) // || connection.GetSessionState() == SessionState.Reconnecting)
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
                    this.ConnectBtnLbl = langContr.DisconnectMNI;
                    EnabledURL = false;
                    connection.SessionClient.Reconnecting += ClientReconnecting;
                    connection.SessionClient.Reconnected += ClientReconnected;
                }
                else
                {
                    this.ConnectBtnLbl = langContr.ConnectMNI;
                    this.Message = langContr.CantConnect + connection.ErrorMessage;
                    EnabledURL = true;
                }

            }

            // Connected -> disconnecting
            else
            {
                _autoEnable = false;
                UpdateAutoLabel(0);

                bool res = await connection.DisConnect();
                if (res)
                {
                    this.ConnectBtnLbl = langContr.ConnectMNI;
                    Message = langContr.Disconnected;
                    EnabledURL = true;
                }
                else
                {
                    this.Message = langContr.CantConnect + connection.ErrorMessage;
                    EnabledURL = true;
                }

            }

            // Update menu items
            UpdateMenuItems();
            connection._inConnectProcess = false;
        }

        /// <summary>
        /// Update connection related menu items
        /// </summary>
        private void UpdateMenuItems()
        {
            if (connection.IsClientDisconnected())
            {
                connection._connected = !connection._connected;

                this.ConnectBtnLbl = connection._connected ? langContr.DisconnectMNI : langContr.ConnectMNI;
                EnabledButtons = connection._connected;
                EnabledURL = !connection._connected;
            }
        }

        /// <summary>
        /// Update menu items if application is currently reconnecting
        /// </summary>
        private void ReconnectingMenuItems()
        {
            EnableConnect = false;
            EnabledButtons = false;
            ConnectBtnLbl = langContr.ReconnectMNI;
        }

        /// <summary>
        /// If client has disconnected / has been disconnected
        /// </summary>
        /// <param name="e"> Exception </param>
        private void SessionClient_Disconnected(Exception e)
        {
            EnableConnect = true;
            UpdateMenuItems();
            Message = langContr.Disconnected;
        }

        /// <summary>
        /// Action called when client looses connection to server and is trying to reconnect
        /// </summary>
        /// <param name="e"></param>
        private void ClientReconnecting(Exception e)
        {
            ReconnectingMenuItems();
            Message = langContr.Reconnecting;
        }

        /// <summary>
        /// Action called when client reconnects to server
        /// </summary>
        private void ClientReconnected()
        {
            ConnectBtnLbl = langContr.DisconnectMNI;
            EnableConnect = true;
            EnabledButtons = true;
            Message = langContr.Reconnected;
        }

        /// <summary>
        /// Downloading mesh + texture from server
        /// </summary>
        private async void OnMeshDownloaded()
        {
            if (connection.GetSessionState() != SessionState.Connected)
                return;

            WorldObjectDto wo = await connection.GetWorldObject(MESH_NAME + "_" + clientName);
            // WorldObjectDto tex = await connection.GetWorldObject(MESH_TEXTURE_NAME);

            if (wo == null) // || tex == null)
            {
                Message = langContr.MeshNotOnSer;
                return;
            }

            var mesh = MeshProcessor.CreateMeshFrameFromWO(wo);
            _frame = mesh;
            BuildMesh(mesh);
        }

        /// <summary>
        /// Send mesh and ply to server
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
            bool resM = false, resP = false;

            //send ply
            if (frame.Ply!=null){
                var w = WorldObjectProcessor.CreatePlyWO(frame.Ply, PLY_NAME + "_" + clientName);

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
                var w = WorldObjectProcessor.CreateMeshWO(frame, MESH_NAME + "_" + clientName);

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
                    Message = success ? langContr.OpenedRSFile : langContr.CouldNotOpen;

                    SavePlyBtnEnable = success;
                });
                t.Start();
            }

            // freezeDepth = false;
        }

        /// <summary>
        /// Connect camera
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
            ProcessingWindow.FreezeBuffer(false);
            saveDialog.Reset();
        }

        /// <summary>
        /// Change point visibility
        /// </summary>
        internal void OnPointVisibilityChange()
        {
            // if true - set to points
            if (FiltData.PointFilter)
                this.Points = _pointsStorage;
            // if untrue - unset points
            else
                this.Points = new Point3DCollection();

            RaisePropertyChanged(POINTS_PROPERTY);
        }

    }
}