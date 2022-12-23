using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    internal class Constants
    {
        // property names
        public const string POINTS_PROPERTY = "Points";
        public const string MODELTF_PROPERTY = "ModelTF";
        public const string MESSAGE_PROPERTY = "Message";
        public const string CNTCBTLB_PROPERTY = "ConnectBtnLbl";
        public const string EN_BTN_PROPERTY = "EnabledButtons";
        public const string AUTO_PROPERTY = "AutoEnabledLbl";
        public const string SAVE_PLY_BTN_PROPERTY = "SavePlyBtnEnable";
        public const string MODEL_PROPERTY = "Model";
        public const string FRAME_PROPERTY = "Frame";
        public const string USER_CODE = "UserCode";

        // labels
        public string _connectBtnLbl = "Connect";
        public string _message;

        // object names
        public const string MESH_NAME = "DepthMesh";
        public const string MESH_TEXTURE_NAME = "DepthMeshTexture";
        public const string PLY_NAME = "DepthPly";

    }
}
