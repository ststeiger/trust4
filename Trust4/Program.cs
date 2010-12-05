using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARSoft.Tools.Net.Dns;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider;

namespace Trust4
{
    public class Program
    {
        private static Manager p_Manager = null;

        public static void Main(string[] args)
        {
            Program.p_Manager = new Manager();
        }

        public static Manager Manager
        {
            get
            {
                return Program.p_Manager;
            }
        }
    }
}
