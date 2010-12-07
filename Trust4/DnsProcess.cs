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
using System.Text;
using Daylight;

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
                    else if (!this.m_Manager.Mappings.Waiting(q.Name.ToLowerInvariant()))
                    {
                        // Search DHT.
                        
                        // Since we're about to query peers, add this domain to our
                        // "waiting on" list which means that any requests for this
                        // domain from other peers will result in not found.
                        Console.WriteLine("DNS LOOKUP - Begin wait on " + q.Name);
                        this.m_Manager.Mappings.BeginWait(q.Name.ToLowerInvariant());

                        try
                        {
                        
                            // We haven't found it in our local cache.  Query our
                            // peers to see if they've got any idea where this site is.
                            Console.WriteLine("DNS LOOKUP - Create identifier on " + q.Name);
                            ID domainid = ID.Hash(DnsSerializer.ToStore(q));
                            Console.WriteLine("DNS LOOKUP - Retrieve results on " + q.Name);
                            IList<Result> results = this.m_Manager.KademliaNode.Get(domainid);
    
                            // Find out the most trusted results.
                            Console.WriteLine("DNS LOOKUP - Find trusted result on " + q.Name + " (total results: " + results.Count + ")");
                            Contact trustedcontact = null;
                            decimal trustedamount = 0;
                            foreach (Result r in results)
                            {
                                Contact source = r.Contact;
                                DnsRecordBase result = DnsSerializer.FromStore(q.Name.ToLowerInvariant(), ByteString.GetBytes(r.Data));

                                // Assign if this result is trusted higher than the current result.
                                decimal trust = 0;
                                if (source is TrustedContact)
                                    trust = ( source as TrustedContact ).TrustAmount;
    
                                if (( trust > trustedamount || ( trustedamount == 0 && trustedcontact == null ) ) && result != null)
                                {
                                    trustedcontact = source;
                                    trustedamount = trust;
                                }
                            }
    
                            // Now get the results from the most trusted person.
                            if (trustedcontact == null)
                                Console.WriteLine("DNS LOOKUP - There are no trusted results");
                            else
                                Console.WriteLine("DNS LOOKUP - Trusted results comes from " + trustedcontact.GetID().ToString());
    
                            foreach (Result r in results)
                            {
                                if (r.Contact == trustedcontact)
                                {
                                    Console.WriteLine("DNS LOOKUP - Retrieving result from store " + q.Name);
                                    DnsRecordBase result = DnsSerializer.FromStore(q.Name.ToLowerInvariant(), ByteString.GetBytes(r.Data));
    
                                    // Cache the result.
                                    Console.WriteLine("DNS LOOKUP - Adding to cache " + q.Name);
                                    this.m_Manager.Mappings.AddCached(q, result);

                                    Console.WriteLine("DNS LOOKUP - Found via peer " + trustedcontact.GetEndPoint().ToString() + " (" + result.RecordType.ToString() + ")");
    
                                    // Add the result.
                                    query.ReturnCode = ReturnCode.NoError;
                                    query.AnswerRecords.Add(result);
                                }
                            }
                            
                            // Remove the domain from the waiting on list.
                            Console.WriteLine("DNS LOOKUP - End wait on " + q.Name);
                            this.m_Manager.Mappings.EndWait(q.Name.ToLowerInvariant());
                        }
                        catch (Exception e)
                        {
                            // Ugh.. something went wrong with the DHT.
                            Console.WriteLine(e.ToString());
                            Console.WriteLine("Caught an exception in the DHT... Hopefully everything still works o_o'.");
                        }
                        finally
                        {
                            // Remove the domain from the waiting on list.
                            this.m_Manager.Mappings.EndWait(q.Name.ToLowerInvariant());
                        }
                    }
                }

                else if (q.Name.EndsWith(".key"))
                {
                    // We are quering a domain mapping with public-private key pair.
                    Console.WriteLine("DNS LOOKUP - User asked for " + q.Name + " (" + q.RecordType.ToString().ToUpperInvariant() + ")");
                    
                    // Search cache.
                    if (this.m_Manager.Mappings.Fetch(ref query, q))
                        Console.WriteLine("DNS LOOKUP - Returned answer from cache.");
                    else if (!this.m_Manager.Mappings.Waiting(q.Name.ToLowerInvariant()))
                    {
                        // Search DHT.
                        
                        // Since we're about to query peers, add this domain to our
                        // "waiting on" list which means that any requests for this
                        // domain from other peers will result in not found.
                        this.m_Manager.Mappings.BeginWait(q.Name.ToLowerInvariant());

                        try
                        {
                            // We haven't found it in our local cache.  Query our
                            // peers to see if they've got any idea where this site is.
                            ID domainid = ID.Hash(DnsSerializer.ToStore(q));
                            IList<Result> results = this.m_Manager.KademliaNode.Get(domainid);
                        
                            // We need to fetch the public key from the domain request so
                            // that we can decrypt / verify the results.
                            string[] s = q.Name.Split(new char[] { '.' });
                            if (s.Length >= 2)
                            {
                                // The .key domain is valid.
                                byte[] publichash = ByteString.GetBase32Bytes(s[s.Length - 2]);
                                Console.WriteLine(s[s.Length - 2]);
                                
                                // Loop through the results; trust order doesn't matter here
                                // because we have the public hash to verify the data.  If verification
                                // results in something the Serializer can get a record from, then
                                // we know that it's valid.
                                Console.WriteLine("DNS LOOKUP - Find verified result on " + q.Name + " (total results: " + results.Count + ")");
                                foreach (Result r in results)
                                {
                                    byte[] v = Mappings.Verify(
                                        publichash,
                                        ByteString.GetBytes(r.Data)
                                        );
                                    if (v == null)
                                    {
                                        Console.WriteLine("Unable to verify the result of the DNS query!");
                                        continue;
                                    }
    
                                    DnsRecordBase record = DnsSerializer.FromStore(
                                        q.Name.ToLowerInvariant(),
                                        v
                                        );
                                    
                                    if (record != null)
                                    {
                                        // Cache the result.
                                        Console.WriteLine("DNS LOOKUP - Adding to cache " + q.Name);
                                        this.m_Manager.Mappings.AddCached(q, record);
    
                                        string sip = r.Contact.GetEndPoint().ToString();
                                        Console.WriteLine("DNS LOOKUP - Found via peer " + sip + " (" + record.RecordType.ToString() + ")");
                                        
                                        // Add the result.
                                        query.ReturnCode = ReturnCode.NoError;
                                        query.AnswerRecords.Add(record);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // Ugh.. something went wrong with the DHT.
                            Console.WriteLine(e.ToString());
                            Console.WriteLine("Caught an exception in the DHT... Hopefully everything still works o_o'.");
                        }
                        finally
                        {
                            // Remove the domain from the waiting on list.
                            this.m_Manager.Mappings.EndWait(q.Name.ToLowerInvariant());
                        }
                    }
                }
            }

            if (query.AnswerRecords.Count == 0)
            {
                Console.WriteLine("DNS LOOKUP - No results");
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
