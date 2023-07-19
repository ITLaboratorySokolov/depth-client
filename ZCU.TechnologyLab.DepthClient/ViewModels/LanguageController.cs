namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// Class holding CZ and EN strings used in application
    /// </summary>
    public class LanguageController : NotifyingClass
    {
        // MAIN WINDOW UI
        string _fileHeader = "_File";
        string _openBAG = "_Open BAG";
        string _openCamera = "_Open Camera";
        string _savePLY = "_Save PLY";

        string _serverMN = "_Server";

        string _connectMNI = "Connect";
        string _disconnectMNI = "Disconnect";
        string _reconnectMNI = "Reconnecting";

        string _sendMeshMNI = "_Send Mesh";
        string _deleteMeshMNI = "_Delete Mesh";
        string _dwnldMeshMNI = "_Download Mesh";

        string _pythonMN = "_Python";
        string _pythonPathMNI = "_Set path to python";

        string _settingsMN = "_Settings";
        string _languageMNI = "Set to _CZ";
        string _setNameMNI = "Client _name";

        string _autoMenu = "Auto send: OFF"; 

        string _snapshotBT = "Snapshot";
        string _resetBT = "Reset view";
        private string _applyCodeBT = "Apply";

        string _decimateLBL = "Decimate";
        string _thresholdLBL = "Thresholding";
        string _holeLBL = "Hole filling";

        string _smoothing = "Smoothing";
        string _spatialLBL = "Spatial";
        string _temporalLBL = "Temporal";
        
        string _triangleThLBL = "Triangle threshold:";
        string _verticesLBL = "Visible vertices";

        string _filterSettingsBT = "Advanced settings";

        // MESSAGES
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
        string reconnecting = "Trying to reconncet to server";
        string reconnected = "Reconnceted to server";
        string meshNotOnSer = "Mesh not found on server";
        string serUnavail = "Server currently unavailible";
        string meshSent = "Mesh & Ply File Sent to server as ";
        string meshNotSent = "Mesh & Ply File Updated on server";
        string meshRemoved = "Mesh removed from server";
        string meshPlyRemoved = "Ply & Mesh removed from server";
        string plyRemoved = "Ply removed from server";
        string meshAndPlyNotFound = "No Ply & Mesh found on server";
        string openedRSFile = "Opened RealSense file";
        string couldNotOpen = "Could not open file";

        string noSnap = "No snapshot taken yet";
        string plySaved = "Ply file saved";

        string nameQuestion = "Input client name:";
        string nameChange = "Client name changed to: ";

        string noUserCode = "No code to execute";
        string loadFileErr = "File couldn't be read";
        string loadFileSucc = "Code loaded from file";

        // FILTER CONFIGURATION WINDOW UI
        string decFilt = "Decimation filter parameters";
        string linSc = "Linear scale factor";

        string deptFilt = "Depth thresholding parameters";
        string min = "Minimum";
        string max = "Maximum";
        string _disparityLBL = "Smoothig in disparity domain";

        string spatFilt = "Spatial smoothing parameters";
        string it = "Number of iterations";
        string alphaSp = "Alpha";
        string deltaSp = "Delta";
        string holeSp = "Hole filling";

        string tempFilt = "Temporal smoothing parameters";
        string alphaTemp = "Alpha";
        string deltaTemp = "Delta";
        string pers = "Persistency index";

        string holeFilt = "Hole filling parameters";
        string method = "Method";

        // TOOLTIPS
        // Main window
        string _decimateLBLTooltip = "Decreases the size of the created mesh";
        string _thresholdLBLTooltip = "Cuts off objects too close/far";
        string _holeLBLTooltip = "Fills in holes in scanned data";
        string _spatialLBLToolTip = "Smoothing using neighboring pixels";
        string _temporalLBLToolTip = "Smoothing using past frames";
        string _triangleThLBLToolTip = "Filtering out triangles with edges that are too long";
        string _funcHeaderLBLToolTip = "Header of the user function, points - list with vertex coordinates, uvs - list with texture coordinates, faces - list with triangle indices";
        string _autosendToolTip = "Click to turn on/off autosend";
        string _autosendIntToolTip = "Resend interval in seconds";
        string _runBT = "Run code";
        string _saveBT = "Save code to .py file";
        string _loadBT = "Load .py file";

        // Filter configuration
        string _decLinScaleLBLToolTip = "How much is the created mesh scaled down";
        string _disparityLBLToolTip = "Smoothing in domain that represents the difference between projections of left and right cameras";
        string _spAlphaLBLToolTip = "Strength of filter";
        string _spDeltaLBLToolTip = "Establishes the threshold used to preserve edges";
        string _spHoleLBLToolTip = "Horizontal hole-filling mode";
        string _tempAlphaLBLToolTip = "Strength of filter";
        string _tempDeltaLBLToolTip = "Establishes the threshold used to preserve edges";
        string _tempPersLBLToolTip = "Used to decide when the missing pixel value should be corrected with previous data";
        string _persIndex0ToolTip = "No hole filling occurs";
        string _persIndex1ToolTip = "Corrected if the pixel was valid in 8 out of the last 8 frames";
        string _persIndex2ToolTip = "Corrected if the pixel was valid in 2 out of the last 3 frames";
        string _persIndex3ToolTip = "Corrected if the pixel was valid in 2 out of the last 4 frames";
        string _persIndex4ToolTip = "Corrected if the pixel was valid in 2 out of the last 8 frames";
        string _persIndex5ToolTip = "Corrected if the pixel was valid in 1 of the last 2 frames";
        string _persIndex6ToolTip = "Corrected if the pixel was valid in 1 out of the last 5 frames";
        string _persIndex7ToolTip = "Corrected if the pixel was valid in 1 out of the last 8 frames";
        string _persIndex8ToolTip = "Always corrected";
        string _holeMethod0ToolTip = "Use the value from the left neighbor pixel to fill the hole";
        string _holeMethod1ToolTip = "Use the value from the neighboring pixel furthest away from the sensor";
        string _holeMethod2ToolTip = "Use the value from the neighboring pixel closest to the sensor";

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
        public string ConnectMNI { get => _connectMNI; set => _connectMNI = value; }
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
        public string HoleLBL { get => _holeLBL; set => _holeLBL = value; }
        public string DecFilt { get => decFilt; set => decFilt = value; }
        public string LinSc { get => linSc; set => linSc = value; }
        public string DeptFilt { get => deptFilt; set => deptFilt = value; }
        public string Min { get => min; set => min = value; }
        public string Max { get => max; set => max = value; }
        public string SpatFilt { get => spatFilt; set => spatFilt = value; }
        public string It { get => it; set => it = value; }
        public string AlphaSp { get => alphaSp; set => alphaSp = value; }
        public string DeltaSp { get => deltaSp; set => deltaSp = value; }
        public string HoleSp { get => holeSp; set => holeSp = value; }
        public string TempFilt { get => tempFilt; set => tempFilt = value; }
        public string AlphaTemp { get => alphaTemp; set => alphaTemp = value; }
        public string DeltaTemp { get => deltaTemp; set => deltaTemp = value; }
        public string Pers { get => pers; set => pers = value; }
        public string HoleFilt { get => holeFilt; set => holeFilt = value; }
        public string Method { get => method; set => method = value; }
        public string FilterSettingsBT { get => _filterSettingsBT; set => _filterSettingsBT = value; }
        public string DecimateLBLTooltip { get => _decimateLBLTooltip; set => _decimateLBLTooltip = value; }
        public string ThresholdLBLTooltip { get => _thresholdLBLTooltip; set => _thresholdLBLTooltip = value; }
        public string HoleLBLTooltip { get => _holeLBLTooltip; set => _holeLBLTooltip = value; }
        public string SpatialLBLToolTip { get => _spatialLBLToolTip; set => _spatialLBLToolTip = value; }
        public string TemporalLBLToolTip { get => _temporalLBLToolTip; set => _temporalLBLToolTip = value; }
        public string TriangleThLBLToolTip { get => _triangleThLBLToolTip; set => _triangleThLBLToolTip = value; }
        public string DecLinScaleLBLToolTip { get => _decLinScaleLBLToolTip; set => _decLinScaleLBLToolTip = value; }
        public string DisparityLBLToolTip { get => _disparityLBLToolTip; set => _disparityLBLToolTip = value; }
        public string SpAlphaLBLToolTip { get => _spAlphaLBLToolTip; set => _spAlphaLBLToolTip = value; }
        public string SpDeltaLBLToolTip { get => _spDeltaLBLToolTip; set => _spDeltaLBLToolTip = value; }
        public string TempAlphaLBLToolTip { get => _tempAlphaLBLToolTip; set => _tempAlphaLBLToolTip = value; }
        public string TempDeltaLBLToolTip { get => _tempDeltaLBLToolTip; set => _tempDeltaLBLToolTip = value; }
        public string TempPersLBLToolTip { get => _tempPersLBLToolTip; set => _tempPersLBLToolTip = value; }
        public string PersIndex0ToolTip { get => _persIndex0ToolTip; set => _persIndex0ToolTip = value; }
        public string PersIndex1ToolTip { get => _persIndex1ToolTip; set => _persIndex1ToolTip = value; }
        public string PersIndex2ToolTip { get => _persIndex2ToolTip; set => _persIndex2ToolTip = value; }
        public string PersIndex3ToolTip { get => _persIndex3ToolTip; set => _persIndex3ToolTip = value; }
        public string PersIndex4ToolTip { get => _persIndex4ToolTip; set => _persIndex4ToolTip = value; }
        public string PersIndex5ToolTip { get => _persIndex5ToolTip; set => _persIndex5ToolTip = value; }
        public string PersIndex6ToolTip { get => _persIndex6ToolTip; set => _persIndex6ToolTip = value; }
        public string PersIndex7ToolTip { get => _persIndex7ToolTip; set => _persIndex7ToolTip = value; }
        public string PersIndex8ToolTip { get => _persIndex8ToolTip; set => _persIndex8ToolTip = value; }
        public string HoleMethod0ToolTip { get => _holeMethod0ToolTip; set => _holeMethod0ToolTip = value; }
        public string HoleMethod1ToolTip { get => _holeMethod1ToolTip; set => _holeMethod1ToolTip = value; }
        public string HoleMethod2ToolTip { get => _holeMethod2ToolTip; set => _holeMethod2ToolTip = value; }
        public string SpHoleLBLToolTip { get => _spHoleLBLToolTip; set => _spHoleLBLToolTip = value; }
        public string DisconnectMNI { get => _disconnectMNI; set => _disconnectMNI = value; }
        public string NoUserCode { get => noUserCode; set => noUserCode = value; }
        public string FuncHeaderLBLToolTip { get => _funcHeaderLBLToolTip; set => _funcHeaderLBLToolTip = value; }
        public string Reconnecting { get => reconnecting; set => reconnecting = value; }
        public string Reconnected { get => reconnected; set => reconnected = value; }
        public string ReconnectMNI { get => _reconnectMNI; set => _reconnectMNI = value; }
        public string AutosendToolTip { get => _autosendToolTip; set => _autosendToolTip = value; }
        public string AutosendIntToolTip { get => _autosendIntToolTip; set => _autosendIntToolTip = value; }
        public string MeshPlyRemoved { get => meshPlyRemoved; set => meshPlyRemoved = value; }
        public string PlyRemoved { get => plyRemoved; set => plyRemoved = value; }
        public string LoadFileErr { get => loadFileErr; set => loadFileErr=value; }
        public string LoadFileSucc { get => loadFileSucc; set => loadFileSucc=value; }
        public string PySaved { get; internal set; }
        public string PySavedErr { get; internal set; }
        public string RunBT { get => _runBT; set => _runBT = value; }
        public string SavePyBT { get => _saveBT; set => _saveBT = value; }
        public string LoadPyBT { get => _loadBT; set => _loadBT = value; }

        /// <summary>
        /// Get translated connect/disconnect menu item text
        /// </summary>
        /// <param name="txt"> Current text </param>
        /// <returns> Translated text </returns>
        internal string GetConnectText(string txt)
        {
            if (lang == "CZ")
            {
                if (txt == "Connect")
                    return _connectMNI;
                else
                    return _disconnectMNI;
            }
            else
            {
                if (txt == "Připojit")
                    return _connectMNI;
                else
                    return _disconnectMNI;
            }

        }

        /// <summary>
        /// Swap language between CZ and EN
        /// </summary>
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

                ServerMN = "_Server";
                SendMeshMNI = "_Poslat mesh";
                DeleteMeshMNI = "_Smazat mesh";
                DwnldMeshMNI = "S_táhnout mesh";

                PythonMN = "_Python";
                PythonPathMNI = "_Nastavit cestu";

                SettingsMN = "_Nastavení";
                LanguageMNI = "Přepnout do _EN";
                SetNameMNI = "Jméno klienta";

                _connectMNI = "Připojit";
                DisconnectMNI = "Odpojit";
                _reconnectMNI = "Připojování";


                SnapshotBT = "Snímek";
                ResetBT = "Reset náhl.";
                ApplyCodeBT = "Provést";

                DecimateLBL = "Decimační filtr";
                ThresholdLBL = "Oříznout";
                HoleLBL = "Záplatování děr";

                Smoothing = "Vyhlazování";
                SpatialLBL = "Prostorové";
                TemporalLBL = "Časové";

                TriangleThLBL = "Prahování trojúh.:";
                VerticesLBL = "Viditelné vrcholy";

                FilterSettingsBT = "Pokročilé nastavení";

                RunBT = "Spustit kód";
                SavePyBT = "Uložit kód do .py souboru";
                LoadPyBT = "Načíst .py soubor";

                //

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
                reconnecting = "Probíhá připojování k serveru";
                reconnected = "Připojeno k serveru";
                meshNotOnSer = "Mesh se nenachází na serveru";
                serUnavail = "Server momentálně nedostupný";
                meshSent = "Mesh & Ply soubory poslány na server jako ";
                meshNotSent = "Mesh & Ply soubory updatovány na serveru";
                meshRemoved = "Mesh soubor odstraněný ze serveru";
                meshPlyRemoved = "Mesh & Ply soubor odstraněný ze serveru";
                plyRemoved = "Ply soubor odstraněný ze serveru";
                meshAndPlyNotFound = "No Ply & Mesh found on server";
                openedRSFile = "Otevřen RealSense soubor";
                couldNotOpen = "Soubor nelze otevřít";

                noSnap = "Dosud nebyl vytvořen žádný snímek";
                plySaved = "Ply soubor uložen";

                NameQuestion = "Zadejte jméno klienta:";
                NameChange = "Jméno kienta změněno na: ";

                NoUserCode = "Žádný uživatelský kód";
                loadFileErr = "Soubor nemohl být přečten";
                loadFileSucc = "Uživatelský kód načten ze souboru";
                PySaved = "Uživatelský kód uložen do souboru";
                PySavedErr = "Uživatelský kód se nepodařilo uložit";

                //
                DecFilt = "Parametry decimace trojúhelníků";
                LinSc = "Faktor zmenšení";

                DeptFilt = "Parametry oříznutí";
                Min = "Minimum";
                Max = "Maximum";

                DisparityLBL = "Vyhlazování v rozdílové doméně";

                SpatFilt = "Parametry prostorového vyhlazování";
                It = "Počet iterací";
                AlphaSp = "Alfa";
                DeltaSp = "Delta";
                HoleSp = "Záplatování děr";

                TempFilt = "Parametry časového vyhlazování";
                AlphaTemp = "Alfa";
                DeltaTemp = "Delta";
                Pers = "Metoda persistence";

                HoleFilt = "Parametry záplatování děr";
                Method = "Metoda";

                //
                DecimateLBLTooltip = "Zmenšuje velikost vytvořené meshe";
                ThresholdLBLTooltip = "Odstraní objekty moc blízko/daleko";
                HoleLBLTooltip = "Záplatuje díry v naskenovaných datech";
                SpatialLBLToolTip = "Vyhlazování používající pixely z okolí";
                TemporalLBLToolTip = "Vyhlazování používající předchozí snímky";
                TriangleThLBLToolTip = "Odfiltruje trojúhelníky s příliš dlouhými hranami";
                FuncHeaderLBLToolTip = "Hlavička uživatelské funkce, points - seznam se souřadnicemi bodů, uvs - seznam se souřadnicemi do textury, faces - seznam s indexy vrcholů trojúhelníků";
                AutosendToolTip = "Klikněte pro zapnutí/vypnutí automatického posílání na server";
                AutosendIntToolTip = "Délka intervalu po kterém se odesílá na server ve vteřinách";

                DecLinScaleLBLToolTip = "Kolikrát je vytvořená mesh zmenšena";
                DisparityLBLToolTip = "Vyhlazování v doméně reprezentující rozdíl mezi projekcemi levé a pravé kamery";
                SpAlphaLBLToolTip = "Síla filtru";
                SpDeltaLBLToolTip = "Stanoví práh použitý k zachování hran";
                SpHoleLBLToolTip = "Metoda horizontálního záplatování děr";
                TempAlphaLBLToolTip = "Síla filtru";
                TempDeltaLBLToolTip = "Stanoví práh použitý k zachování hran";
                TempPersLBLToolTip = "V jakém případě má být chybějící pixel opraven na předchozí hodnotu";
                PersIndex0ToolTip = "Žádné opravování hodnot";
                PersIndex1ToolTip = "Opravit pokud byl pixel validní v 8 z posledních 8 snímků";
                PersIndex2ToolTip = "Opravit pokud byl pixel validní v 2 z posledních 3 snímků";
                PersIndex3ToolTip = "Opravit pokud byl pixel validní v 2 z posledních 4 snímků";
                PersIndex4ToolTip = "Opravit pokud byl pixel validní v 2 z posledních 8 snímků";
                PersIndex5ToolTip = "Opravit pokud byl pixel validní v 1 z posledních 2 snímků";
                PersIndex6ToolTip = "Opravit pokud byl pixel validní v 1 z posledních 5 snímků";
                PersIndex7ToolTip = "Opravit pokud byl pixel validní v 1 z posledních 8 snímků";
                PersIndex8ToolTip = "Vždy opravit";
                HoleMethod0ToolTip = "Použít hodnotu z levého sousedního pixelu Use the value from the left neighbor pixel to fill the hole";
                HoleMethod1ToolTip = "Použít hodnotu ze sousedního pixelu nejdále od senzoru";
                HoleMethod2ToolTip = "Použít hodnotu ze sousedního pixelu nejblíže k senzoru";

            }
            else if (lang == "EN")
            {
                FileHeader = "_File";
                OpenBAG = "_Open BAG";
                OpenCamera = "_Open Camera";
                SavePLY = "_Save PLY";

                _connectMNI = "Connect";
                DisconnectMNI = "Disconnect";
                _reconnectMNI = "Reconnecting";

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

                DecimateLBL = "Decimation filter";
                ThresholdLBL = "Thresholding";
                HoleLBL = "Hole filling";
                
                Smoothing = "Smoothing";
                SpatialLBL = "Spatial";
                TemporalLBL = "Temporal";
                
                TriangleThLBL= "Triangle threshold:";
                VerticesLBL = "Visible vertices";

                FilterSettingsBT = "Advanced settings";

                RunBT = "Run code";
                SavePyBT = "Save code to .py file";
                LoadPyBT = "Load .py file";

                //

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
                reconnecting = "Trying to reconncet to server";
                reconnected = "Reconnceted to server";
                meshNotOnSer = "Mesh not found on server";
                serUnavail = "Server currently unavailible";
                meshSent = "Mesh & Ply File sent to server as ";
                meshNotSent = "Mesh & Ply File updated on server";
                meshRemoved = "Mesh removed from server";
                meshPlyRemoved = "Ply & Mesh removed from server";
                plyRemoved = "Ply removed from server";
                meshAndPlyNotFound = "No Ply & Mesh found on server";
                openedRSFile = "Opened RealSense file";
                couldNotOpen = "Could not open file";

                noSnap = "No snapshot taken yet";
                plySaved = "Ply file saved";

                nameQuestion = "Input client name:";
                NameChange = "Client name changed to: ";

                NoUserCode = "No code to execute";
                loadFileErr = "File couldn't be read";
                loadFileSucc = "Code loaded from file";
                PySaved = "Code saved to file";
                PySavedErr = "Code was not successfully saved";

                // 
                DecFilt = "Decimation filter parameters";
                LinSc = "Linear scale factor";

                DeptFilt = "Depth thresholding parameters";
                Min = "Minimum";
                Max = "Maximum";
                _disparityLBL = "Smoothig in disparity domain";

                SpatFilt = "Spatial smoothing parameters";
                It = "Number of iterations";
                AlphaSp = "Alpha";
                DeltaSp = "Delta";
                HoleSp = "Hole filling";

                TempFilt = "Temporal smoothing parameters";
                AlphaTemp = "Alpha";
                DeltaTemp = "Delta";
                Pers = "Persistency index";

                HoleFilt = "Hole filling parameters";
                Method = "Method";

                //
                DecimateLBLTooltip = "Decreases the size of the created mesh";
                ThresholdLBLTooltip = "Cuts off objects too close/far";
                HoleLBLTooltip = "Fills in holes in scanned data";
                SpatialLBLToolTip = "Smooting using neighboring pixels";
                SpHoleLBLToolTip = "Horizontal hole-filling mode";
                TemporalLBLToolTip = "Smoothing using past frames";
                TriangleThLBLToolTip = "Filtering out triangles with edges that are too long";
                FuncHeaderLBLToolTip = "Header of the user function, points - list with vertex coordinates, uvs - list with texture coordinates, faces - list with triangle indices";
                AutosendToolTip = "Click to turn on/off autosend";
                AutosendIntToolTip = "Resend interval in seconds";

                DecLinScaleLBLToolTip = "How much is the created mesh scaled down";
                DisparityLBLToolTip = "Smoothing in domain that represents the difference between projections of left and right cameras";
                SpAlphaLBLToolTip = "Strength of filter";
                SpDeltaLBLToolTip = "Establishes the threshold used to preserve edges";
                TempAlphaLBLToolTip = "Strength of filter";
                TempDeltaLBLToolTip = "Establishes the threshold used to preserve edges";
                TempPersLBLToolTip = "Used to decide when the missing pixel value should be corrected with previous data";
                PersIndex0ToolTip = "No hole filling occurs";
                PersIndex1ToolTip = "Corrected if the pixel was valid in 8 out of the last 8 frames";
                PersIndex2ToolTip = "Corrected if the pixel was valid in 2 out of the last 3 frames";
                PersIndex3ToolTip = "Corrected if the pixel was valid in 2 out of the last 4 frames";
                PersIndex4ToolTip = "Corrected if the pixel was valid in 2 out of the last 8 frames";
                PersIndex5ToolTip = "Corrected if the pixel was valid in 1 of the last 2 frames";
                PersIndex6ToolTip = "Corrected if the pixel was valid in 1 out of the last 5 frames";
                PersIndex7ToolTip = "Corrected if the pixel was valid in 1 out of the last 8 frames";
                PersIndex8ToolTip = "Always corrected";
                HoleMethod0ToolTip = "Use the value from the left neighbor pixel to fill the hole";
                HoleMethod1ToolTip = "Use the value from the neighboring pixel furthest away from the sensor";
                HoleMethod2ToolTip = "Use the value from the neighboring pixel closest to the sensor";
            }
        }
    }
}
