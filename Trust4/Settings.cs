using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using DistributedServiceProvider.Base;

namespace Trust4
{
    public class Settings
    {
        private string m_Path = null;
        private int p_P2PPort = 12000;
        private int p_DNSPort = 53;
        private IPAddress p_LocalIP = IPAddress.None;
        private Guid p_NetworkID = Guid.Empty;
        private Identifier512 p_RoutingIdentifier = null;
		private uint p_UnixUID = 1000;
		private uint p_UnixGID = 1000;

        public Settings(string path)
        {
            this.m_Path = path;
        }

        public int P2PPort
        {
            get
            {
                return this.p_P2PPort;
            }
        }

        public int DNSPort
        {
            get
            {
                return this.p_DNSPort;
            }
        }

        public IPAddress LocalIP
        {
            get
            {
                return this.p_LocalIP;
            }
        }

        public Guid NetworkID
        {
            get
            {
                return this.p_NetworkID;
            }
        }

        public Identifier512 RoutingIdentifier
        {
            get
            {
                return this.p_RoutingIdentifier;
            }
        }

        public uint UnixUID
        {
            get
            {
                return this.p_UnixUID;
            }
        }

        public uint UnixGID
        {
            get
            {
                return this.p_UnixGID;
            }
        }

        public void Load()
        {
			bool setuid = false;
			bool setgid = false;
            foreach (var line in File.ReadAllLines(this.m_Path).OmitComments("#", "//").Select(a => a.ToLowerInvariant().Replace(" ", "").Replace("\t", "").Split('=')))
            {
                string setting = line[0].Trim();
                string value = line[1].Trim();

                switch (setting)
                {
                    case "peerport":
                        this.p_P2PPort = Convert.ToInt32(value);
                        break;
                    case "port":
                        this.p_P2PPort = Convert.ToInt32(value);
                        break;
                    case "dnsport":
                        this.p_DNSPort = Convert.ToInt32(value);
                        break;
                    case "localip":
                        this.p_LocalIP = IPAddress.Parse(line[1]);
                        break;
                    case "networkid":
                        this.p_NetworkID = new Guid(line[1]);
                        break;
                    case "routingidentifier":
                        var s = line[1].Split(',');
                        this.p_RoutingIdentifier = new Identifier512(new Guid(s[0]), new Guid(s[1]), new Guid(s[2]), new Guid(s[3]));
                        break;
                    case "uid":
                        this.p_UnixUID = Convert.ToUInt32(value);
						setuid = true;
                        break;
                    case "gid":
                        this.p_UnixGID = Convert.ToUInt32(value);
						setgid = true;
                        break;
                    default:
                        Console.WriteLine("Unknown setting " + line[0]);
                        break;
                }
            }
			
			if (Environment.OSVersion.Platform == PlatformID.Unix && (!setuid || !setgid))
			{
				Console.WriteLine("Warning!  You didn't set the 'uid' and 'gid' options in settings.txt.  This probably is going to work as you expect!");
			}
        }
    }
}
