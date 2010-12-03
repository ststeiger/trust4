using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ARSoft.Tools.Net.Dns;
using System.Net;
using System.Net.Sockets;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using DistributedServiceProvider;
using Trust4.DataStorage;

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
                if (!q.Name.EndsWith(".p2p") && !q.Name.EndsWith(".key"))
                {
                    query.ReturnCode = ReturnCode.Refused;
                    return query;
                }

                Console.WriteLine("DNS LOOKUP - User asked for " + q.Name + " (" + q.RecordType.ToString().ToUpperInvariant() + ")");

                // Search our own mappings.
                bool found = false;
                foreach (DomainMap d in this.m_Manager.Mappings.Domains)
                {
                    if (q.Name.Equals(d.Domain, StringComparison.InvariantCultureIgnoreCase) ||
                        q.Name.EndsWith("." + d.Domain, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (q.RecordType != d.Type)
                        {
                            if (q.RecordType != RecordType.A || d.Type != RecordType.CName)
                                continue;
                        }

                        found = true;
                        switch (d.Type)
                        {
                            case RecordType.A:
                                Console.WriteLine("DNS LOOKUP - Found in cache (A, " + d.ATarget.ToString() + ")");
                                query.ReturnCode = ReturnCode.NoError;
                                query.AnswerRecords.Add(new ARecord(q.Name, 3600, d.ATarget));
                                break;
                            case RecordType.CName:
                                Console.WriteLine("DNS LOOKUP - Found in cache (CNAME, " + d.CNAMETarget.ToString() + ")");
                                query.ReturnCode = ReturnCode.NoError;
                                query.AnswerRecords.Add(new CNameRecord(q.Name, 3600, d.CNAMETarget));
                                break;
                            default:
                                found = false;
                                continue;
                        }
                        break;
                    }
                }

                if (!found && !this.m_Manager.Mappings.Waiting(q.Name.ToLowerInvariant()))
                {
                    // Since we're about to query peers, add this domain to our
                    // "waiting on" list which means that any requests for this
                    // domain from other peers will result in not found.
                    this.m_Manager.Mappings.BeginWait(q.Name.ToLowerInvariant());

                    // We haven't found it in our local cache.  Query our
                    // peers to see if they've got any idea where this site is.
                    Identifier512 domainid = Identifier512.CreateKey("dns-" + q.RecordType.ToString().ToUpperInvariant() + "-" + q.Name.ToLowerInvariant());
                    IEnumerable<DataResult> results = this.m_Manager.DataStore.Get(domainid);

                    IPAddress highest = IPAddress.None;
                    Contact highcontact = null;
                    decimal hightrust = 0;
                    foreach (DataResult r in results)
                    {
                        Contact source = r.Source;
                        IPAddress ip = new IPAddress(r.Data);

                        decimal trust = 0;
                        if (source is TrustedContact)
                            trust = (source as TrustedContact).TrustAmount;

                        if (trust > hightrust)
                        {
                            highest = ip;
                            highcontact = source;
                            hightrust = trust;
                        }
                    }

                    if (highest != IPAddress.None)
                    {
                        // We found a match.
                        found = true;

                        // Cache the result.
                        this.m_Manager.Mappings.AddCached(q.RecordType.ToString().ToUpperInvariant(), q.Name.ToLowerInvariant(), highest);

                        string sip = "<unknown>";
                        if (highcontact is UdpContact)
                            sip = (highcontact as UdpContact).Ip.ToString();
                        Console.WriteLine("DNS LOOKUP - Found via peer " + sip + " (" + highest.ToString() + ")");

                        // Add the result.
                        query.ReturnCode = ReturnCode.NoError;
                        query.AnswerRecords.Add(new ARecord(q.Name.ToLowerInvariant(), 3600, highest));
                    }

                    this.m_Manager.Mappings.EndWait(q.Name.ToLowerInvariant());
                }

                if (!found)
                {
                    query.ReturnCode = ReturnCode.ServerFailure;
                    Console.WriteLine("DNS LOOKUP - Not found");
                }

                return query;
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
