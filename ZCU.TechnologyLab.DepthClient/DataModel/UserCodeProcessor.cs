using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZCU.PythonExecutionLibrary;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    /// <summary>
    /// Class for processing user code
    /// </summary>
    internal class UserCodeProcessor
    {
        /// <summary> Message of the last error </summary>
        public string ERROR_MSG;
        private PythonExecutor pythonExecutor;
        private string pythonPath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pythonPath"> Path to python dll </param>
        public UserCodeProcessor(string pythonPath)
        {
            pythonExecutor = new PythonExecutor();
            this.pythonPath = pythonPath;
        }

        /// <summary>
        /// Initialize python
        /// </summary>
        public void InitializePython()
        {
            pythonExecutor.SetPython(pythonPath);
            pythonExecutor.Initialize();
        }

        /// <summary>
        /// Get status of python engine initialization
        /// Once initialized, python dll cannot be set again
        /// </summary>
        /// <returns></returns>
        public bool GetStatusOfInit()
        {
            return pythonExecutor.initializedOnce;
        }

        /// <summary>
        /// Execute python user code
        /// </summary>
        /// <param name="code"> Python function code </param>
        /// <param name="varValues"> Variables and their values </param>
        /// <returns> Edited point cloud </returns>
        public async Task<PointMesh> ExecuteUserCode(string code, Dictionary<string, object> varValues, ConsoleData consData)
        {

            // redirect prints to console
            var stdoutWriter = new ConsoleWriter(consData, false);
            var stderrWriter = new ConsoleWriter(consData, true);

            PointMesh pc = await Task<PointMesh>.Run(() =>
            {
                PointMesh cloud = new PointMesh();
                List<string> paramNames = new List<string>(varValues.Keys);

                string pycode = pythonExecutor.CreateCode("userFunc", paramNames, varValues.Keys.ToList<string>(), code);

                try
                {
                    InitializePython();
                    pythonExecutor.RunCode(pycode, varValues, cloud, stdoutWriter, stderrWriter); // bool res
                }
                catch (Python.Runtime.PythonException e)
                {
                    ERROR_MSG = e.Message; // pythonExecutor.ERROR_MSG;
                    return null;
                }
                catch (Exception e)
                {
                    // python runtime incorrectly set
                    if (e.Message.Contains("Runtime.PythonDLL was not set or does not point to a supported Python runtime DLL"))
                        ERROR_MSG = e.Message + "\nRestart of application might be needed.";
                    else
                        ERROR_MSG = e.Message;

                    return null;
                }

                return cloud;
            });

            return pc;
        }

    }
}
