using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public class LanguageController : NotifyingClass
    {

        string _fileHeader = "_File";
        string _openBAG = "_Open BAG";
        string _openCamera = "_Open Camera";
        string _savePLY = "_Save PLY";

        string _serverMN = "_Server";

        string _disconnect = "Disconnect";
        string _connect = "Connect";
        
        string _sendMeshMNI = "_Send Mesh";
        string _deleteMeshMNI = "_Delete Mesh";
        string _dwnldMeshMNI = "_Download Mesh";

        string _pythonMN = "_Python";
        string _pythonPathMNI = "_Set path to python";

        string _settingsMN = "_Settings";
        string _languageMNI = "Set to _CZ";
        string _setNameMNI = "Client _name";

        string _autoMenu = "Auto send: OFF"; // TODO OFF/ON + secs

        string _snapshotBT = "Snapshot";
        string _resetBT = "Reset view";
        private string _applyCodeBT = "Apply";

        string _decimateLBL = "Decimate";
        string _thresholdLBL = "Threshold";

        string _smoothing = "Smoothing";
        string _disparityLBL = "Disparity";
        string _spatialLBL = "Spatial";
        string _temporalLBL = "Temporal";
        
        string _triangleThLBL = "Triangle threshold:";
        string _verticesLBL = "Visible vertices";

        //
        string py_dialog_Title = "Select python.dll file";
        string py_success = "Python dll path set";
        string py_unsucc = "Python dll path not set";
        string no_pointcloud = "No pointcloud availible";
        string processing = "Processing...";
        string codeExec = "Code executed";

        string noCamOrSer = "No Camera or Server connected";
        string noCam = "No camera connected";
        string camConn = "Camera connected";
        string camNotFound = "Camera not found";

        string noMesh = "No mesh available";
        string connecting = "Connecting";
        string cantConnect = "Cannot connect to server: ";
        string connectedToSer = "Connected to server: ";
        string disconnected = "Disconnected from server";
        string meshNotOnSer = "Mesh not found on server";
        string serUnavail = "Server currently unavailible";
        string meshSent = "Mesh & Ply File Sent to server as ";
        string meshNotSent = "Mesh & Ply File Updated on server";
        string meshRemoved = "Ply & Mesh removed from server";
        string meshAndPlyNotFound = "No Ply & Mesh found on server";
        string openedRSFile = "Opened RealSense file";
        string couldNotOpen = "Could not open file";

        string noSnap = "No snapshot taken yet";
        string plySaved = "Ply file saved";

        string nameQuestion = "Input client name:";
        string nameChange = "Client name changed to: ";

        //

        string lang = "EN";

        public string FileHeader { get => this._fileHeader; set => this._fileHeader = value; }
        public string OpenBAG { get => _openBAG; set => _openBAG = value; }
        public string OpenCamera { get => _openCamera; set => _openCamera = value; }
        public string SavePLY { get => _savePLY; set => _savePLY = value; }
        public string ServerMN { get => _serverMN; set => _serverMN = value; }
        public string SendMeshMNI { get => _sendMeshMNI; set => _sendMeshMNI = value; }
        public string DeleteMeshMNI { get => _deleteMeshMNI; set => _deleteMeshMNI = value; }
        public string DwnldMeshMNI { get => _dwnldMeshMNI; set => _dwnldMeshMNI = value; }
        public string PythonMN { get => _pythonMN; set => _pythonMN = value; }
        public string PythonPathMNI { get => _pythonPathMNI; set => _pythonPathMNI = value; }
        public string SettingsMN { get => _settingsMN; set => _settingsMN = value; }
        public string LanguageMNI { get => _languageMNI; set => _languageMNI = value; }
        public string AutoMenu { get => _autoMenu; set => _autoMenu = value; }
        public string SnapshotBT { get => _snapshotBT; set => _snapshotBT = value; }
        public string ResetBT { get => _resetBT; set => _resetBT = value; }
        public string ApplyCodeBT { get => _applyCodeBT; set => _applyCodeBT = value; }
        public string DecimateLBL { get => _decimateLBL; set => _decimateLBL = value; }
        public string ThresholdLBL { get => _thresholdLBL; set => _thresholdLBL = value; }
        public string DisparityLBL { get => _disparityLBL; set => _disparityLBL = value; }
        public string SpatialLBL { get => _spatialLBL; set => _spatialLBL = value; }
        public string TemporalLBL { get => _temporalLBL; set => _temporalLBL = value; }
        public string VerticesLBL { get => _verticesLBL; set => _verticesLBL = value; }
        public string PyDialogTitle { get => py_dialog_Title; set => py_dialog_Title = value; }
        public string PySuccess { get => py_success; set => py_success = value; }
        public string Py_unsucc { get => py_unsucc; set => py_unsucc = value; }
        public string NoPointcloud { get => no_pointcloud; set => no_pointcloud = value; }
        public string Processing { get => processing; set => processing = value; }
        public string CodeExec { get => codeExec; set => codeExec = value; }
        public string NoCamOrSer { get => noCamOrSer; set => noCamOrSer = value; }
        public string NoMesh { get => noMesh; set => noMesh = value; }
        public string NoCam { get => noCam; set => noCam = value; }
        public string Disconnect { get => _disconnect; set => _disconnect = value; }
        public string Connect { get => _connect; set => _connect = value; }
        public string CantConnect { get => cantConnect; set => cantConnect = value; }
        public string ConnectedToSer { get => connectedToSer; set => connectedToSer = value; }
        public string Connecting { get => connecting; set => connecting = value; }
        public string Disconnected { get => disconnected; set => disconnected = value; }
        public string MeshNotOnSer { get => meshNotOnSer; set => meshNotOnSer = value; }
        public string SerUnavail { get => serUnavail; set => serUnavail = value; }
        public string MeshNotSent { get => meshNotSent; set => meshNotSent = value; }
        public string MeshSent { get => meshSent; set => meshSent = value; }
        public string MeshAndPlyNotFound { get => meshAndPlyNotFound; set => meshAndPlyNotFound = value; }
        public string MeshRemoved { get => meshRemoved; set => meshRemoved = value; }
        public string CouldNotOpen { get => couldNotOpen; set => couldNotOpen = value; }
        public string OpenedRSFile { get => openedRSFile; set => openedRSFile = value; }
        public string CamConn { get => camConn; set => camConn = value; }
        public string CamNotFound { get => camNotFound; set => camNotFound = value; }
        public string NoSnap { get => noSnap; set => noSnap = value; }
        public string PlySaved { get => plySaved; set => plySaved = value; }
        public string TriangleThLBL { get => _triangleThLBL; set => _triangleThLBL = value; }
        public string SetNameMNI { get => _setNameMNI; set => _setNameMNI = value; }
        public string NameQuestion { get => nameQuestion; set => nameQuestion = value; }
        public string NameChange { get => NameChange1; set => NameChange1 = value; }
        public string NameChange1 { get => nameChange; set => nameChange = value; }
        public string Smoothing { get => _smoothing; set => _smoothing = value; }

        public LanguageController()
        {

        }

        internal void SwapLanguage()
        {
            if (lang == "CZ")
                lang = "EN";
            else if (lang == "EN")
                lang = "CZ";

            if (lang == "CZ")
            {
                FileHeader = "_Složka";
                OpenBAG = "_Otevřít BAG";
                OpenCamera = "_Připojit kameru";
                SavePLY = "_Uložit PLY";

                _disconnect = "Odpojit";
                _connect = "Připojit";

                ServerMN = "_Server";
                SendMeshMNI = "_Poslat mesh";
                DeleteMeshMNI = "_Smazat mesh";
                DwnldMeshMNI = "S_táhnout mesh";

                PythonMN = "_Python";
                PythonPathMNI = "_Nastavit cestu";

                SettingsMN = "_Nastavení";
                LanguageMNI = "Přepnout do _EN";
                SetNameMNI = "Jméno klienta";

                SnapshotBT = "Snímek";
                ResetBT = "Reset náhl.";
                ApplyCodeBT = "Provést";

                DecimateLBL = "Decimovat trojúh.";
                ThresholdLBL = "Oříznout";

                Smoothing = "Vyhlazování";
                DisparityLBL = "V disparity doméně";
                SpatialLBL = "Prostorové";
                TemporalLBL = "Časové";

                TriangleThLBL = "Prahování trojúh.:";
                VerticesLBL = "Viditelné vrcholy";

                py_dialog_Title = "Vyberte python.dll soubor";
                py_success = "Python dll path nastavena";
                py_unsucc = "Python dll path nenastavena";
                no_pointcloud = "Žádný dostupný pointcloud";
                processing = "Zpracovávám...";
                codeExec = "Kód vykonán";

                noCamOrSer = "Kamera nebo server nedostupné";
                noCam = "Kamera není nepřipojena";
                camConn = "Kamera připojena";
                camNotFound = "Kamera nenalezena";

                noMesh = "Žádná dostupná mesh";
                connecting = "Probíhá připojování";
                cantConnect = "Nelze se připojit k serveru: ";
                connectedToSer = "Připojeno k serveru: ";
                disconnected = "Odpojeno od serveru";
                meshNotOnSer = "Mesh se nenachází na serveru";
                serUnavail = "Server momentálně nedostupný";
                meshSent = "Mesh & Ply soubory poslány na server jako ";
                meshNotSent = "Mesh & Ply soubory updatovány na serveru";
                meshRemoved = "Mesh & Ply soubory odstraněný ze serveru";
                meshAndPlyNotFound = "No Ply & Mesh found on server";
                openedRSFile = "Otevřen RealSense soubor";
                couldNotOpen = "Soubor nelze otevřít";

                noSnap = "Dosud nebyl vytvořen žádný snímek";
                plySaved = "Ply soubor uložen";

                NameQuestion = "Zadejte jméno klienta:";
                NameChange = "Jméni kienta změněno na: ";

            }
            else if (lang == "EN")
            {
                FileHeader = "_File";
                OpenBAG = "_Open BAG";
                OpenCamera = "_Open Camera";
                SavePLY = "_Save PLY";

                _disconnect = "Disconnect";
                _connect = "Connect";

                ServerMN = "_Server";
                SendMeshMNI = "_Send Mesh";
                DeleteMeshMNI = "_Delete Mesh";
                DwnldMeshMNI = "_Download Mesh";

                PythonMN = "_Python";
                PythonPathMNI = "_Set path to python";

                SettingsMN = "_Settings";
                LanguageMNI = "Set to _CZ";
                SetNameMNI = "Client name";

                SnapshotBT = "Snapshot";
                ResetBT = "Reset view";
                ApplyCodeBT = "Apply";

                DecimateLBL = "Decimate triang.";
                ThresholdLBL = "Threshold";
                
                Smoothing = "Smoothing";
                DisparityLBL = "In disparity domain";
                SpatialLBL = "Spatial";
                TemporalLBL = "Temporal";
                
                TriangleThLBL= "Triangle threshold:";
                VerticesLBL = "Visible vertices";

                py_dialog_Title = "Select python.dll file";
                py_success = "Python dll path set";
                py_unsucc = "Python dll path not set";
                no_pointcloud = "No pointcloud availible";
                processing = "Processing...";
                codeExec = "Code executed";

                noCamOrSer = "No Camera or Server connected";
                noCam = "No camera connected";
                camConn = "Camera connected";
                camNotFound = "Camera not found";

                noMesh = "No mesh available";
                connecting = "Connecting";
                cantConnect = "Cannot connect to server: ";
                connectedToSer = "Connected to server: ";
                disconnected = "Disconnected from server";
                meshNotOnSer = "Mesh not found on server";
                serUnavail = "Server currently unavailible";
                meshSent = "Mesh & Ply File sent to server as ";
                meshNotSent = "Mesh & Ply File updated on server";
                meshRemoved = "Mesh & Ply File removed from server";
                meshAndPlyNotFound = "No Ply & Mesh found on server";
                openedRSFile = "Opened RealSense file";
                couldNotOpen = "Could not open file";

                noSnap = "No snapshot taken yet";
                plySaved = "Ply file saved";

                nameQuestion = "Input client name:";
                NameChange = "Client name changed to: ";
            }
        }
    }
}
