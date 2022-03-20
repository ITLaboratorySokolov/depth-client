using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Intel.RealSense;
using ZCU.TechnologyLab.Common.Models;


namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// View model for the application.
    /// </summary>
    public class MainViewModel : NotifyingClass
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        /// <summary>
        /// Name of the Message property.
        /// </summary>
        private const string MESSAGE_PROPERTY = "Message";
        private const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        private const string EN_BTN_PROPERTY = "EnabledButtons";

        /// <summary>
        /// Message.
        /// </summary>
        private string message;
        private string connectBtnLbl="Connect";
        private bool enabledButtons = false;

        /// <summary>
        /// Multiple start calls throw an error so check if it is already connecting.
        /// </summary>
        private bool connecting;

        private bool depthMapOnServer = false;

        /// <summary>
        /// Hub connection.
        /// </summary>
        private readonly HubConnection hubConnection;

        /// <summary>
        /// image Hub connection.
        /// </summary>
        private readonly HubConnection imageHubConnection;


        /// <summary>
        /// Initialize commands and variables.
        /// </summary>
        public MainViewModel()
        {
            AllocConsole();


            var port = 49153;


            this.Connect = new Command(this.OnConnect);
            this.SendImage = new Command(this.OnSendImage);
            this.RemoveImage = new Command(this.OnRemoveImage);
            this.hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:" + port + "/echoHub")
                .Build();
            this.imageHubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:" + port + "/virtualWorldHub")
                .Build();

            this.hubConnection.On("Echo", () => this.Message = "Connected");
            this.imageHubConnection.On("UpdateWorldObject", () => this.Message = "Object came updated");
            this.imageHubConnection.On("GetAllWorldObjects", (List<WorldObject> worldObjects) =>
            {
                depthMapOnServer = false;

                bool found = false;
                foreach (var worldObject in worldObjects)
                {
                    if (worldObject.Name == "DepthImage")
                    {
                        depthMapOnServer = true;
                        var pixel = worldObject.Properties["pixels"];
                        this.Message = "Found image on server";

                        var pic = Base64ToPicture(pixel);
                        Buffer.BlockCopy(pic, 0, ProcessingWindow.depth_buffer, 0, pic.Length * sizeof(ushort));
                        ProcessingWindow.FreezeBuffer(true);
                        found = true;
                        break;
                    }
                    else Console.WriteLine("No image was found");
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
                    await this.hubConnection.StartAsync();
                    await this.imageHubConnection.StartAsync();
                }
                catch (Exception e)
                {
                    this.Message = "Cannot connect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }else
            {
                ProcessingWindow.FreezeBuffer(false);

                try
                {
                    await this.hubConnection.StopAsync();
                    await this.imageHubConnection.StopAsync();
                }
                catch (Exception e)
                {
                    this.Message = "Cannot disconnect to a server: " + e.Message;
                    Console.WriteLine(e.Message);
                }
            }
            if ((!this.connecting && this.hubConnection.State == HubConnectionState.Connected)
            || (this.connecting && this.hubConnection.State == HubConnectionState.Disconnected)
            ) {
                this.connecting = !this.connecting;

                this.ConnectBtnLbl = this.connecting ? "Disconnect" : "Connect";
                EnabledButtons = this.connecting;
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

        private async void OnSendImage()
        {
            WorldObject w = new()
            {
                Name = "DepthImage",
                Type = "DepthBitmap"
            };
            w.Properties = new Dictionary<string, string>
            {
                ["pixels"] = PictureToBase64(ProcessingWindow.depth_buffer_live)
            };

            // update snapshot
            ProcessingWindow.FreezeBuffer(true);
            Buffer.BlockCopy(ProcessingWindow.depth_buffer_live,0,
                ProcessingWindow.depth_buffer,0,sizeof(ushort)*ProcessingWindow.depth_buffer_live.Length);

            Console.WriteLine("Snapshot taken of pixels of count: " + w.Properties["pixels"].Length);

            Console.WriteLine("Snapshot taken of pixel: \n . " + ProcessingWindow.DepthScale + " " +
                              ProcessingWindow.DepthWidth);


            if (!depthMapOnServer)
                await this.imageHubConnection.InvokeAsync<WorldObject>("AddWorldObject", w);
            else
                await this.imageHubConnection.InvokeAsync<WorldObject>("UpdateWorldObject", w);
            depthMapOnServer = true;
        }

        private async void OnRemoveImage()
        {
            await this.imageHubConnection.InvokeAsync<WorldObject>("RemoveWorldObject", "DepthImage");
            this.Message = "Removed image from server";
            ProcessingWindow.FreezeBuffer(false);
            depthMapOnServer = false;
        }
    }
}