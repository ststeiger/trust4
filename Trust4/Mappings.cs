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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ARSoft.Tools.Net.Dns;
using DistributedServiceProvider.Base;

namespace Trust4
{
    public class Mappings
    {
        private string m_Path = null;
        private Manager m_Manager = null;
        private List<DomainMap> p_Domains = new List<DomainMap>();
        private List<string> m_WaitingOn = new List<string>();

        public Mappings(Manager manager, string path)
        {
            this.m_Path = path;
            this.m_Manager = manager;
        }

        public ReadOnlyCollection<DomainMap> Domains
        {
            get { return this.p_Domains.AsReadOnly(); }
        }

        private static RSACryptoServiceProvider CreateRSA(string containerName)
        {
            CspParameters parms = new CspParameters();
            parms.KeyContainerName = containerName;
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(parms);
            return rsa;
        }

        public struct EncryptionGuids
        {
            public Guid Public;
            public Guid Private;

            public EncryptionGuids(string publicguid)
            {
                this.Public = new Guid(publicguid);
                this.Private = Guid.Empty;
            }

            public EncryptionGuids(string publicguid, string privateguid)
            {
                this.Public = new Guid(publicguid);
                this.Private = new Guid(privateguid);
            }
        }

        public static byte[] Encrypt(EncryptionGuids guids, byte[] data)
        {
            // Create an encryptor and a decryptor.
            RSACryptoServiceProvider decryptor = Mappings.CreateRSA(guids.Public.ToString());
            RSACryptoServiceProvider encryptor = Mappings.CreateRSA(guids.Private.ToString());
            
            // Export the public key from the decryptor
            string key = decryptor.ToXmlString(false);
            
            // Load the public key into the encryptor
            encryptor.FromXmlString(key);
            
            // Now encrypt our data.
            return encryptor.Encrypt(data, true);
        }

        public static byte[] Decrypt(EncryptionGuids guids, byte[] encrypted)
        {
            // Create a decryptor.
            RSACryptoServiceProvider decryptor = Mappings.CreateRSA(guids.Public.ToString());
            
            return decryptor.Decrypt(encrypted, true);
        }

        /// <summary>
        /// Adds the specified question-answer pair to the DHT, adding an intermediatary CNAME record
        /// to prevent modification of the end destination for the domain.
        /// </summary>
        /// <param name="question">The original DNS question that will be asked.</param>
        /// <param name="answer">The original DNS answer that should be returned.</param>
        /// <param name="guids">The encryption GUIDs for this domain record.</param>
        public void Add(DnsQuestion question, DnsRecordBase answer, EncryptionGuids guids)
        {
            // First automatically create a DnsRecordBase based on the question.
            string keydomain = question.Name.ToLowerInvariant() + "." + guids.Public.ToString() + ".key";
            DnsRecordBase keyanswer = new CNameRecord(question.Name.ToLowerInvariant(), 3600, keydomain);
            
            // Add that CNAME record to the DHT.
            Identifier512 questionid = Identifier512.CreateKey(DnsSerializer.ToStore(question));
            this.m_Manager.DataStore.Put(questionid, Encoding.ASCII.GetBytes(DnsSerializer.ToStore(keyanswer)));
            
            // Now create a CNAME question that will be asked after looking up the original domain.
            DnsQuestion keyquestion = new DnsQuestion(keydomain, RecordType.CName, RecordClass.INet);
            
            // Add the original answer to the DHT, but encrypt it using our private key.
            Identifier512 keyquestionid = Identifier512.CreateKey(DnsSerializer.ToStore(keyquestion));
            this.m_Manager.DataStore.Put(keyquestionid, Mappings.Encrypt(guids, Encoding.ASCII.GetBytes(DnsSerializer.ToStore(answer))));
            
            // Add the domain to our cache.
            this.p_Domains.Add(new DomainMap(question, keyanswer));
            this.p_Domains.Add(new DomainMap(keyquestion, answer));
        }

        /// <summary>
        /// Adds the specified question-answer pair to the DHT, without any public-private key pair
        /// translation.  It is expected that this record points to a .key domain in it's answer.
        /// </summary>
        /// <param name="question">The original DNS question that will be asked.</param>
        /// <param name="answer">The original DNS answer that should be returned.</param>
        public void Add(DnsQuestion question, DnsRecordBase answer)
        {
            // Add the record to the DHT.
            Identifier512 questionid = Identifier512.CreateKey(DnsSerializer.ToStore(question));
            this.m_Manager.DataStore.Put(questionid, Encoding.ASCII.GetBytes(DnsSerializer.ToStore(answer)));

            // Add the domain to our cache.
            this.p_Domains.Add(new DomainMap(question, answer));
        }

