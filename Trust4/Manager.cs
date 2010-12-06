//
//  Copyright 2010  Trust4 Developers
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using ARSoft.Tools.Net.Dns;
using System.Security.AccessControl;
using Daylight;

namespace Trust4
{
    public class Manager
    {
        private Settings p_Settings = null;
        private Mappings p_Mappings = null;

        private DnsServer m_DNSServer = null;
        private DnsProcess m_DNSProcess = null;
        private KademliaNode p_KademliaNode = null;
        private Dht p_KademliaDHT = null;

        private static readonly Guid m_P2PRootStore = new Guid("94e9bd40-2547-4232-9266-4f93310bf906");
        private static readonly Guid m_KeyRootStore = new Guid("09a2cbb4-ef12-431c-9419-5a655075039e");

        /// <summary>
        /// Creates a new Manager instance, which handles execution of the Trust4
        /// server.
        /// </summary>
        public Manager()
        {
            // Load the settings.
            this.p_Settings = new Settings("settings.txt");
            this.p_Settings.Load();
            
            // Initalize the DNS service.
            if (!this.InitalizeDNS())
            {
                // Couldn't lower permissions from root; exit immediately.
                return;
            }

            // Initalize the DHT service.
            this.InitalizeDHT();
            
            // Load the mappings.
            this.p_Mappings = new Mappings(this, "mappings.txt");
            this.p_Mappings.Load();
            
            // .. the Trust4 server is now running ..
            Thread.Sleep(Timeout.Infinite);
            
            // Stop the server
            this.m_DNSServer.Stop();
        }

        /// <summary>
        /// The settings for the Trust4 server.
        /// </summary>
        public Settings Settings
        {
            get { return this.p_Settings; }
        }

        /// <summary>
        /// The local and cached domain mappings.
        /// </summary>
        public Mappings Mappings
        {
            get { return this.p_Mappings; }
        }

        /// <summary>
        /// Returns the distributed routing table.
        /// </summary>
        public KademliaNode KademliaNode
        {
            get { return this.p_KademliaNode; }
        }

        /// <summary>
        /// The data store for the distributed routing table.
        /// </summary>
        public Dht KademliaDHT
        {
            get { return this.p_KademliaDHT; }
        }

        /// <summary>
        /// Initalizes the DNS server component.
        /// </summary>
        public bool InitalizeDNS()
        {
            // Create the DNS processing instance.
            this.m_DNSProcess = new DnsProcess(this);
            
            // Start the DNS server.
            this.m_DNSServer = new DnsServer(IPAddress.Any, this.p_Settings.DNSPort, 10, 10, this.m_DNSProcess.ProcessQuery);
            this.m_DNSServer.ExceptionThrown += new EventHandler<ExceptionEventArgs>(this.m_DNSProcess.ExceptionThrown);
            this.m_DNSServer.Start();
            
            int p = (int) Environment.OSVersion.Platform;
            if (( p == 4 ) || ( p == 6 ) || ( p == 128 ))
            {
                if (!this.UpdateUnixUIDGID(this.Settings.UnixUID, this.Settings.UnixGID))
                {
                    Console.WriteLine("Error!  I couldn't not lower the permissions of the current process.  I'm not going to continue for security reasons!");
                    return false;
                }
            }
            
            return true;
        }

        public bool UpdateUnixUIDGID(uint uid, uint gid)
        {
            if (Mono.Unix.Native.Syscall.getuid() != 0)
            {
                // We don't need to lower / change permissions since we aren't root.
                return true;
            }

            // Ensure that the environment variable XDG_CONFIG_HOME is set correctly.
            if (Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") != ".")
            {
                Console.WriteLine("Error!  You must set XDG_CONFIG_HOME to \".\" when running this application.  i.e. 'sudo XDG_CONFIG_HOME=. mono Trust4.exe'");
                return false;
            }

            int res = Mono.Unix.Native.Syscall.setregid(gid, gid);
            if (res != 0)
            {
                Console.WriteLine("Error!  Unable to lower effective and real group IDs to " + gid + ".  '" + Mono.Unix.Native.Stdlib.GetLastError().ToString() + "'");
                return false;
            }
            res = Mono.Unix.Native.Syscall.setreuid(uid, uid);
            if (res != 0)
            {
                Console.WriteLine("Error!  Unable to lower effective and real user IDs to " + uid + ".  '" + Mono.Unix.Native.Stdlib.GetLastError().ToString() + "'");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initalizes the Distributed Hash Table component.
        /// </summary>
        public void InitalizeDHT()
        {
            // Start the Distributed Hash Table.
            this.p_KademliaNode = new KademliaNode(new IPEndPoint(this.p_Settings.LocalIP, this.p_Settings.P2PPort));
            this.p_KademliaDHT = new Dht(this.p_Settings.NetworkID.ToString(), true);

            // Bootstrap connections using the peers.
            Console.WriteLine("Bootstrapping DHT\r\n { " + this.p_KademliaNode.GetID().ToString() + " }");
            this.BootstrapPeers();
            Console.WriteLine("Bootstrap finished");
        }

        /// <summary>
        /// Yeilds a list of peers as they are read from the peers.txt file.
        /// </summary>
        /// <returns>The next peer.</returns>
        public void BootstrapPeers()
        {
            foreach (var line in File.ReadAllLines("peers.txt").OmitComments("#", "//"))
            {
                try
                {
                    string[] split = line.Split(new char[] {
                        ' ',
                        '\t'
                    }, StringSplitOptions.RemoveEmptyEntries);

                    // Bootstrap by reading the peer and connecting to them.
                    decimal trust = Decimal.Parse(split[0]);
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(split[1]), int.Parse(split[2]));

                    if (this.p_KademliaNode.Bootstrap(endpoint))
                    {
                        Console.WriteLine("BOOTSTRAP SUCCESS: Got a bootstrapping response from " + endpoint.ToString() + ".");
                    }
                    else
                    {
                        Console.WriteLine("BOOTSTRAP FAILURE: No peer online at " + endpoint.ToString() + ".");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("BOOTSTRAP FAILURE: Exception parsing bootstrap file: " + e);
                }
            }
        }
    }
}
