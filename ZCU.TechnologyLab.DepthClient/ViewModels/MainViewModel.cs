using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Input;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;
using Intel.RealSense;
using Microsoft.Win32;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Connections.Client.Data;
using ZCU.TechnologyLab.Common.Connections.Client.Session;
using ZCU.TechnologyLab.Common.Connections.Repository.Server;
using ZCU.TechnologyLab.Common.Serialization.Bitmap;
using ZCU.TechnologyLab.Common.Serialization.Mesh;
using ZCU.TechnologyLab.DepthClient.DataModel;

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
        private const string AUTO_PROPERTY = "AutoEnabledLbl";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";
        public const string MODEL_PROPERTY = "Model";
        public const string FRAME_PROPERTY = "Frame";
        public const string USER_CODE = "UserCode";

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

        // filters
        private readonly bool[] _filters = Enumerable.Repeat(true, 5).ToArray();
        private bool _pointFilter = true;
        private double _thresholdSlider = 0.2;
        
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
        private string _userCode;

        // networking
        ConnectionHandler connection;
        private string _autoLbl="AutoSend: OFF";
        private bool _autoEnable;


        #region PublicProperties

        public static string ServerUrl { get; set; } = "https://localhost:49153/";

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

        public bool PointFilter
        {
            get => _pointFilter;
            set
            {
                this._pointFilter = value;
                OnPointFilterChange();
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
            connection = new ConnectionHandler();

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

            // Set up default view
            OnConnectCamera();
            BuildMeshDefault();

            // Set up timer
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }

        /// <summary>
        /// Reaction to Apply button clicked
        /// </summary>
        /// <exception cref="Exception"></exception>
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
                Message = "No Camera or Server connected";
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
                Console.WriteLine("Sending mesh");
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

            Console.WriteLine("Elapsed for mesh build " + watch.ElapsedMilliseconds);

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
            Console.WriteLine("Updating filters");
            RealS.updateFilters(_filters);
        }

        // TODO Connection to different class

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
                Message = "Connecting";
                bool res = await connection.Connect(ServerUrl, SessionClient_Disconnected);
                if (res)
                {
                    this.Message = "Connected to server: " + ServerUrl;
                    this.ConnectBtnLbl = "Disconnect";
                }
                else
                {
                    this.ConnectBtnLbl = "Connect";
                    this.Message = "Cannot connect to a server: " + connection.ErrorMessage;
                }

            }
            else
            {
                _autoEnable = false;
                UpdateAutoLabel(0);

                bool res = await connection.DisConnect();
                if (res)
                {
                    this.ConnectBtnLbl = "Connect";
                    Message = "Disconnected";
                }
                else
                {
                    this.Message = "Cannot disconnect to a server: " + connection.ErrorMessage;
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

                this.ConnectBtnLbl = connection._connected ? "Disconnect" : "Connect";
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
            Message = "Disconnected from server";
        }

        /// <summary>
        /// Downloading mesh + texture from server
        /// </summary>
        private async void OnMeshDownloaded()
        {
            if (connection.GetSessionState() != SessionState.Connected)
                return;

            WorldObjectDto wo = await connection.GetWorldObject(MESH_NAME);
            WorldObjectDto tex = await connection.GetWorldObject(MESH_TEXTURE_NAME);

            if (wo == null || tex == null)
            {
                Message = "Mesh with texture not found on server";
                return;
            }

            var mesh = MeshProcessor.CreateMeshFraneFromWO(wo, tex);
            BuildMesh(mesh);
        }

        /// <summary>
        /// Send mesh to server
        /// </summary>
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
            bool resT, resM, resP = false;


            //send ply
            if(frame.Ply!=null){
                var p = new Dictionary<string, byte[]>
                {
                    ["Data"] = frame.Ply
                };
                var w = new WorldObjectDto
                {
                    Name = PLY_NAME,
                    Type = "PlyFile",
                    Properties = p,
                    Position = new RemoteVectorDto(),
                    Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                    Rotation = new RemoteVectorDto()
                };
                w.Position.X = 0;
                w.Position.Y = 0;
                w.Position.Z = 0;

                resP = await connection.SendWorldObject(w);
            }

            //send mesh
            {
                MeshProcessor.GenerateFaces(frame, (float)_thresholdSlider);

                var properties =
                    new RawMeshSerializer().Serialize(frame.Vertices,frame.TempFaces, "Triangle", MESH_TEXTURE_NAME, frame.UVs);
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

                resM = await connection.SendWorldObject(w);
            }

            //send texture
            {
                var properties =
                    new BitmapSerializerFactory().RawBitmapSerializer.Serialize(frame.Width, frame.Height, "RGB", frame.Colors);

                var w = new WorldObjectDto
                {
                    Name = MESH_TEXTURE_NAME,
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

            if (resM && resP && resT)
                Message = "Mesh & Ply File Sent to server as " + MESH_NAME + "," + PLY_NAME+", "+ MESH_TEXTURE_NAME;
            else
                Message = "Mesh & Ply File Updated on server";

        }

        /// <summary>
        /// Remove mesh from server
        /// </summary>
        private async void OnRemoveImage()
        {
            bool mRes = await connection.RemoveWorldObject(MESH_NAME);
            bool pRes = await connection.RemoveWorldObject(PLY_NAME);
            bool tMes = await connection.RemoveWorldObject(MESH_TEXTURE_NAME);

            if (mRes && pRes && tMes)
                this.Message = "Ply & Mesh removed from server";
            else
                this.Message = "No Ply & Mesh found on server";
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
                        ? "Opened RealSense file"
                        : "Could not open file";

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
                Message = success ? "Camera connected" : "Camera not found";
                SavePlyBtnEnable = success;
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