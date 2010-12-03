using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.ObjectModel;

namespace Trust4
{
    public class Mappings
    {
        private string m_Path = null;
        private List<DomainMap> p_Domains = new List<DomainMap>();
        private List<string> m_WaitingOn = new List<string>();

        public Mappings(string path)
        {
            this.m_Path = path;
        }

        public ReadOnlyCollection<DomainMap> Domains
        {
            get
            {
                return this.p_Domains.AsReadOnly();
            }
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
                        this.p_Domains.Add(new DomainMap(domain, o));
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
    }
}
