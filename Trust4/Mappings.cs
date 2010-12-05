using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.ObjectModel;
using DistributedServiceProvider.Base;
using ARSoft.Tools.Net.Dns;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Math;

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
            get
            {
                return this.p_Domains.AsReadOnly();
            }
        }
		
		private static RSACryptoServiceProvider CreateRSA(string containerName)
		{
		    CspParameters parms = new CspParameters();
		    parms.KeyContainerName = containerName;
		    RSACryptoServiceProvider rsa = 
		        new RSACryptoServiceProvider(parms);
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
			foreach (DomainMap m in this.p_Domains)
			{
				Console.Write(m.Domain + " == " + question.Name + "? ");
				if (m.Domain == question.Name)
				{
					Console.WriteLine("yes");
					msg.AnswerRecords.Add(m.Answer);
					return true;
				}
				else
					Console.WriteLine("no");
			}
			
			return false;
		}

		/// <summary>
		/// Loads the domain records from the mappings.txt file. 
		/// </summary>
    	public void Load()
        {
            using (StreamReader reader = new StreamReader(this.m_Path))
            {
                while (!reader.EndOfStream)
                {
                    string[] s = reader.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    string type = s[0].Trim();
                    string domain = s[1].Trim();
                    string target = s[2].Trim();
					string publicguid = s[3].Trim();
					string privateguid = s[4].Trim();

                    Console.Write("Mapping (" + type.ToUpperInvariant() + ") " + domain + " to " + target + "... ");
                    try
                    {
                        switch (type.ToUpperInvariant())
                        {
                            case "A":
                                IPAddress o = IPAddress.None;
                                IPAddress.TryParse(target, out o);
								this.Add(
							         new DnsQuestion(domain, RecordType.A, RecordClass.INet),
							         new ARecord(domain, 3600, o),
							         new EncryptionGuids(publicguid, privateguid)
							         );
                                Console.WriteLine("done.");
                                break;
                            case "CNAME":
								this.Add(
							         new DnsQuestion(domain, RecordType.A, RecordClass.INet),
							         new CNameRecord(domain, 3600, target),
							         new EncryptionGuids(publicguid, privateguid)
							         );
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
