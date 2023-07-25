using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using ZCU.TechnologyLab.DepthClient.DataModel;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public class ConsoleViewModel : NotifyingClass
    {
        ConsoleData _consoleData;
        public ConsoleData ConsoleData { get => _consoleData; set { RaisePropertyChanged("ConsoleData"); _consoleData = value; } }

        public ConsoleViewModel() {
        }

    }
}
