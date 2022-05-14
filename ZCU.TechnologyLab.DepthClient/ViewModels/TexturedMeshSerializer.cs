using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZCU.TechnologyLab.Common.Serialization;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    namespace ZCU.TechnologyLab.Common.Serialization
    {
        /// <summary>
        /// The class serializes and deserializes meshes that are sent to server or received from a server.
        /// 
        /// This class ensures that all mesh properties are serialized and deserialized according to API specification.
        /// If you want to send a mesh to a server, you need three things:
        ///     1) your own class that holds and manages data of a mesh (in Unity it could be Mesh class),
        ///     2) you take data from the class in 1) and serialize them via methods in this serializer,
        ///     3) you take serialized data and create WorldObjectDto object then you pass it to ServerConnection which will send it to a server.
        ///     
        /// If you want to receive a mesh from a server, you again need a class that holds and manages data, but the order of steps is reversed.
        /// ServerConnection will give you dto object you take its properties deserialize them in this class and set data to your class.
        /// </summary>
        public class TexturedMeshSerializer : MeshSerializer
        {


            /// <summary>
            /// Name of primitive key in dictionary.
            /// </summary>
            public const string UVKey = "UV";

            public const string UVTextureNameKey = "Texture";

            /// <summary>
            /// Serializes mesh properties to a dictionary.
            /// </summary>
            /// <param name="vertices">Vertices of a mesh.</param>
            /// <param name="indices">Indices of a mesh.</param>
            /// <param name="primitive">Primitive type.</param>
            /// <param name="textureName">name of texture object</param>
            /// <returns>The dictionary.</returns>
            public Dictionary<string, byte[]> SerializeProperties(float[] vertices,float[] uvs, int[] indices, string primitive,string textureName)
            {
                var o = SerializeProperties(vertices, indices, primitive);
                o.Add(UVKey, this.SerializeFloats(uvs));
                o.Add(UVTextureNameKey, Encoding.ASCII.GetBytes(textureName));
                return o;
            }

            /// <summary>
            /// Serializes uvs.
            /// </summary>
            /// <param name="uvs">The uvs.</param>
            /// <returns>Serialized uvs.</returns>
            public byte[] SerializeFloats(float[] uvs)
            {
                var byteArray = new byte[uvs.Length * sizeof(float)];
                Buffer.BlockCopy(uvs, 0, byteArray, 0, byteArray.Length);
                return byteArray;
            }

           
            /// <summary>
            /// Deserializes vertices from properties.
            /// If vertices are not saved in properties throw an exception.
            /// </summary>
            /// <param name="properties">Properties that contain vertices.</param>
            /// <exception cref="ArgumentException">Thrown when properties do not contain vertices.</exception>
            public float[] DeserializeUVs(Dictionary<string, byte[]> properties)
            {
                return base.DeserializeProperty(
                    UVKey,
                    properties,
                    this.DeserializeFloats);
            }

            /// <summary>
            /// Deserializes uvs from a byte array.
            /// </summary>
            /// <param name="property">Byte array ovs property.</param>
            /// <returns>Array of uvs.</returns>
            public float[] DeserializeFloats(byte[] property)
            {
                var floatArray = new float[property.Length / sizeof(float)];
                Buffer.BlockCopy(property, 0, floatArray, 0, property.Length);
                return floatArray;
            }
        }
    }

}
