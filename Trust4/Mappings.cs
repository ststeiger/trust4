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

        public void Add(string domain, IPAddress addr)
        {
            this.p_Domains.Add(new DomainMap(domain, addr));

            // Add to DHT.
            Identifier512 domainid = Identifier512.CreateKey("dns-a-" + domain);
            this.m_Manager.DataStore.Put(domainid, addr.GetAddressBytes());
        }

        public void AddCached(string domain, IPAddress addr)
        {
            this.p_Domains.Add(new DomainMap(domain, addr));

            // It's a cached mapping, so don't add to the DHT.
        }

        public void Load()
        {
            using (StreamReader reader = new StreamReader(this.m_Path))
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
                        this.Add(domain, o);
                        Console.WriteLine("done.");
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
