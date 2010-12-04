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
using System.Net;
using System.Net.Sockets;
using ARSoft.Tools.Net.Dns;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
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
                if (q.Name.EndsWith(".p2p"))
                {
                    // We are quering a top-level domain that has no public key attached.
                    Console.WriteLine("DNS LOOKUP - User asked for " + q.Name + " (" + q.RecordType.ToString().ToUpperInvariant() + ")");
                    
                    // Search cache.
                    if (this.m_Manager.Mappings.Fetch(ref query, q))
                        Console.WriteLine("DNS LOOKUP - Returned answer from cache.");
                    else
                    {
                        // Search DHT.
                        
                        // Since we're about to query peers, add this domain to our
                        // "waiting on" list which means that any requests for this
                        // domain from other peers will result in not found.
                        this.m_Manager.Mappings.BeginWait(q.Name.ToLowerInvariant());
                        
                        // We haven't found it in our local cache.  Query our
                        // peers to see if they've got any idea where this site is.
                        Identifier512 domainid = Identifier512.CreateKey(DnsSerializer.ToStore(q));
                        IEnumerable<DataResult> results = this.m_Manager.DataStore.Get(domainid);
                        
                        // Get the most trusted result.
                        DnsRecordBase highest = null;
                        Contact highcontact = null;
                        decimal hightrust = 0;
                        foreach (DataResult r in results)
                        {
                            Contact source = r.Source;
                            DnsRecordBase result = DnsSerializer.FromStore(q.Name.ToLowerInvariant(), r.Data);
                            
                            // Assign if this result is trusted higher than the current result.
                            decimal trust = 0;
                            if (source is TrustedContact)
                                trust = ( source as TrustedContact ).TrustAmount;
                            
                            if (( trust > hightrust || ( hightrust == 0 && highest == null ) ) && result != null)
                            {
                                highest = result;
                                highcontact = source;
                                hightrust = trust;
                            }
                        }
                        
                        // Check to see whether we got a result.
                        if (highest != null)
                        {
                            // Cache the result.
                            this.m_Manager.Mappings.AddCached(q, highest);
                            
                            string sip = "<unknown>";
                            if (highcontact is UdpContact)
                                sip = ( highcontact as UdpContact ).Ip.ToString();
                            Console.WriteLine("DNS LOOKUP - Found via peer " + sip + " (" + highest.RecordType.ToString() + ")");
                            
                            // Add the result.
                            query.ReturnCode = ReturnCode.NoError;
                            query.AnswerRecords.Add(highest);
                        }
                        
                        // Remove the domain from the waiting on list.
                        this.m_Manager.Mappings.EndWait(q.Name.ToLowerInvariant());
                    }
                }

                else if (q.Name.EndsWith(".key"))
                {
                    // We are quering a domain mapping with public-private key pair.
                    Console.WriteLine("DNS LOOKUP - User asked for " + q.Name + " (" + q.RecordType.ToString().ToUpperInvariant() + ")");
                    
                    // Search cache.
                    if (this.m_Manager.Mappings.Fetch(ref query, q))
                        Console.WriteLine("DNS LOOKUP - Returned answer from cache.");
                    else
                    {
                        // Search DHT.
                        
                        // Since we're about to query peers, add this domain to our
                        // "waiting on" list which means that any requests for this
                        // domain from other peers will result in not found.
                        this.m_Manager.Mappings.BeginWait(q.Name.ToLowerInvariant());
                        
                        // We haven't found it in our local cache.  Query our
                        // peers to see if they've got any idea where this site is.
                        Identifier512 domainid = Identifier512.CreateKey(DnsSerializer.ToStore(q));
                        IEnumerable<DataResult> results = this.m_Manager.DataStore.Get(domainid);
                        
                        // We need to fetch the public key from the domain request so
                        // that we can decrypt / verify the results.
                        string[] s = q.Name.Split(new char[] { '.' });
                        if (s.Length >= 2)
                        {
                            // The .key domain is valid.
                            Mappings.EncryptionGuids guids = new Mappings.EncryptionGuids(s[s.Length - 2]);
                            
                            // Loop through the results; trust order doesn't matter here
                            // because we have the public key to decrypt the data.  If decryption
                            // results in something the Serializer can get a record from, then
                            // we know that it's valid.
                            foreach (DataResult r in results)
                            {
                                DnsRecordBase record = DnsSerializer.FromStore(q.Name.ToLowerInvariant(), Mappings.Decrypt(guids, r.Data));
                                
                                if (record != null)
                                {
                                    string sip = "<unknown>";
                                    if (r.Source is UdpContact)
                                        sip = ( r.Source as UdpContact ).Ip.ToString();
                                    Console.WriteLine("DNS LOOKUP - Found via peer " + sip + " (" + record.RecordType.ToString() + ")");
                                    
                                    // Add the result.
                                    query.ReturnCode = ReturnCode.NoError;
                                    query.AnswerRecords.Add(record);
                                    break;
                                }
                            }
                        }
                        
                        // Remove the domain from the waiting on list.
                        this.m_Manager.Mappings.EndWait(q.Name.ToLowerInvariant());
                    }
                }
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
