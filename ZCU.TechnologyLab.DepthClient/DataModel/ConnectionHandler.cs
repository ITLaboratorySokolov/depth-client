using System;
using System.Threading.Tasks;
using ZCU.TechnologyLab.Common.Connections.Client.Data;
using ZCU.TechnologyLab.Common.Connections.Client.Session;
using ZCU.TechnologyLab.Common.Connections.Repository.Server;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    internal class ConnectionHandler
    {
        // signifiers
        public bool _inConnectProcess;
        public bool _connected;

        // networking
        private ServerDataAdapter _dataConnection;
        private SignalRSession _sessionClient;
        private ServerSessionAdapter _sessionConnection;

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
            return _sessionClient.State;
        }

        /// <summary>
        /// Set up connection to a server.
        /// </summary>
        /// <returns> True if successful, false if not </returns>
        public async Task<bool> Connect(string serverUrl, EventHandler<Exception> onDisconnected)
        {
            var restClient = new RestDataClient(serverUrl);
            _dataConnection = new ServerDataAdapter(restClient);

            var signalrClient = new SignalRSession(serverUrl, "virtualWorldHub");
            signalrClient.Disconnected += onDisconnected;
            _sessionClient = signalrClient;

            _sessionConnection = new ServerSessionAdapter(signalrClient);

            try
            {
                if (_sessionClient is { State: SessionState.Connected })
                    await _sessionClient.StopSessionAsync();

                await _sessionClient.StartSessionAsync();
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
                await _sessionClient.StopSessionAsync();
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
            if ((!this._connected && this._sessionClient.State == SessionState.Connected)
                 || (this._connected && this._sessionClient.State == SessionState.Closed))
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
                wo = await _dataConnection.GetWorldObjectAsync(name);
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
            try
            {
                //if object already on server wanna update instead
                await _dataConnection.AddWorldObjectAsync(wo);
            }
            catch (Exception ex)
            {
                var w = new WorldObjectUpdateDto
                {
                    Type = "PlyFile",
                    Properties = wo.Properties,
                    Position = wo.Position,
                    Scale = wo.Scale,
                    Rotation = wo.Rotation
                };

                await _dataConnection.UpdateWorldObjectAsync(wo.Name, w);
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
                await this._dataConnection.RemoveWorldObjectAsync(name);
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
