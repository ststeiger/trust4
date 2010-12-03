using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.ObjectModel;
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
            get
            {
                return this.p_Domains.AsReadOnly();
            }
        }

        public void Add(string type, string domain, IPAddress addr)
        {
            this.p_Domains.Add(new DomainMap(domain, addr));

            // Add to DHT.
            Identifier512 domainid = Identifier512.CreateKey("dns-" + type.ToUpperInvariant() + "-" + domain);
            this.m_Manager.DataStore.Put(domainid, addr.GetAddressBytes());
        }

        public void Add(string type, string domain, string target)
        {
            this.p_Domains.Add(new DomainMap(domain, target));

            // Add to DHT.
            Identifier512 domainid = Identifier512.CreateKey("dns-" + type.ToUpperInvariant() + "-" + domain);
            this.m_Manager.DataStore.Put(domainid, Encoding.ASCII.GetBytes(target));
        }

        public void AddCached(string type, string domain, IPAddress addr)
        {
            this.p_Domains.Add(new DomainMap(domain, addr));

            // It's a cached mapping, so don't add to the DHT.
        }

        public void AddCached(string type, string domain, string target)
        {
            this.p_Domains.Add(new DomainMap(domain, target));

            // It's a cached mapping, so don't add to the DHT.
        }

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

                    Console.Write("Mapping (" + type.ToUpperInvariant() + ") " + domain + " to " + target + "... ");
                    try
                    {
                        switch (type.ToUpperInvariant())
                        {
                            case "A":
                                IPAddress o = IPAddress.None;
                                IPAddress.TryParse(target, out o);
                                this.Add(type, domain, o);
                                Console.WriteLine("done.");
                                break;
                            case "CNAME":
                                this.Add(type, domain, target);
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
