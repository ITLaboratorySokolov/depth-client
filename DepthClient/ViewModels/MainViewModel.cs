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

        /// <summary>
        /// Message.
        /// </summary>
        private string message;

        /// <summary>
        /// Multiple start calls throw an error so check if it is already connecting.
        /// </summary>
        private bool connecting;

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
          


            this.Connect = new Command(this.OnConnect);
            this.SendImage = new Command(this.OnSendImage);
            this.hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:49165/echoHub")
                .Build();
            this.imageHubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:49165/virtualWorldHub")
                .Build();

            this.hubConnection.On("Echo", () => this.Message = "Connected");
            this.imageHubConnection.On("UpdateWorldObject", () => this.Message = "Object came updated");
            this.imageHubConnection.On("GetAllWorldObjects", (List<WorldObject> worldObjects) =>
            {
                bool found = false; 
                foreach (var worldObject in worldObjects)
                {
                    if (worldObject.Name == "DepthImage")
                    {
                        var pixel = worldObject.Properties["pixel"];
                        this.Message = "Karel on server with pixel: "+pixel;
                        found = true;
                        break;
                    }
                }
                if(!found)
                    this.Message = "Karel not found "+worldObjects.Count;
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

        /// <summary>
        /// Connect to server command
        /// </summary>
        public ICommand Connect { get; private set; }

        public ICommand SendImage { get; private set; }


        /// <summary>
        /// Connects connection to a server.
        /// </summary>
        private async void OnConnect()
        {
            if (this.connecting) return;

            this.connecting = true;

            try
            {
                await this.hubConnection.StartAsync();
                await this.imageHubConnection.StartAsync();
            } catch (Exception e)
            {
                this.Message = "Cannot connect to a server: " + e.Message;
                Console.WriteLine(e.Message);
            }
        }
        private async void OnSendImage()
        {
            var depth = ProcessingWindow.buffer[1280*720/2]*ProcessingWindow.DepthScale;
            WorldObject w = new()
            {
                Name = "DepthImage",
                Type = "DepthBitmap"
               
            };
            w.Properties = new Dictionary<string, string>();
            w.Properties["pixel"] = "" + depth + "m";
            
            Console.WriteLine("Snapshot taken of pixel: " + w.Properties["pixel"]+" . "+ ProcessingWindow.DepthScale+" "+ ProcessingWindow.DepthWidth);

            await this.imageHubConnection.InvokeAsync<WorldObject>("AddWorldObject",w);

        }

    }
}
