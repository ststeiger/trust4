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
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Daylight;

namespace Trust4
{
    public class Settings
    {
        private string m_Path = null;
        private int p_P2PPort = 12000;
        private int p_DNSPort = 53;
        private IPAddress p_LocalIP = IPAddress.None;
        private Guid p_NetworkID = Guid.Empty;
        private uint p_UnixUID = 1000;
        private uint p_UnixGID = 1000;

        public Settings(string path)
        {
            this.m_Path = path;
        }

        public int P2PPort
        {
            get { return this.p_P2PPort; }
        }

        public int DNSPort
        {
            get { return this.p_DNSPort; }
        }

        public IPAddress LocalIP
        {
            get { return this.p_LocalIP; }
        }

        public Guid NetworkID
        {
            get { return this.p_NetworkID; }
        }

        public uint UnixUID
        {
            get { return this.p_UnixUID; }
        }

        public uint UnixGID
        {
            get { return this.p_UnixGID; }
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
                        if (line[1].Equals("dynamic", StringComparison.InvariantCultureIgnoreCase))
                        {
                            try
                            {
                                WebClient c = new WebClient();
                                this.p_LocalIP = IPAddress.Parse(c.DownloadString("http://www.whatismyip.com/automation/n09230945.asp"));
                            }
                            catch (WebException)
                            {
                                Console.WriteLine("DYNAMIC IP ERROR: Unable to retrieve dynamic IP address from whatsismyip.com.");
                            }
                        }
                        else
                            this.p_LocalIP = IPAddress.Parse(line[1]);
                        Console.Title = this.p_LocalIP.ToString();
                        break;
                    case "networkid":
                        this.p_NetworkID = new Guid(line[1]);
                        break;
                    case "routingidentifier":
                        Console.WriteLine("SETTINGS WARNING: RoutingIdentifier setting is deprecated and ignored");
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
                        Console.WriteLine("SETTINGS WARNING: Unknown setting " + line[0]);
                        break;
                }
            }
            
            if (Environment.OSVersion.Platform == PlatformID.Unix && ( !setuid || !setgid ))
            {
                Console.WriteLine("SETTINGS WARNING: You didn't set the 'uid' and 'gid' options in settings.txt.  This is probably not going to work as you expect!");
            }
        }
    }
}
