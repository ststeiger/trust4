using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ARSoft.Tools.Net.Dns;
using System.Timers;

namespace Trust4
{
    class Peer
    {
        private IPAddress p_Address = IPAddress.None;
        private int p_Port = 12000;
        private StatelessSocket p_Connection = null;

        public Peer(IPAddress ip, int port)
        {
            this.p_Address = ip;
            this.p_Port = port;
            this.p_Connection = new StatelessSocket(ip, port);
        }

        public DnsMessage Query(DnsQuestion q)
        {
            return this.Query(q.Name);
        }

        public IPAddress Address
        {
            get
            {
                return this.p_Address;
            }
        }

        public int Port
        {
            get
            {
                return this.p_Port;
            }
        }

        public StatelessSocket Connection
        {
            get
            {
                return this.p_Connection;
            }
        }

        internal DnsMessage Query(string domain)
        {
            if (!this.p_Connection.Connected)
            {
                DnsMessage m = new DnsMessage();
                m.ReturnCode = ReturnCode.ServerFailure;
                return m;
            }

            // Send the lookup request.
            this.p_Connection.Send("LOOKUP:" + domain);

            // Get and handle the response.
            string[] result = this.p_Connection.Receive().Split(':');
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
    }
}
