using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Intel.RealSense;
using Newtonsoft.Json;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Connections;
using ZCU.TechnologyLab.Common.Connections.Session;
using ZCU.TechnologyLab.Common.Serialization;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// View model for the application.
    /// </summary>
    /// 
    public class MainViewModel : NotifyingClass
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();


        private readonly VirtualWorldServerConnection serverConnection;

        private readonly ISessionClient sessionClient;


        /// <summary>
        /// Name of the Message property.
        /// </summary>
        private const string MESSAGE_PROPERTY = "Message";

        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";

        public const string DLL_PATH = "d435i_walk_around.bag";

        /// <summary>
        /// Message.
        /// </summary>
        private string message;

        private string connectBtnLbl = "Connect";
        private bool enabledButtons = false;

        /// <summary>
        /// Multiple start calls throw an error so check if it is already connecting.
        /// </summary>
        private bool connecting;

        /// <summary>
        /// Is depth map saved on the server
        /// </summary>
        private bool depthMapOnServer = false;


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
            this.SendImage = new Command(this.OnSendImage);
            this.SendMesh = new Command(this.onSendMesh);
            this.RemoveImage = new Command(this.OnRemoveImage);

            this.sessionClient = new SignalRSession("https://localhost:49153/", "virtualWorldHub");

            this.serverConnection = new VirtualWorldServerConnection(this.sessionClient);


            serverConnection.OnGetAllWorldObjects((list) =>
            {
                depthMapOnServer = false;

                bool found = false;
                foreach (var worldObject in list)
                {
                    if (worldObject.Name == "DepthImage")
                    {
                        depthMapOnServer = true;
                        var pixel = worldObject.Properties["pixels"];
                        this.Message = "Found image on server";

                        var pic = Base64ToPicture(pixel);
                        Buffer.BlockCopy(pic, 0, ProcessingWindow.depth_buffer, 0, pic.Length * sizeof(ushort));
                        // ProcessingWindow.FreezeBuffer(true);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    this.Message = "Image not found";
            });
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

        public string ConnectBtnLbl
        {
            get => this.connectBtnLbl;
            set
            {
                this.connectBtnLbl = value;
                RaisePropertyChanged(CNTCBTLB_PROPERTY);
            }
        }

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

        public ICommand SendImage { get; private set; }
        public ICommand SendMesh { get; private set; }
        public ICommand RemoveImage { get; private set; }


        /// <summary>
        /// Connects connection to a server.
        /// </summary>
        private async void OnConnect()
        {
            if (!this.connecting)
            {
                try
                {
                    await this.sessionClient.StartSessionAsync();
                    RealSenseWrapper.Start(DLL_PATH);

                }
                catch (Exception e)
                {
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
                    RealSenseWrapper.Exit();
                }
                catch (Exception e)
                {
                    this.Message = "Cannot disconnect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }

            if ((!this.connecting && this.sessionClient.SessionState == SessionState.Connected)
                || (this.connecting && this.sessionClient.SessionState == SessionState.Closed)
               )
            {
                this.connecting = !this.connecting;

                this.ConnectBtnLbl = this.connecting ? "Disconnect" : "Connect";
                EnabledButtons = this.connecting;

                if (this.connecting)
                {
                    Console.WriteLine("Asking for worldobjects");
                    await serverConnection.GetAllWorldObjectsAsync();
                }
            }
        }

        private String PictureToBase64(ushort[] depths)
        {
            byte[] result = new byte[depths.Length * sizeof(ushort)];
            Buffer.BlockCopy(depths, 0, result, 0, result.Length);
            return Convert.ToBase64String(result);
        }

        private ushort[] Base64ToPicture(String s)
        {
            var c = Convert.FromBase64String(s);
            var result = new ushort[c.Length / sizeof(ushort)];
            Buffer.BlockCopy(c, 0, result, 0, c.Length);
            return result;
        }

        private async void onSendMesh()
        {
            Console.WriteLine("Parsing mesh");
            WorldObjectDto w = new()
            {
                Name = "DepthMesh",
                Type = "Mesh"
            };

            float[] managedVertices=null;
            int[] managedFaces=null;
            byte[] managedPly = null;


            unsafe
            {

                float* vertices;
                int* faces;
                byte* plyBinary;
                int vertexCount;
                int faceCount;
                int plyLength;

                Console.WriteLine("getting frame from realsense dll");
                RealSenseWrapper.GetFrame(out vertices, out faces, out vertexCount, out faceCount,out plyBinary,out plyLength);
                Console.WriteLine("got frame");

                managedVertices = new float[vertexCount];
                managedFaces = new int[faceCount];
                managedPly = new byte[plyLength];

                Console.WriteLine("copying to managed memory");
                Marshal.Copy((IntPtr)vertices, managedVertices, 0, vertexCount);
                Marshal.Copy((IntPtr)faces, managedFaces, 0, faceCount);
                Marshal.Copy((IntPtr)plyBinary, managedPly, 0, plyLength);

                
                RealSenseWrapper.DropFrame(vertices, faces, plyBinary);

            }
            Console.WriteLine("serialization");
            w.Properties = new MeshWorldObjectSerializer().SerializeProperties(managedVertices, managedFaces, "Triangle");
            Console.WriteLine("serialization done");

            // adding mesh file
            await this.serverConnection.AddWorldObjectAsync(w);

            
            //adding ply file to server
            w = new()
            {
                Name = "DepthPly",
                Type = "File"
            };
            w.Properties = new Dictionary<string, string>
            {
                ["data"] =Convert.ToBase64String(managedPly)
            };

            await this.serverConnection.AddWorldObjectAsync(w);


            Console.WriteLine("Sent mesh");

        }

        private async void OnSendImage()
        {
            WorldObjectDto w = new()
            {
                Name = "DepthImage",
                Type = "DepthBitmap"
            };
            w.Properties = new Dictionary<string, string>
            {
                ["pixels"] = PictureToBase64(ProcessingWindow.depth_buffer_live)
            };

            // update snapshot
            //ProcessingWindow.FreezeBuffer(true);
            Buffer.BlockCopy(ProcessingWindow.depth_buffer_live, 0,
                ProcessingWindow.depth_buffer, 0, sizeof(ushort) * ProcessingWindow.depth_buffer_live.Length);

            Console.WriteLine("Snapshot taken of pixels of count: " + w.Properties["pixels"].Length);

            Console.WriteLine("Snapshot taken of pixel: \n . " + ProcessingWindow.DepthScale + " " +
                              ProcessingWindow.DepthWidth);


            if (!depthMapOnServer)
                await this.serverConnection.AddWorldObjectAsync(w);
            else
                await this.serverConnection.UpdateWorldObjectAsync(w);
            depthMapOnServer = true;
        }

        private async void OnRemoveImage()
        {
            await this.serverConnection.RemoveWorldObjectAsync("DepthImage");

            this.Message = "Removed image from server";
            //  ProcessingWindow.FreezeBuffer(false);
            depthMapOnServer = false;
        }

    }
}