using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSetter.Events
{
    public class NetworkErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }

        public NetworkErrorEventArgs(string message)
        {
            ErrorMessage = message;
        }
    }
}
