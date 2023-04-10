using System;
using System.Globalization;
using Python.Runtime;
using ZCU.PythonExecutionLibrary;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    /// <summary>
    /// Class representing a mesh
    /// </summary>
    internal class PointMesh : IReturnable
    {
        public float[] points;
        public float[] uvs;
        public int[] faces;

        CultureInfo baseCulture = new CultureInfo("en-US");

        /// <summary>
        /// Set parameters of point mesh from PyObject list
        /// </summary>
        /// <param name="lst"> Py object </param>
        /// <returns> True if successful false if not </returns>
        public bool SetParameters(PyObject lst)
        {
            try
            {
                if (lst[0].Length() % 3 != 0 || lst[1].Length() % 2 != 0 || lst[2].Length() % 3 != 0) 
                    throw new Exception();
                if (lst[1].Length() / 2 != lst[0].Length() / 3)
                    throw new Exception();

                long vertCount = (lst[0].Length() / 3);

                points = new float[lst[0].Length()];
                uvs = new float[lst[1].Length()];
                faces = new int[lst[2].Length()];


                // points
                for (int i = 0; i < lst[0].Length(); i += 3)
                {
                    float x = (float)lst[0][i + 0].ToDouble(baseCulture);
                    float y = (float)lst[0][i + 1].ToDouble(baseCulture);
                    float z = (float)lst[0][i + 2].ToDouble(baseCulture);

                    points[i + 0] = x;
                    points[i + 1] = y;
                    points[i + 2] = z;
                }

                // uvs
                for (int i = 0; i < lst[1].Length(); i += 2)
                {
                    float u = (float)lst[1][i + 0].ToDouble(baseCulture);
                    float v = (float)lst[1][i + 1].ToDouble(baseCulture);

                    uvs[i + 0] = u;
                    uvs[i + 1] = v;
                }

                // faces
                for (int i = 0; i < lst[2].Length(); i += 3)
                {
                    int t1 = lst[2][i + 0].ToInt32(baseCulture);
                    int t2 = lst[2][i + 1].ToInt32(baseCulture);
                    int t3 = lst[2][i + 2].ToInt32(baseCulture);

                    if (t1 < 0 || t2 < 0 || t3 < 0 || t1 >= vertCount || t2 >= vertCount || t3 >= vertCount)
                        throw new Exception("Invalid vertex index");

                    faces[i + 0] = t1;
                    faces[i + 1] = t2;
                    faces[i + 2] = t3;
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
