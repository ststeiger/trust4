using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARSoft.Tools.Net.Dns;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Trust4
{
    class Program
    {
        static List<Peer> m_Peers = new List<Peer>();
        static List<DomainMap> m_Mappings = new List<DomainMap>();
        static List<string> m_WaitingOn = new List<string>();
        static StatelessSocket m_PeerServer = null;
        static Thread m_PeerThread = null;
        static int m_Port = 12000;
        public static ManualResetEvent AllDone = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            // Read our settings.
            int dnsport = 53;
            using (StreamReader reader = new StreamReader("settings.txt"))
            {
                while (!reader.EndOfStream)
                {
                    string[] s = reader.ReadLine().Split('=');
                    string setting = s[0].Trim();
                    string value = s[1].Trim();

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
                    }
                }
            }

            // Start the DNS server.
            DnsServer server = new DnsServer(IPAddress.Any, dnsport, 10, 10, Program.ProcessQuery);
            server.ExceptionThrown += new EventHandler<ExceptionEventArgs>(ExceptionThrown);
            server.Start();

            // Read our mappings.
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

            // Now connect to peers.
            using (StreamReader reader = new StreamReader("peers.txt"))
            {
                while (!reader.EndOfStream)
                {
                    string[] s = reader.ReadLine().Split(':');
                    string ip = s[0].Trim();
                    int port = Convert.ToInt32(s[1].Trim());

                    Console.Write("Connecting to " + ip + ":" + port + "... ");
                    try
                    {
                        IPAddress o = IPAddress.None;
                        IPAddress.TryParse(ip, out o);
                        Peer p = new Peer(o, port);
                        Program.m_Peers.Add(p);
                        if (p.Connection.Connected)
                            Console.WriteLine("success!");
                        else
                            Console.WriteLine("not online.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("failed!");
                        Console.WriteLine(e.ToString());
                    }
                }
            }

            // Show information and start socket listener.
            Program.m_PeerThread = new Thread(Program.PeerListen);
            Program.m_PeerThread.IsBackground = true;
            Program.m_PeerThread.Start();

            Console.WriteLine("Press any key to stop server.");
            Console.ReadLine();
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
                        foreach (Peer p in Program.m_Peers)
                        {
                            if (!p.Connection.Connected)
                                continue;

                            DnsMessage m = p.Query(q);
                            if (m.ReturnCode == ReturnCode.NoError)
                            {
                                found = true;

                                // Cache the result.
                                ARecord a = (m.AnswerRecords[0] as ARecord);
                                Program.m_Mappings.Add(new DomainMap(a.Name, a.Address));

                                Console.WriteLine("DNS LOOKUP - Found via peer " + p.Address.ToString() + " (" + a.Address.ToString() + ")");

                                // Add the result.
                                query.ReturnCode = ReturnCode.NoError;
                                query.AnswerRecords.Add(new ARecord(a.Name, 3600, a.Address));

                                break;
                            }
                        }
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
        }

        static void PeerListen()
        {
            Program.m_PeerServer = new StatelessSocket();
            Program.m_PeerServer.OnConnected += new StatelessEventHandler(m_PeerServer_OnConnected);
            Program.m_PeerServer.OnReceived += new StatelessEventHandler(m_PeerServer_OnReceived);
            Program.m_PeerServer.Listen(new IPEndPoint(IPAddress.Any, Program.m_Port));
        }

        /// <summary>
        /// This event is raised when a message is received from one of the
        /// connected clients.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void m_PeerServer_OnReceived(object sender, StatelessEventArgs e)
        {
            string[] request = e.Data.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(':');
            switch (request[0].ToUpperInvariant())
            {
                case "LOOKUP":
                    // Search our own mappings.
                    bool found = false;
                    foreach (DomainMap d in Program.m_Mappings)
                    {
                        if (request[1].Equals(d.Domain, StringComparison.InvariantCultureIgnoreCase) ||
                            request[1].EndsWith("." + d.Domain, StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                            e.Client.Send("RESULT:FOUND:" + d.Target.ToString());
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Check to make sure we aren't already waiting on this domain.
                        if (Program.m_WaitingOn.Contains(request[1].ToLowerInvariant()))
                        {
                            e.Client.Send("RESULT:NOTFOUND");
                            break;
                        }

                        // We haven't found it in our local cache.  Query our
                        // peers to see if they've got any idea where this site is.
                        foreach (Peer p in Program.m_Peers)
                        {
                            if (!p.Connection.Connected)
                                continue;

                            DnsMessage m = p.Query(request[1]);
                            if (m.ReturnCode == ReturnCode.NoError)
                            {
                                found = true;

                                // Cache the result.
                                ARecord a = (m.AnswerRecords[0] as ARecord);
                                Program.m_Mappings.Add(new DomainMap(a.Name, a.Address));

                                // Return the result.
                                e.Client.Send("RESULT:FOUND:" + a.Address.ToString());
                                break;
                            }
                        }
                    }

                    if (!found)
                        e.Client.Send("RESULT:NOTFOUND");
                    break;
                default:
                    Console.WriteLine("Got Unknown - " + request[0] + ":" + request[1]);
                    e.Client.Send("RESULT:UNKNOWN");
                    break;
            }
        }

        static void m_PeerServer_OnConnected(object sender, StatelessEventArgs e)
        {
            Console.WriteLine(e.Client.ClientEndPoint + " - PEER LISTEN");
        }
    }
}
