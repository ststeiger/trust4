using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARSoft.Tools.Net.Dns;
using System.Net;
using System.Net.Sockets;

namespace Trust4
{
    public class DnsProcess
    {
        private Manager m_Manager = null;

        /// <summary>
        /// Creates a new DNSProcess instance, which processes DNS messages as they arrive.
        /// </summary>
        /// <param name="manager"></param>
        public DnsProcess(Manager manager)
        {
            this.m_Manager = manager;
        }

        /// <summary>
        /// This function is called by the DnsServer instance when a new DNS query arrives.
        /// </summary>
        /// <param name="qquery"></param>
        /// <param name="clientAddress"></param>
        /// <param name="protocolType"></param>
        /// <returns></returns>
        public DnsMessage ProcessQuery(DnsMessageBase qquery, IPAddress clientAddress, ProtocolType protocolType)
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
                    foreach (DomainMap d in this.m_Manager.Mappings.Domains)
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
                        //Program.m_WaitingOn.Add(q.Name.ToLowerInvariant());

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

        /// <summary>
        /// This function is called by the DnsServer when an internal or threaded exception occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExceptionThrown(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.ToString());
        }
    }
}
