using YKEnroll.Lib;
using YKEnroll.Win.ViewModels.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKEnroll.Win.ViewModels
{
    internal class LogHistoryViewModel : Presenter
    {        
        public LogHistoryViewModel()
        {
            LogHistory = Logger.History;
            LogHistoryString = string.Join(System.Environment.NewLine, Logger.History);
        }

        public List<string> LogHistory { get; private set; }
        
        public string LogHistoryString { get; private set; }

    }
}
