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
    /// <summary>
    /// A class for storing both the predefined peer name and identifier in a Dictionary value.
    /// </summary>
    public class PeerMap
    {
        private string p_Name = null;
        private ID p_Identifier = null;

        public PeerMap(string name, ID id)
        {
            this.p_Name = name;
            this.p_Identifier = id;
        }

        /// <summary>
        /// The name of the predefined peer.
        /// </summary>
        public string Name
        {
            get { return this.p_Name; }
        }

        /// <summary>
        /// The identifier of the predefined peer.
        /// </summary>
        public ID Identifier
        {
            get { return this.p_Identifier; }
        }
    }

    /// <summary>
    /// The peer management page.
    /// </summary>
    public class PeersPage : Page
    {
        public static readonly Dictionary<IPEndPoint, PeerMap> m_MappedPeers = new Dictionary<IPEndPoint, PeerMap>
        {
            {
                new IPEndPoint(IPAddress.Parse("74.207.247.199"), 12000),
                new PeerMap(
                    "Trust Cache 01",
                    new ID(
                        new Guid("baef6bc6-3959-476b-89cd-35957663cedf"),
                        new Guid("1615ab0d-6b65-42e4-9983-3e4f79f01900"),
                        new Guid("348fe616-a98f-4315-b32e-ac07a90c5686"),
                        new Guid("636803ef-a779-4e33-8b93-5407249e531a")
                    )
                )
            },
            {
                new IPEndPoint(IPAddress.Parse("173.193.221.29"), 12000),
                new PeerMap(
                    "Trust Cache 02",
                    new ID(
                        new Guid("99743a87-7d31-4407-8cb4-1443fcaa0b97"),
                        new Guid("0362060b-a64e-483a-b53f-ff9502ca9673"),
                        new Guid("028e7318-2fce-46d1-af74-5a47c5456042"),
                        new Guid("21322e40-40e8-4b1a-b0c0-0977fe885501")
                    )
                )
            }
        };

        private bool m_InvalidPeerAdd = false;
        private bool m_InvalidPeerRemove = false;

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
            else if (this.Parameters.Count == 1 && this.Parameters[1] == "remove" && this.Manager.Dht != null)
            {
                // Attempt to remove the peer.
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
                    this.m_InvalidPeerRemove = true;
                    return false;
                }

                // Remove the peer.
                this.Manager.Dht.Contacts.RemoveAll((Contact c) =>
                    {
                        if (c.Identifier == new ID(guid1, guid2, guid3, guid4) && c.EndPoint.Address.Equals(ip) && c.EndPoint.Port == port)
                            return true;
                        else
                            return false;
                    });
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
            if (this.m_InvalidPeerRemove)
                this.Output("<p><strong style='color: #F00;'>The peer IP address or routing identifer was not valid.</strong></p>");
            this.Output("<table cellpadding='5' border='1' width='100%'>");
            this.Output("   <tr>");
            this.Output("       <th>Peer Name</th>");
            this.Output("       <th>Peer Endpoint</th>");
            this.Output("       <th colspan='2'>Peer Identifier</th>");
            this.Output("   </tr>");
            int counted = 0;
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
                            this.Output("       <td>" + PeersPage.m_MappedPeers[ipep].Name + "</td>");
                            found = true;
                            counted += 1;
                            break;
                        }
                    }
                    if (!found)
                        this.Output("       <td>&lt;unnamed&gt;</td>");
                    this.Output("       <td>" + c.EndPoint + "</td>");
                    this.Output("       <td>" + c.Identifier + "</td>");
                    this.Output("       <td width='300'>");
                    this.Output("           <form action='/peers/remove' method='POST'>");
                    this.Output("               <input name='endpoint' type='hidden' value='" + c.EndPoint + "' />");
                    this.Output("               <input name='identifier' type='hidden' value='" + c.Identifier + "' />");
                    this.Output("               <div class='buttons'>");
                    this.Output("                   <button class='negative' type='submit'>Remove Peer</button>");
                    this.Output("               </div>");
                    this.Output("           </form>");
                    this.Output("       </td>");
                    this.Output("   </tr>");
                }
            }
            this.Output("</table>");

            // Predefined peer form.
            if (this.Manager.Dht != null)
            {
                if (counted != PeersPage.m_MappedPeers.Keys.Count)
                {
                    // The user hasn't added all of the predefined peers, so make it easy for them to do so.
                    this.Output("<h2>Predefined Peers</h2>");
                    this.Output("<table cellpadding='5' border='1' width='100%'>");
                    this.Output("   <tr>");
                    this.Output("       <th>Peer Name</th>");
                    this.Output("       <th>Peer Endpoint</th>");
                    this.Output("       <th colspan='2'>Peer Identifier</th>");
                    this.Output("   </tr>");
                    if (this.Manager.Dht == null)
                    {
                        this.Output("   <tr>");
                        this.Output("       <td colspan='3'>This information can not be shown while in offline mode.</td>");
                        this.Output("   </tr>");
                    }
                    else
                    {
                        foreach (IPEndPoint ipep in PeersPage.m_MappedPeers.Keys)
                        {
                            bool found = false;
                            foreach (Contact c in this.Manager.Dht.Contacts)
                            {
                                if (ipep.Address.Equals(c.EndPoint.Address) && ipep.Port == c.EndPoint.Port)
                                    found = true;
                            }

                            // Only list if it's not already a contact.
                            if (!found)
                            {
                                this.Output("   <tr>");
                                this.Output("       <td>" + PeersPage.m_MappedPeers[ipep].Name + "</td>");
                                this.Output("       <td>" + ipep + "</td>");
                                this.Output("       <td>" + PeersPage.m_MappedPeers[ipep].Identifier + "</td>");
                                this.Output("       <td width='300'>");
                                this.Output("           <form action='/peers/add' method='POST'>");
                                this.Output("               <input name='endpoint' type='hidden' value='" + ipep + "' />");
                                this.Output("               <input name='identifier' type='hidden' value='" + PeersPage.m_MappedPeers[ipep].Identifier + "' />");
                                this.Output("               <div class='buttons'>");
                                this.Output("                   <button class='positive' type='submit'>Add Peer</button>");
                                this.Output("               </div>");
                                this.Output("           </form>");
                                this.Output("       </td>");
                                this.Output("   </tr>");
                            }
                        }
                    }
                    this.Output("</table>");
                }
            }

            // Add peer form.
            if (this.Manager.Dht != null)
            {
                this.Output("<h2>Add Custom Peer</h2>");
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
                this.Output("           <td colspan='2' style='padding: 5px;'><div style='width: auto;' align='right' class='buttons'><button class='positive' style='margin-left: 208px;' type='submit'>Add Peer</button></div></td>");
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