        /// <summary>
        /// Add a question-answer pair to the cache.  This does not add any intermediatary CNAME
        /// records, so it should only be used for caching original .p2p requests from other peers.
        /// </summary>
        /// <param name="question">The original DNS question that will be asked.</param>
        /// <param name="answer">The original DNS answer that should be returned.</param>
        public void AddCached(DnsQuestion question, DnsRecordBase answer)
        {
            // Add the domain to our cache.
            this.p_Domains.Add(new DomainMap(question, answer));
        }

        /// <summary>
        /// Pushes the answer for the specified question if we know the answer.  Returns
        /// true if the answer was pushed onto the return message.
        /// </summary>
        /// <param name="msg">A reference to the return message to which answer records should be added.</param>
        /// <param name="question">The original DNS question.</param>
        /// <returns>Whether the answer was added to the return message.</returns>
        public bool Fetch(ref DnsMessage msg, DnsQuestion question)
        {
            bool found = false;
            foreach (DomainMap m in this.p_Domains)
            {
                Console.Write(m.Domain + " == " + question.Name + "? ");
                if (m.Domain == question.Name)
                {
                    Console.WriteLine("yes");
                    msg.AnswerRecords.Add(m.Answer);
                    found = true;
                }
                else
                    Console.WriteLine("no");
            }
            
            return found;
        }

        /// <summary>
        /// Loads the domain records from the mappings.txt file. 
        /// </summary>
        public void Load()
        {
            using (StreamReader reader = new StreamReader(this.m_Path))
            {
                foreach (var s in File.ReadAllLines(this.m_Path).OmitComments("#", "//").Select(a => a.ToLowerInvariant().Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)))
                {
                    string type = s[0].Trim();
                    
                    try
                    {
                        string domain = null;
                        string target = null;
                        string publicguid = null;
                        string privateguid = null;
                        string priority = null;
                        string tdomain = null;
                        switch (type.ToUpperInvariant())
                        {
                            case "A":
                                domain = s[1].Trim();
                                target = s[2].Trim();
                                publicguid = s[3].Trim();
                                privateguid = s[4].Trim();
                                Console.Write("Mapping (" + type.ToUpperInvariant() + ") " + domain + " to " + target + "... ");
                                IPAddress o = IPAddress.None;
                                IPAddress.TryParse(target, out o);
                                this.Add(new DnsQuestion(domain, RecordType.A, RecordClass.INet), new ARecord(domain, 3600, o), new EncryptionGuids(publicguid, privateguid));
                                Console.WriteLine("done.");
                                break;
                            case "CNAME":
                                domain = s[1].Trim();
                                target = s[2].Trim();
                                publicguid = s[3].Trim();
                                privateguid = s[4].Trim();
                                Console.Write("Mapping (" + type.ToUpperInvariant() + ") " + domain + " to " + target + "... ");
                                this.Add(new DnsQuestion(domain, RecordType.A, RecordClass.INet), new CNameRecord(domain, 3600, target), new EncryptionGuids(publicguid, privateguid));
                                Console.WriteLine("done.");
                                break;
                            case "MX":
                                priority = s[1].Trim();
                                domain = s[2].Trim();
                                target = s[3].Trim();
                                Console.Write("Mapping (" + type.ToUpperInvariant() + ") " + domain + " to " + target + " with priority " + priority + "... ");

                                // Get the target domain.
                                tdomain = this.GetPublicCNAME(target);
                                if (tdomain == null)
                                {
                                    Console.WriteLine("failed.");
                                    Console.WriteLine("A record must exist for domain target when MX record reached.  " + "Place the A record earlier in your mappings file or add it if needed.  " + "The MX record will be ignored.");
                                    break;
                                }
                                
                                this.Add(new DnsQuestion(domain, RecordType.A, RecordClass.INet), new MxRecord(domain, 3600, Convert.ToUInt16(priority), tdomain));
                                Console.WriteLine("done.");
                                break;
                            default:
                                Console.WriteLine("failed.");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("failed.");
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the translated name for domain.p2p (such as domain.p2p.publickey.key). 
        /// </summary>
        /// <param name="domain">
        /// A <see cref="System.String"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/>
        /// </returns>
        public string GetPublicCNAME(string domain)
        {
            foreach (DomainMap d in this.p_Domains)
            {
                if (d.Domain == domain)
                {
                    return d.CNAMETarget;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds the domain to the WaitingOn list.
        /// </summary>
        /// <param name="domain">The domain name.</param>
        public void BeginWait(string domain)
        {
            this.m_WaitingOn.Add(domain);
        }

        /// <summary>
        /// Removes the domain from the WaitingOn list.
        /// </summary>
        /// <param name="domain">The domain name.</param>
        public void EndWait(string domain)
        {
            this.m_WaitingOn.Remove(domain);
        }

        /// <summary>
        /// Returns whether we're waiting on a resolution for this domain name.
        /// </summary>
        /// <param name="domain">The domain name.</param>
        public bool Waiting(string domain)
        {
            return this.m_WaitingOn.Contains(domain);
        }
    }
}
