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
        static Socket m_PeerServer = null;
        static Thread m_PeerThread = null;
        static int m_Port = 12000;
        public static ManualResetEvent AllDone = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            // Start the DNS server.
            DnsServer server = new DnsServer(IPAddress.Loopback, 10, 10, Program.ProcessQuery);
            server.ExceptionThrown += new EventHandler<ExceptionEventArgs>(ExceptionThrown);
            server.Start();

            // Read our settings.
            using (StreamReader reader = new StreamReader("settings.txt"))
            {
                while (!reader.EndOfStream)
                {
                    string[] s = reader.ReadLine().Split('=');
                    string setting = s[0].Trim();
                    string value = s[1].Trim();

                    switch (setting)
                    {
                        case "port":
                            Program.m_Port = Convert.ToInt32(value);
                            break;
                    }
                }
            }

            // Show information and start socket listener.
            Program.m_PeerThread = new Thread(Program.PeerListen);
            Program.m_PeerThread.IsBackground = true;
            Program.m_PeerThread.Start();

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
                        Console.WriteLine("success!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("failed!");
                        Console.WriteLine(e.ToString());
                    }
                }
            }

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
                        query.ReturnCode = ReturnCode.NotAuthoritive;
                        return query;
                    }

                    Console.WriteLine("DNS LOOKUP - User asked for " + q.Name);

                    // Search our own mappings.
                    bool found = false;
                    foreach (DomainMap d in Program.m_Mappings)
                    {
                        if (q.Name.Equals(d.Domain, StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                            Console.WriteLine("DNS LOOKUP - Found in cache (" + d.Target.ToString() + ")");
                            query.ReturnCode = ReturnCode.NoError;
                            query.AnswerRecords.Add(new ARecord(d.Domain, 3600, d.Target));
                            break;
                        }
                    }

                    if (!found)
                    {
                        // We haven't found it in our local cache.  Query our
                        // peers to see if they've got any idea where this site is.
                        foreach (Peer p in Program.m_Peers)
                        {
                            DnsMessage m = p.Query(q);
                            if (m.ReturnCode == ReturnCode.NoError)
                            {
                                found = true;

                                // Cache the result.
                                ARecord a = (m.AnswerRecords[0] as ARecord);
                                Program.m_Mappings.Add(new DomainMap(a.Name, a.Address));

                                Console.WriteLine("DNS LOOKUP - Found via peer " + p.IPAddress.ToString() + " (" + a.Address.ToString() + ")");

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
            Console.WriteLine(e.Exception.ToString());
        }

        public class StateObject
        {
            // Client  socket.
            public Socket workSocket = null;
            // Size of receive buffer.
            public const int BufferSize = 1024;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.
            public StringBuilder sb = new StringBuilder();
        }

        static void PeerListen()
        {
            Program.m_PeerServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Program.m_PeerServer.Bind(new IPEndPoint(IPAddress.Any, Program.m_Port));
                Program.m_PeerServer.Listen(20);
                Console.WriteLine("Listening on port " + Program.m_Port.ToString());

                while (true)
                {
                    // Set the event to nonsignaled state.
                    Program.AllDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    Program.m_PeerServer.BeginAccept(
                        new AsyncCallback(PeerAccept),
                        Program.m_PeerServer);

                    // Wait until a connection is made before continuing.
                    Program.AllDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Peering server has fallen over!");
                Console.WriteLine(e.ToString());
            }
        }

        static void PeerAccept(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            Program.AllDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            Console.WriteLine(handler.RemoteEndPoint.ToString() + " - START");

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(PeerRead), state);
        }

        static void PeerRead(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read.  Handle it.
                    string[] request = content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(':');
                    Console.WriteLine(handler.RemoteEndPoint.ToString() + " - " + content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                    switch (request[0].ToUpperInvariant())
                    {
                        case "LOOKUP":
                            // Search our own mappings.
                            bool found = false;
                            foreach (DomainMap d in Program.m_Mappings)
                            {
                                if (request[1].Equals(d.Domain, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    found = true;
                                    Program.PeerSend(handler, "RESULT:FOUND:" + d.Target.ToString() + "<EOF>");
                                    break;
                                }
                            }

                            if (!found)
                            {
                                // We haven't found it in our local cache.  Query our
                                // peers to see if they've got any idea where this site is.
                                foreach (Peer p in Program.m_Peers)
                                {
                                    DnsMessage m = p.Query(request[1]);
                                    if (m.ReturnCode == ReturnCode.NoError)
                                    {
                                        found = true;

                                        // Cache the result.
                                        ARecord a = (m.AnswerRecords[0] as ARecord);
                                        Program.m_Mappings.Add(new DomainMap(a.Name, a.Address));

                                        // Return the result.
                                        Program.PeerSend(handler, "RESULT:FOUND:" + a.Address.ToString() + "<EOF>");
                                        break;
                                    }
                                }
                            }

                            if (!found)
                                Program.PeerSend(handler, "RESULT:NOTFOUND" + "<EOF>");
                            break;
                        default:
                            Console.WriteLine("Got Unknown - " + request[0] + ":" + request[1]);
                            Program.PeerSend(handler, "RESULT:UNKNOWN" + "<EOF>");
                            break;
                    }
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(PeerRead), state);
                }
            }
        }
    
        static void PeerSend(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            Console.WriteLine(handler.LocalEndPoint.ToString() + " - " + data.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0]);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(PeerSendCallback), handler);
        }

        static void PeerSendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
