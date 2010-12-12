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
using System.Text;
using Trust4;
using Data4;
using System.Net;

namespace Admin4.Pages
{
    public class PeersPage : Page
    {
        public static readonly Dictionary<IPEndPoint, string> m_MappedPeers = new Dictionary<IPEndPoint, string>
        {
            {
                new IPEndPoint(IPAddress.Parse("74.207.247.199"), 12000),
                "Trust Cache 01"
            },
            {
                new IPEndPoint(IPAddress.Parse("173.193.221.29"), 12000),
                "Trust Cache 02"
            }
        };

        private bool m_InvalidPeerAdd = false;

        public PeersPage(Manager manager)
            : base(manager, new List<string> { "peers" })
        {
        }

        protected override bool OnPageInit()
        {
            if (!this.Manager.Settings.Configured)
            {
                this.Response.Status = HttpStatusCode.Redirect;
                this.Response.AddHeader("Location", "/");
                return true;
            }

            this.m_InvalidPeerAdd = false;
            if (this.Parameters.Count == 1 && this.Parameters[1] == "add" && this.Manager.Dht != null)
            {
                // Attempt to add the peer.
                // TODO: Catch exceptions.
                string[] ips = this.Request.Form["endpoint"].Value.Split(new char[] { ':' }, 2);
                string[] guids = this.Request.Form["identifier"].Value.Split(new char[] { ' ' }, 4);
                Guid guid1 = new Guid(guids[0]);
                Guid guid2 = new Guid(guids[1]);
                Guid guid3 = new Guid(guids[2]);
                Guid guid4 = new Guid(guids[3]);
                IPAddress ip = IPAddress.Parse(ips[0]);
                ushort port = Convert.ToUInt16(ips[1]);

                // Check validity.
                if (ip.Equals(IPAddress.None) || ip.Equals(IPAddress.Any) ||
                    ip.Equals(IPAddress.Loopback) || ip.Equals(this.Manager.Settings.LocalIP) ||
                    (guid1.Equals(Guid.Empty) && guid2.Equals(Guid.Empty) &&
                     guid3.Equals(Guid.Empty) && guid4.Equals(Guid.Empty)) ||
                    port == 0 || port == ushort.MaxValue || port == ushort.MinValue)
                {
                    this.m_InvalidPeerAdd = true;
                    return false;
                }

                // Add the peer.
                this.Manager.Dht.Contacts.Add(new Contact(new ID(guid1, guid2, guid3, guid4), new IPEndPoint(ip, port)));
                this.Manager.SavePeers();
            }

            return false;
        }

        protected override void OnPageHead()
        {
            this.Output("<title>Trust4 Administration Panel - Peer Management</title>");
        }

        protected override void OnPageBody()
        {
            // Peer management
            this.Output("<h2>Current Peers</h2>");
            this.Output("<table cellpadding='5' border='1' width='100%'>");
            this.Output("   <tr>");
            this.Output("       <th>Peer Name</th>");
            this.Output("       <th>Peer Endpoint</th>");
            this.Output("       <th>Peer Identifier</th>");
            this.Output("   </tr>");
            if (this.Manager.Dht == null)
            {
                this.Output("   <tr>");
                this.Output("       <td colspan='3'>This information can not be shown while in offline mode.</td>");
                this.Output("   </tr>");
            }
            else
            {
                foreach (Contact c in this.Manager.Dht.Contacts)
                {
                    this.Output("   <tr>");
                    bool found = false;
                    foreach (IPEndPoint ipep in PeersPage.m_MappedPeers.Keys)
                    {
                        if (ipep.Address.Equals(c.EndPoint.Address) && ipep.Port == c.EndPoint.Port)
                        {
                            this.Output("       <td>" + PeersPage.m_MappedPeers[ipep] + "</td>");
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        this.Output("       <td>&lt;unnamed&gt;</td>");
                    this.Output("       <td>" + c.EndPoint + "</td>");
                    this.Output("       <td>" + c.Identifier + "</td>");
                    this.Output("   </tr>");
                }
            }
            this.Output("</table>");

            // Add peer form.
            if (this.Manager.Dht != null)
            {
                this.Output("<h2>Add Peer</h2>");
                if (this.m_InvalidPeerAdd)
                    this.Output("<p><strong style='color: #F00;'>The peer IP address or routing identifer was not valid.</strong></p>");
                this.Output("<form action='/peers/add' method='POST'>");
                this.Output("   <table cellpadding='0' border='1' width='100%'>");
                this.Output("       <tr>");
                this.Output("           <th style='padding: 5px; width: 200px;'>Peer Endpoint:</th>");
                this.Output("           <td style='border: none;'><input name='endpoint' value='0.0.0.0:12000' /></td>");
                this.Output("       </tr>");
                this.Output("       <tr>");
                this.Output("           <th style='padding: 5px; width: 200px;'>Peer Identifier</th>");
                this.Output("           <td style='border: none;'><input name='identifier' value='00000000-0000-0000-0000-000000000000 00000000-0000-0000-0000-000000000000 00000000-0000-0000-0000-000000000000 00000000-0000-0000-0000-000000000000' /></td>");
                this.Output("       </tr>");
                this.Output("       <tr>");
                this.Output("           <td colspan='2' style='padding: 5px;'><div style='width: auto;' align='right' class='buttons'><button style='margin-left: 208px;' type='submit'>Add Peer</button></div></td>");
                this.Output("       </tr>");
                this.Output("   </table>");
                this.Output("</form>");
            }
        }

        protected override void OnPageExit()
        {
        }
    }
}
