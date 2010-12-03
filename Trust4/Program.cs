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
    class Program
    {
        static List<DomainMap> m_Mappings = new List<DomainMap>();
        static List<string> m_WaitingOn = new List<string>();
        static int m_Port = 12000;

        static IPAddress localIp;
        static Guid networkId;
        static Identifier512 routingIdentifier;

        static void Main(string[] args)
        {
            int dnsport = 53;
            ReadSettings(ref dnsport);

            // Start the DNS server.
            DnsServer server = new DnsServer(IPAddress.Any, dnsport, 10, 10, Program.ProcessQuery);
            server.ExceptionThrown += new EventHandler<ExceptionEventArgs>(ExceptionThrown);
            server.Start();

            ReadMappings();

            DistributedRoutingTable routingTable = new DistributedRoutingTable(routingIdentifier, (a) => new UdpContact(a.LocalIdentifier, networkId, localIp, m_Port), networkId, new Configuration());

            UdpContact.InitialiseUdp(routingTable, m_Port);

            Console.WriteLine("Bootstrapping DHT");
            routingTable.Bootstrap(LoadBootstrapData());

            Console.WriteLine("Bootstrap finished");
            Console.WriteLine("There are " + routingTable.ContactCount + " Contacts");

            Console.WriteLine("Press any key to stop server.");
            Console.ReadLine();
        }

        private static void ReadMappings()
        {
            using (StreamReader reader = new StreamReader("mappings.txt"))
            {
                while (!reader.EndOfStream)
                {
                    string[] s = reader.ReadLine().Split('=');
                    string domain = s[0].Trim();
                    string ip = s[1].Trim();

                    Console.Write("Mapping " + domain + " to " + ip + "... ");
                    try
                    {
                        IPAddress o = IPAddress.None;
                        IPAddress.TryParse(ip, out o);
                        Program.m_Mappings.Add(new DomainMap(domain, o));
                        Console.WriteLine("done.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("failed.");
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        private static void ReadSettings(ref int dnsport)
        {
            foreach (var line in File.ReadAllLines("Settings.txt").OmitComments("#", "//").Select(a => a.ToLowerInvariant().Replace(" ", "").Replace("\t", "").Split('=')))
            {
                string setting = line[0].Trim();
                string value = line[1].Trim();

                switch (setting)
                {
                    case "peerport":
                        Program.m_Port = Convert.ToInt32(value);
                        break;
                    case "port":
                        Program.m_Port = Convert.ToInt32(value);
                        break;
                    case "dnsport":
                        dnsport = Convert.ToInt32(value);
                        break;
                    case "localip":
                        localIp = IPAddress.Parse(line[1]);
                        break;
                    case "networkid":
                        networkId = new Guid(line[1]);
                        break;
                    case "routingidentifier":
                        var s = line[1].Split(',');
                        routingIdentifier = new Identifier512(new Guid(s[0]), new Guid(s[1]), new Guid(s[2]), new Guid(s[3]));
                        break;
                    default:
                        Console.WriteLine("Unknown setting " + line[0]);
                        break;
                }
            }
        }

        private static IEnumerable<Contact> LoadBootstrapData()
        {
            foreach (var line in File.ReadAllLines("peers.txt").OmitComments("#", "//"))
            {
                UdpContact udpC = null;

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

                    udpC = new UdpContact(id, networkId, ip, port);

                    Console.WriteLine("Loaded bootstrap contact " + udpC);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception parsing bootstrap file: " + e);
                }

                if (udpC != null)
                    yield return udpC;
            }
        }

        static DnsMessage ProcessQuery(DnsMessageBase qquery, IPAddress clientAddress, ProtocolType protocolType)
        {
            DnsMessage query = qquery as DnsMessage;
            query.IsQuery = false;

            foreach (DnsQuestion q in query.Questions)
            {
                if (q.RecordType == RecordType.A)
                {
                    if (!q.Name.EndsWith(".p2p"))
                    {
                        query.ReturnCode = ReturnCode.Refused;
                        return query;
                    }

                    Console.WriteLine("DNS LOOKUP - User asked for " + q.Name);

                    // Search our own mappings.
                    bool found = false;
                    foreach (DomainMap d in Program.m_Mappings)
                    {
                        if (q.Name.Equals(d.Domain, StringComparison.InvariantCultureIgnoreCase) ||
                            q.Name.EndsWith("." + d.Domain, StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                            Console.WriteLine("DNS LOOKUP - Found in cache (" + d.Target.ToString() + ")");
                            query.ReturnCode = ReturnCode.NoError;
                            query.AnswerRecords.Add(new ARecord(q.Name, 3600, d.Target));
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Since we're about to query peers, add this domain to our
                        // "waiting on" list which means that any requests for this
                        // domain from other peers will result in not found.
                        Program.m_WaitingOn.Add(q.Name.ToLowerInvariant());

                        // We haven't found it in our local cache.  Query our
                        // peers to see if they've got any idea where this site is.
                        //foreach (Peer p in Program.m_Peers)
                        //{
                        //    if (!p.Connection.Connected)
                        //        continue;

                        //    DnsMessage m = p.Query(q);
                        //    if (m.ReturnCode == ReturnCode.NoError)
                        //    {
                        //        found = true;

                        //        // Cache the result.
                        //        ARecord a = (m.AnswerRecords[0] as ARecord);
                        //        Program.m_Mappings.Add(new DomainMap(a.Name, a.Address));

                        //        Console.WriteLine("DNS LOOKUP - Found via peer " + p.Address.ToString() + " (" + a.Address.ToString() + ")");

                        //        // Add the result.
                        //        query.ReturnCode = ReturnCode.NoError;
                        //        query.AnswerRecords.Add(new ARecord(a.Name, 3600, a.Address));

                        //        break;
                        //    }
                        //}
                        throw new NotImplementedException("Not found in local cache, need to go out to DHT");
                    }

                    if (!found)
                    {
                        query.ReturnCode = ReturnCode.ServerFailure;
                        Console.WriteLine("DNS LOOKUP - Not found");
                    }

                    return query;
                }
                else
                    query.ReturnCode = ReturnCode.NotImplemented;
            }

            return query;
        }

        static void ExceptionThrown(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.ToString());
        }
    }
}
