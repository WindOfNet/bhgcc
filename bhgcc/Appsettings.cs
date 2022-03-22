using System;
using System.Collections.Generic;
using System.Text;

namespace bhgcc
{
    public class Appsettings
    {
        public string LineNotifyToken { get; set; }
        public int Cycle { get; set; }
        public WorkerSetting[] Worker { get; set; }
    }
}
