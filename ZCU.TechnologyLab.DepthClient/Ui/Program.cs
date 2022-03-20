using System;
using Intel.RealSense;

namespace ZCU.TechnologyLab.DepthClient.Ui
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var w = new ProcessingWindow();
            w.ShowDialog();
        }
    }
}
