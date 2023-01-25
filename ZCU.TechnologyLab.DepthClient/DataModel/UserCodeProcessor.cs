using System.Collections.Generic;
using PythonExecutionLibrary;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    internal class UserCodeProcessor
    {
        /// <summary> Message of the last error </summary>
        public string ERROR_MSG;
        private PythonExecutor pythonExecutor;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pythonPath"> Path to python dll </param>
        public UserCodeProcessor(string pythonPath)
        {
            pythonExecutor = new PythonExecutor();
            pythonExecutor.SetPython(pythonPath);
        }

        /// <summary>
        /// Execute python user code
        /// </summary>
        /// <param name="code"> Python function code </param>
        /// <param name="varValues"> Variables and their values </param>
        /// <returns> Edited point cloud </returns>
        public PointCloud ExecuteUserCode(string code, Dictionary<string, object> varValues)
        {
            PointCloud cloud = new PointCloud();
            List<string> paramNames = new List<string>(varValues.Keys);

            string pycode = pythonExecutor.CreateCode("userFunc", paramNames, varValues, code);
            bool res = pythonExecutor.RunCode(pycode, varValues, cloud);

            // if code execution failed
            if (!res)
            {
                ERROR_MSG = pythonExecutor.ERROR_MSG;
                return null;
            }

            return cloud;
        }

    }
}
