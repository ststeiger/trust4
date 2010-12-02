using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ARSoft.Tools.Net.Dns;

namespace Trust4
{
    class Peer
    {
        private IPAddress p_IPAddress = IPAddress.None;
        private int m_Port = 12000;
        private Socket m_Connection = null;

        public Peer(IPAddress ip, int port)
        {
            this.p_IPAddress = ip;
            this.m_Port = port;
            this.m_Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.m_Connection.Connect(this.p_IPAddress, this.m_Port);
        }

        public DnsMessage Query(DnsQuestion q)
        {
            return this.Query(q.Name);
        }

        public IPAddress IPAddress
        {
            get
            {
                return this.p_IPAddress;
            }
        }

        internal DnsMessage Query(string domain)
        {
            this.m_Connection.Send(Encoding.ASCII.GetBytes("LOOKUP:" + domain + "<EOF>"));

            byte[] storage = new byte[256];
            string content = string.Empty;
            int read = this.m_Connection.Receive(storage, 256, SocketFlags.None);

            if (read > 0)
            {
                content = Encoding.ASCII.GetString(storage);
                while (content.IndexOf("<EOF>") == -1)
                {
                    storage = new byte[256];
                    read += this.m_Connection.Receive(storage, 256, SocketFlags.None);
                    content += Encoding.ASCII.GetString(storage);
                }

                string[] result = content.Split(new string[] { "<EOF>" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(':');
                if (result[1].ToUpperInvariant() == "FOUND")
                {
                    DnsMessage m = new DnsMessage();
                    IPAddress o = IPAddress.None;
                    IPAddress.TryParse(result[2], out o);
                    m.ReturnCode = ReturnCode.NoError;
                    m.AnswerRecords.Add(new ARecord(domain, 3600, o));
                    return m;
                }
                else
                {
                    DnsMessage m = new DnsMessage();
                    m.ReturnCode = ReturnCode.NotAuthoritive;
                    return m;
                }
            }
            else
            {
                DnsMessage m = new DnsMessage();
                m.ReturnCode = ReturnCode.ServerFailure;
                return m;
            }
        }
    }
}
