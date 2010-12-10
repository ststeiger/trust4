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
using System.Collections.Generic;
using Data4;

namespace Trust4
{
    public class Settings
    {
        private string m_Path = null;
        private int p_P2PPort = 12000;
        private int p_DNSPort = 53;
        private IPAddress p_LocalIP = IPAddress.None;
        private ID p_RoutingIdentifier = null;
        private uint p_UnixUID = 1000;
        private uint p_UnixGID = 1000;
        private bool p_Configured = false;
        private bool p_Online = false;
        private bool p_Public = false;

        public Settings(string path)
        {
            this.m_Path = path;
        }

        public int P2PPort
        {
            get { return this.p_P2PPort; }
            set { this.p_P2PPort = value; }
        }

        public int DNSPort
        {
            get { return this.p_DNSPort; }
            set { this.p_DNSPort = value; }
        }

        public IPAddress LocalIP
        {
            get { return this.p_LocalIP; }
            set { this.p_LocalIP = value; }
        }

        public ID RoutingIdentifier
        {
            get { return this.p_RoutingIdentifier; }
            set { this.p_RoutingIdentifier = value; }
        }

        public uint UnixUID
        {
            get { return this.p_UnixUID; }
        }

        public uint UnixGID
        {
            get { return this.p_UnixGID; }
        }

        public bool Configured
        {
            get { return this.p_Configured; }
            set { this.p_Configured = value; }
        }

        public bool Online
        {
            get { return this.p_Online; }
            set { this.p_Online = value; }
        }

        public bool Public
        {
            get { return this.p_Public; }
        }

        public void Load()
        {
            bool setuid = false;
            bool setgid = false;
            foreach (var line in File.ReadAllLines(this.m_Path).OmitComments("#", "//").Select(a => a.ToLowerInvariant().Split(new char[] { '=' }, 2)))
            {
                string setting = line[0].Trim();
                string value = line[1].Trim();
                
                switch (setting)
                {
                    case "configured":
                        this.p_Configured = Convert.ToBoolean(value);
                        break;
                    case "ip.port.p2p":
                        this.p_P2PPort = Convert.ToInt32(value);
                        break;
                    case "ip.port.dns":
                        this.p_DNSPort = Convert.ToInt32(value);
                        break;
                    case "ip.address":
                        if (value.Equals("dynamic", StringComparison.InvariantCultureIgnoreCase))
                            this.p_LocalIP = LoadDynamicIp();
                        else
                            this.p_LocalIP = IPAddress.Parse(value);
                        Console.Title = this.p_LocalIP.ToString();
                        break;
                    case "id.routing":
                        string[] gs = value.Split(new char[] {
                            ' ',
                            ',',
                            '\t'
                        }, StringSplitOptions.RemoveEmptyEntries);
                        Guid a = new Guid(gs[0]);
                        Guid b = new Guid(gs[1]);
                        Guid c = new Guid(gs[2]);
                        Guid d = new Guid(gs[3]);
                        this.p_RoutingIdentifier = new ID(a, b, c, d);
                        break;
                    case "unix.uid":
                        this.p_UnixUID = Convert.ToUInt32(value);
                        setuid = true;
                        break;
                    case "unix.gid":
                        this.p_UnixGID = Convert.ToUInt32(value);
                        setgid = true;
                        break;
                    case "info.public":
                        this.p_Public = Convert.ToBoolean(value);
                        break;
                    default:
                        Console.WriteLine("Unknown setting " + setting);
                        break;
                }
            }
            
            if (Environment.OSVersion.Platform == PlatformID.Unix && ( !setuid || !setgid ))
            {
                Console.WriteLine("Warning!  You didn't set the 'unix.uid' and 'unix.gid' options in settings.txt.  This is probably not going to work as you expect!");
            }
        }

        readonly static Random r = new Random();
        readonly static KeyValuePair<string, Func<string, IPAddress>>[] dynamicIpSources = new KeyValuePair<string, Func<string, IPAddress>>[]
            {
                new KeyValuePair<string, Func<string, IPAddress>>("http://www.whatismyip.com/automation/n09230945.asp", (s) => IPAddress.Parse(s)),
                new KeyValuePair<string, Func<string, IPAddress>>("http://www.lusion.co.za/ip", (s) => IPAddress.Parse(s)),
            };

        public IPAddress LoadDynamicIp()
        {
            int start;
            lock (r) { start = r.Next(dynamicIpSources.Length);}

            WebClient c = new WebClient();
            for (int i = 0; i < dynamicIpSources.Length; i++)
            {
                try
                {
                    int index = (i + start) % dynamicIpSources.Length;

                    var source = dynamicIpSources[index];

                    var downloadedString = c.DownloadString(source.Key);

                    return source.Value(downloadedString);
                }
                catch (Exception e) { Console.WriteLine("Error establishing external IP: " + e.Message); }
            }

            throw new InvalidOperationException("No sites successfully established an external IP");
        }
    }
}
