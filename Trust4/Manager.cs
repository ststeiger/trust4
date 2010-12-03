using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARSoft.Tools.Net.Dns;
using DistributedServiceProvider;
using System.Net;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using System.IO;

namespace Trust4
{
    public class Manager
    {
        private Settings p_Settings = new Settings("settings.txt");
        private Mappings p_Mappings = new Mappings("mappings.txt");

        private DnsServer m_DNSServer = null;
        private DnsProcess m_DNSProcess = null;
        private DistributedRoutingTable m_RoutingTable = null;

        /// <summary>
        /// Creates a new Manager instance, which handles execution of the Trust4
        /// server.
        /// </summary>
        public Manager()
        {
            // Load the settings.
            this.p_Settings.Load();

            // Load the mappings.
            this.p_Mappings.Load();

            // Initalize the DNS service.
            this.InitalizeDNS();

            // Initalize the DHT service.
            this.InitalizeDHT();
        }

        /// <summary>
        /// The settings for the Trust4 server.
        /// </summary>
        public Settings Settings
        {
            get
            {
                return this.p_Settings;
            }
        }

        /// <summary>
        /// The local and cached domain mappings.
        /// </summary>
        public Mappings Mappings
        {
            get
            {
                return this.p_Mappings;
            }
        }

        /// <summary>
        /// Initalizes the DNS server component.
        /// </summary>
        public void InitalizeDNS()
        {
            // Create the DNS processing instance.
            this.m_DNSProcess = new DnsProcess(this);

            // Start the DNS server.
            this.m_DNSServer = new DnsServer(IPAddress.Any, this.p_Settings.DNSPort, 10, 10, this.m_DNSProcess.ProcessQuery);
            this.m_DNSServer.ExceptionThrown += new EventHandler<ExceptionEventArgs>(this.m_DNSProcess.ExceptionThrown);
            this.m_DNSServer.Start();
        }

        /// <summary>
        /// Initalizes the Distributed Hash Table component.
        /// </summary>
        public void InitalizeDHT()
        {
            // Start the Distributed Hash Table.
            this.m_RoutingTable = new DistributedRoutingTable(
                this.p_Settings.RoutingIdentifier,
                (a) => new UdpContact(a.LocalIdentifier, this.p_Settings.NetworkID, this.p_Settings.LocalIP, this.p_Settings.P2PPort),
                this.p_Settings.NetworkID,
                new Configuration()
                    {
                        BucketRefreshPeriod = TimeSpan.FromMinutes(10),
                        BucketSize = 10,
                        LookupConcurrency = 5,
                        LookupTimeout = 150,
                        PingTimeout = TimeSpan.FromSeconds(1),
                        UpdateRoutingTable = true,
                    }
                );

            UdpContact.InitialiseUdp(this.m_RoutingTable, this.p_Settings.P2PPort);

            Console.WriteLine("Bootstrapping DHT");
            this.m_RoutingTable.Bootstrap(this.BootstrapPeers());

            Console.WriteLine("Bootstrap finished");
            Console.WriteLine("There are " + this.m_RoutingTable.ContactCount + " Contacts");

            Console.WriteLine("Press any key to stop server.");
            Console.ReadLine();

            UdpContact.Stop();
        }

        /// <summary>
        /// Yeilds a list of peers as they are read from the peers.txt file.
        /// </summary>
        /// <returns>The next peer.</returns>
        public IEnumerable<Contact> BootstrapPeers()
        {
            foreach (var line in File.ReadAllLines("peers.txt").OmitComments("#", "//"))
            {
                UdpContact udp = null;

                try
                {
                    string[] split = line.Split(' ');
                    Guid a = new Guid(split[0]);
                    Guid b = new Guid(split[1]);
                    Guid c = new Guid(split[2]);
                    Guid d = new Guid(split[3]);

                    Identifier512 id = new Identifier512(a, b, c, d);

                    IPAddress ip = IPAddress.Parse(split[4]);
                    int port = Int32.Parse(split[5]);

                    udp = new UdpContact(id, this.p_Settings.NetworkID, ip, port);

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
