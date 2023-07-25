using System;
using System.Threading.Tasks;
using ZCU.TechnologyLab.Common.Connections.Client.Data;
using ZCU.TechnologyLab.Common.Connections.Client.Session;
using ZCU.TechnologyLab.Common.Connections.Repository.Server;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    /// <summary>
    /// Class handling the connection to a server
    /// </summary>
    internal class ConnectionHandler
    {
        // signifiers
        /// <summary> In connection process </summary>
        public bool _inConnectProcess;
        /// <summary> Is connected  </summary>
        public bool _connected;
        /// <summary> Was last time an object was sent to server an update or not </summary>
        public bool wasAnUpdate;

        // networking
        /// <summary> Server data adapter </summary>
        private ServerDataAdapter _dataConnection;
        /// <summary> SignalR session </summary>
        private SignalRSession _sessionClient;
        public SignalRSession SessionClient { get => _sessionClient; set => _sessionClient = value; }
        /// <summary> Server session adapter </summary>
        private ServerSessionAdapter _sessionConnection;

        /// <summary> Last error message </summary>
        private string errorMessage;

        /// <summary>
        /// Deletes error message after getting
        /// </summary>
        public string ErrorMessage { get { string msg = errorMessage; errorMessage = ""; return msg; } set => errorMessage = value; }


        /// <summary>
        /// Get session state
        /// </summary>
        /// <returns> Session state </returns>
        public SessionState GetSessionState()
        {
            return SessionClient.State;
        }

        /// <summary>
        /// Set up connection to a server.
        /// </summary>
        /// <returns> True if successful, false if not </returns>
        public async Task<bool> Connect(string serverUrl, Action<Exception> onDisconnected)
        {
            var restClient = new RestDataClient(serverUrl);
            _dataConnection = new ServerDataAdapter(restClient);

            var signalrClient = new SignalRSession(serverUrl, "virtualWorldHub");
            signalrClient.Disconnected += onDisconnected;
            SessionClient = signalrClient;

            _sessionConnection = new ServerSessionAdapter(signalrClient);

            try
            {
                await SessionClient.InitializeAsync();

                if (SessionClient is { State: SessionState.Connected })
                    await SessionClient.StopSessionAsync();

                await SessionClient.StartSessionAsync();
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Disconnect from server.
        /// </summary>
        /// <returns> True if successful false if not </returns>
        public async Task<bool> DisConnect()
        {
            try
            {
                await SessionClient.StopSessionAsync();
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns if the client has disconnected
        /// </summary>
        /// <returns> True if disconnected, false if not </returns>
        public bool IsClientDisconnected()
        {
            if ((!this._connected && this.SessionClient.State == SessionState.Connected)
                 || (this._connected && this.SessionClient.State == SessionState.Closed))
                return true;

            return false;
        }
       
        /// <summary>
        /// Get world object from server
        /// </summary>
        /// <param name="name"> Name of the world object </param>
        /// <returns> World object, null if object not found on server </returns>
        public async Task<WorldObjectDto> GetWorldObject(string name)
        {
            WorldObjectDto wo;
            try
            {
                bool res = await _dataConnection.ContainsWorldObjectAsync(name);
                if (res)
                    wo = await _dataConnection.GetWorldObjectAsync(name);
                else throw new Exception("Object not found on server");
            }
            catch
            {
                ErrorMessage = $"{name} not found on server";
                return null;
            }

            return wo;
        }

        /// <summary>
        /// Sénd world object to server
        /// </summary>
        /// <param name="wo"> World object </param>
        /// <returns> True if successfull, false if not </returns>
        public async Task<bool> SendWorldObject(WorldObjectDto wo)
        {
            // TODO returns if send or update!

            try
            {
                wasAnUpdate = false;
                // is object already on server
                bool res = await _dataConnection.ContainsWorldObjectAsync(wo.Name);
                if (!res)
                    await _dataConnection.AddWorldObjectAsync(wo);
                else
                {
                    wasAnUpdate = true;
                    var w = new WorldObjectUpdateDto
                    {
                        Type = wo.Type,
                        Properties = wo.Properties,
                        Position = wo.Position,
                        Scale = wo.Scale,
                        Rotation = wo.Rotation
                    };

                    await _dataConnection.UpdateWorldObjectAsync(wo.Name, w);
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes world object from server
        /// </summary>
        /// <param name="name"> true if successfull, false if not </param>
        /// <returns></returns>
        public async Task<bool> RemoveWorldObject(string name)
        {
            try
            {
                bool res = await _dataConnection.ContainsWorldObjectAsync(name);
                if (res)
                    await this._dataConnection.RemoveWorldObjectAsync(name);
                else throw new Exception("Object not found on server");

            }
            catch (Exception ex)
            {
                ErrorMessage = $"{name} not found on server";
                return false;
            }

            return true;
        }

    }
}
