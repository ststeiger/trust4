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
using DistributedServiceProvider;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using Trust4.DataStorage;
using Trust4.Authentication;
using System.Security.AccessControl;

namespace Trust4
{
    public class Manager
    {
        private Settings p_Settings = null;
        private Mappings p_Mappings = null;

        private DnsServer m_DNSServer = null;
        private DnsProcess m_DNSProcess = null;
        private DistributedRoutingTable p_RoutingTable = null;

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
                return;
            // Couldn't lower permissions from root; exit immediately.
            // Initalize the DHT service.
            this.InitalizeDHT();
            
            // Load the mappings.
            this.p_Mappings = new Mappings(this, "mappings.txt");
            this.p_Mappings.Load();
            
            // .. the Trust4 server is now running ..
            Thread.Sleep(Timeout.Infinite);
            //Console.WriteLine("Press any key to stop server.");
            //Console.ReadLine();
            
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
        public DistributedRoutingTable RoutingTable
        {
            get { return this.p_RoutingTable; }
        }

        /// <summary>
        /// The data store for the distributed routing table.
        /// </summary>
        public IDataStore DataStore
        {
            get { return this.p_RoutingTable.GetConsumer<MultiRecordStore>(Manager.m_P2PRootStore, (  ) => new MultiRecordStore(Manager.m_P2PRootStore)); }
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
            this.p_RoutingTable = new DistributedRoutingTable(this.p_Settings.RoutingIdentifier, a => new UdpContact(a.LocalIdentifier, this.p_Settings.NetworkID, this.p_Settings.LocalIP, this.p_Settings.P2PPort), this.p_Settings.NetworkID, new Configuration {
                BucketRefreshPeriod = TimeSpan.FromMinutes(10),
                BucketSize = 10,
                LookupConcurrency = 5,
                LookupTimeout = 5000,
                PingTimeout = TimeSpan.FromSeconds(1),
                UpdateRoutingTable = true
            });
            
            UdpContact.InitialiseUdp(this.p_RoutingTable, this.p_Settings.P2PPort);
            
            AttachDhtServices();

            Console.WriteLine("Bootstrapping DHT\r\n { " + this.p_RoutingTable.LocalIdentifier + " }");
            this.p_RoutingTable.Bootstrap(this.BootstrapPeers());
            
            Console.WriteLine("Bootstrap finished");
            Console.WriteLine("There are " + this.p_RoutingTable.ContactCount + " Contacts");
        }

        private void AttachDhtServices()
        {
            p_RoutingTable.RegisterConsumer(new MultiRecordStore(Manager.m_P2PRootStore));
        }

        /// <summary>
        /// Yeilds a list of peers as they are read from the peers.txt file.
        /// </summary>
        /// <returns>The next peer.</returns>
        public IEnumerable<Contact> BootstrapPeers()
        {
            foreach (var line in File.ReadAllLines("peers.txt").OmitComments("#", "//"))
            {
                TrustedContact udp = null;
                
                try
                {
                    string[] split = line.Split(new char[] {
                        ' ',
                        '\t'
                    }, StringSplitOptions.RemoveEmptyEntries);
                    
                    decimal trust = Decimal.Parse(split[0]);
                    
                    IPAddress ip = IPAddress.Parse(split[1]);
                    int port = Int32.Parse(split[2]);
                    
                    if (split.Length == 7)
                    {
                        Guid a = new Guid(split[3]);
                        Guid b = new Guid(split[4]);
                        Guid c = new Guid(split[5]);
                        Guid d = new Guid(split[6]);
                        
                        Identifier512 id = new Identifier512(a, b, c, d);
                        udp = new TrustedContact(trust, id, this.p_Settings.NetworkID, ip, port);
                    }

                    else
                    {
                        try
                        {
                            Identifier512 discoveredId = UdpContact.DiscoverIdentifier(ip, port, TimeSpan.FromSeconds(5));
                            
                            Console.WriteLine("Discovered ID " + discoveredId + " for " + ip);
                            
                            udp = new TrustedContact(trust, discoveredId, this.p_Settings.NetworkID, ip, port);
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Timeout trying to discover an id for " + ip + ":" + port);
                        }
                    }
                    
                    Console.WriteLine("Loaded bootstrap contact " + udp);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception parsing bootstrap file: " + e);
                }
                
                if (udp != null)
                    yield return udp;
            }
        }
    }
}
