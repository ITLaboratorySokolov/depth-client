using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ZCU.TechnologyLab.Common.Entities.DataTransferObjects;
using ZCU.TechnologyLab.Common.Serialization.Mesh;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    /// <summary>
    /// Class providing methods for processing a mesh
    /// </summary>
    internal class MeshProcessor
    {
        /// <summary>
        /// Generate triangle faces
        /// </summary>
        /// <param name="frame"> Captured mesh frame </param>
        /// <param name="threshold"> Filtering threshold </param>
        public static int[] GenerateFaces(RealS.MeshFrame frame, float threshold)
        {
            threshold *= threshold;

            Array.Copy(frame.Faces, frame.TempFaces, frame.Faces.Length);

            List<int> newFacesList = new List<int>();
            Stopwatch w = new();
            for (int i = 0; i < frame.TempFaces.Length; i += 3)
            {
                Vector3 v0 = new(new ReadOnlySpan<float>(frame.Vertices, frame.TempFaces[i + 0] * 3, 3));
                Vector3 v1 = new(new ReadOnlySpan<float>(frame.Vertices, frame.TempFaces[i + 1] * 3, 3));
                Vector3 v2 = new(new ReadOnlySpan<float>(frame.Vertices, frame.TempFaces[i + 2] * 3, 3));

                if ((v0 - v1).LengthSquared() > threshold
                    || (v1 - v2).LengthSquared() > threshold
                    || (v2 - v0).LengthSquared() > threshold)
                {
                    frame.TempFaces[i + 0] = 0;
                    frame.TempFaces[i + 1] = 0;
                    frame.TempFaces[i + 2] = 0;
                }
                else 
                {
                    {
                        newFacesList.Add(frame.TempFaces[i + 0] + 1);
                        newFacesList.Add(frame.TempFaces[i + 1] + 1);
                        newFacesList.Add(frame.TempFaces[i + 2] + 1);
                    }
                }
            }

            // Console.WriteLine("generating faces took " + w.ElapsedMilliseconds);

            return newFacesList.ToArray();
        }

        /// <summary>
        /// Create default mesh - displayed after launch of program
        /// </summary>
        /// <returns> Mesh </returns>
        public static MeshGeometry3D CreateDefaultMesh(){
            // Create a mesh builder and add a box to it
            var meshBuilder = new MeshBuilder(false, false);
            meshBuilder.AddBox(new Point3D(0, 0, 1), 1, 2, 0.5);
            meshBuilder.AddBox(new Rect3D(0, 0, 1.2, 0.5, 1, 0.4));
            // Create a mesh from the builder (and freeze it)
            MeshGeometry3D mesh = meshBuilder.ToMesh(true);

            return mesh;
        }

        /// <summary>
        /// Create mesh from MeshFrame
        /// </summary>
        /// <param name="frame"> Captured meshframe </param>
        /// <returns> Mesh </returns>
        /// <exception cref="Exception"> Thrown if wrong frame format and a triangle mesh cannot be created (vertices need to be divisible by 3 and UVs divisible by two) </exception>
        internal static MeshGeometry3D CreateMeshFromFrame(RealS.MeshFrame frame)
        {
            MeshGeometry3D d = new MeshGeometry3D();

            d.Positions = new Point3DCollection(frame.Vertices.Length / 3);

            {
                // sanity check before unsafe
                if (frame.Vertices.Length / 3 != frame.UVs.Length / 2)
                    throw new Exception("Wrong array dimensions");

                if (frame.Vertices.Length == 0)
                    return d;

                d.TextureCoordinates = new PointCollection(frame.UVs.Length / 2);

                unsafe
                {
                    fixed (float* vertex = &frame.Vertices[0], uv = &frame.UVs[0])
                    {
                        float* vert = vertex;
                        float* tex = uv;

                        while (vert != vertex + frame.Vertices.Length)
                        {
                            Point3D p = new(
                                *vert++,
                                *vert++,
                                *vert++);

                            Point u = new Point(
                                *tex++,
                                *tex++);

                            d.Positions.Add(p);
                            d.TextureCoordinates.Add(u);
                        }
                    }
                }
            }

            d.TriangleIndices = new Int32Collection(frame.TempFaces);

            return d;
        }

        /// <summary>
        /// Create textured meshframe from world object
        /// </summary>
        /// <param name="wo"> World object with mesh data </param>
        /// <param name="tex"> World object with texture data </param>
        /// <returns> Mesh frame </returns>
        internal static RealS.MeshFrame CreateMeshFrameFromWO(WorldObjectDto wo, WorldObjectDto woP = null)
        {
            RealS.MeshFrame meshFr = new RealS.MeshFrame();
            RawMeshSerializer rms = new RawMeshSerializer();
            meshFr.Faces = rms.IndicesSerializer.Deserialize(wo.Properties);
            meshFr.TempFaces = new int[meshFr.Faces.Length];
            meshFr.Vertices = rms.VerticesSerializer.Deserialize(wo.Properties);
            meshFr.UVs = rms.UvSerializer.Deserialize(wo.Properties);
            meshFr.Width = rms.DiffuseTextureWidthSerializer.Deserialize(wo.Properties);
            meshFr.Colors = rms.DiffuseTexturePixelsSerializer.Deserialize(wo.Properties);

            if (woP != null)
                if (woP.Properties.ContainsKey("Data"))
                    meshFr.Ply = woP.Properties["Data"];

            // ArraySerializer<int> arrser = new ArraySerializer<int>("TextureSize", sizeof(int));
            // var wah = arrser.Deserialize(wo.Properties);
            //meshFr.Width = wah[0]; // new RawBitmapSerializer().WidthSerializer.Deserialize(tex.Properties);
            // meshFr.Colors = wo.Properties["Texture"]; // new RawBitmapSerializer().PixelsSerializer.Deserialize(tex.Properties);

            return meshFr;
        }
    }
}
