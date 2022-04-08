using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
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


        private VirtualWorldServerConnection serverConnection;

        private ISessionClient sessionClient;


        private const string MESSAGE_PROPERTY = "Message";

        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";
        private const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";

        public const string MESH_NAME = "DepthMesh";
        public const string MESH_TYPE = "Mesh";
        public const string PLY_NAME = "DepthPly";
        public const string PLY_TYPE = "PlyFile";


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

            OnConnectCamera();
        }


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

        public static string ServerUrl { get; set; } = "https://localhost:49155/";

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

        private bool inConnectProcess = false;
        private bool savePlyBtnEnable;

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
                    serverConnection = new VirtualWorldServerConnection(this.sessionClient);

                    serverConnection.OnGetAllWorldObjects((list) => { });

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

                w.Properties =
                    new MeshWorldObjectSerializer().SerializeProperties(frame.Vertices, frame.Faces, "Triangle");


                // adding mesh file
                await this.serverConnection.AddWorldObjectAsync(w);
                Console.WriteLine("Mesh Sent");


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

                await this.serverConnection.AddWorldObjectAsync(w);
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

                    Console.WriteLine(Message +": "+ openDialog.FileName);
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