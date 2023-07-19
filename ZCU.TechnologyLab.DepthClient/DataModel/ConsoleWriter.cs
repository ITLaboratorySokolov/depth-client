using System.IO;
using System.Text;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    public class ConsoleWriter : TextWriter
    {
        /// <summary> Console controller class </summary>
        private readonly ConsoleData consData;
        /// <summary> Is this an error writer </summary>
        private readonly bool _err;

        /// <summary> Encoding </summary>
        public override Encoding Encoding => Encoding.UTF8;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="consData"> Console controller class </param>
        /// <param name="err"> Is it an error writer </param>
        public ConsoleWriter(ConsoleData consData, bool err)
        {
            this.consData = consData;
            //this.txtBx = txtBx;
            _err = err;
        }

        /// <summary>
        /// Write line to console
        /// </summary>
        /// <param name="value"> Line to write </param>
        public override void WriteLine(string value)
        {
            consData.AddToConsole(value);
            // TODO if _err then it is an error writer
        }

        /// <summary>
        /// Write text to console
        /// </summary>
        /// <param name="value"> Text to write </param>
        public override void Write(string value)
        {
            // TODO if _err then it is an error writer
            consData.AddToConsole(value);
        }
    }
}
