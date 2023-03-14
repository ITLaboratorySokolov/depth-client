using System.Collections.Generic;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Serialization.Mesh;
using ZCU.TechnologyLab.Common.Serialization.Properties;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    public class WorldObjectProcessor
    {

        public static WorldObjectDto CreatePlyWO(byte[] plyData, string name)
        {
            var p = new Dictionary<string, byte[]>
            {
                ["Data"] = plyData
            };
            var w = new WorldObjectDto
            {
                Name = name,
                Type = "PlyFile",
                Properties = p,
                Position = new RemoteVectorDto(),
                Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                Rotation = new RemoteVectorDto()
            };
            w.Position.X = 0;
            w.Position.Y = 0;
            w.Position.Z = 0;

            return w;
        } 

        public static WorldObjectDto CreateMeshWO(RealS.MeshFrame frame, string name)
        {
            byte[] texFormat = new StringSerializer("TextureFormat").Serialize("RGB");
            byte[] texSize = new ArraySerializer<int>("TextureSize", sizeof(int)).Serialize(new int[] { frame.Width, frame.Height });

            var properties = new RawMeshSerializer().Serialize(frame.Vertices, frame.TempFaces, "Triangle", frame.UVs, frame.Width, frame.Height, "RGB", frame.Colors);
            properties.Add("TextureFormat", texFormat);
            properties.Add("TextureSize", texSize);
            properties.Add("Texture", frame.Colors);

            var w = new WorldObjectDto
            {
                Name = name,
                Type = "Mesh",
                Position = new RemoteVectorDto(),
                Properties = properties,
                Scale = new RemoteVectorDto() { X = 1, Y = 1, Z = 1 },
                Rotation = new RemoteVectorDto()
            };
            w.Position.X = 0;
            w.Position.Y = 0;
            w.Position.Z = 0;
        
            return w;
        }

    }
}
