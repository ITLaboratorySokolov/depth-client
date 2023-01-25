using System;
using System.Globalization;
using Python.Runtime;
using PythonExecutionLibrary;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    internal class PointCloud : IReturnable
    {
        public float[] points;
        public float[] uvs;
        CultureInfo baseCulture = new CultureInfo("en-US");

        public bool SetParameters(PyObject lst)
        {
            try
            {
                if (lst[0].Length() % 3 != 0 || lst[1].Length() % 2 != 0)
                    throw new Exception();
                if (lst[1].Length() / 2 != lst[0].Length() / 3)
                    throw new Exception();

                points = new float[lst[0].Length()];
                uvs = new float[lst[1].Length()];

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
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
