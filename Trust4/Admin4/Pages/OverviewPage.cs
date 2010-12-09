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
    public class OverviewPage : Page
    {
        private Manager m_Manager = null;

        public OverviewPage(Manager manager)
            : base(new List<string> { "", "overview" })
        {
            this.m_Manager = manager;
        }

        protected override void OnPageInit()
        {
        }

        protected override void OnPageHead()
        {
            this.Output("<title>Trust4 Administration Panel - Overview</title>");
        }

        protected override void OnPageBody()
        {
            // Overview, links to configuration etc.
            this.Output("<h2>Overview</h2>");
            this.Output("<p>Welcome to the .P2P DNS network.  You are connected via the Trust4 0.3.0 demonstration server.</p>");
            this.Output("<p>It appears you haven't yet configured your server; select an option below to get started.</p>");
            this.Output("<div class='buttons'>");
            this.Output("   <a href='' class='positive noimg'>");
            this.Output("       Automatic Configuration");
            this.Output("   </a>");
            this.Output("   ");
            this.Output("   <a href='' class='regular noimg'>");
            this.Output("       Manual Configuration");
            this.Output("   </a>");
            this.Output("</div>");

            // Raw information.
            this.Output("<h2>Basic Information</h2>");
            this.Output("<table cellpadding='5' border='1' width='100%'>");
            this.Output("   <tr>");
            this.Output("       <th rowspan='3' width='300'>IP Address and Ports</th>");
            this.Output("       <td width='300'><strong>IP Address:</strong></td>");
            this.Output("       <td>" + this.m_Manager.Settings.LocalIP + "</td>");
            this.Output("   </tr>");
            this.Output("   <tr>");
            this.Output("       <td width='300'><strong>P2P Port:</em></strong>");
            this.Output("       <td>" + this.m_Manager.Settings.P2PPort + "</td>");
            this.Output("   </tr>");
            this.Output("   <tr>");
            this.Output("       <td width='300'><strong>DNS Port:</em></strong>");
            this.Output("       <td>" + this.m_Manager.Settings.DNSPort + "</td>");
            this.Output("   </tr>");
            this.Output("   <tr>");
            this.Output("       <th width='300'>Routing Identifier</th>");
            this.Output("       <td colspan='2'>" + this.m_Manager.Settings.RoutingIdentifier + "</td>");
            this.Output("   </tr>");
            this.Output("   <tr>");
            this.Output("       <th width='300'>Network Identifier</th>");
            this.Output("       <td colspan='2'>" + this.m_Manager.Settings.NetworkID + "</td>");
            this.Output("   </tr>");
            this.Output("</table>");
            this.Output("<h2>Contact and Mapping Information</h2>");
            this.Output("<table cellpadding='5' border='1' width='100%'>");
            this.Output("   <tr>");
            this.Output("       <th width='300' rowspan='" + (1 + this.m_Manager.Dht.Contacts.Count) + "'>Contacts (Peers)</th>");
            this.Output("       <td colspan='2'><strong>" + this.m_Manager.Dht.Contacts.Count + " total</strong></td>");
            this.Output("   </tr>");
            foreach (Contact c in this.m_Manager.Dht.Contacts)
            {
                this.Output("   <tr>");
                this.Output("       <td>" + c.EndPoint + "</td>");
                this.Output("       <td>" + c.Identifier + "</td>");
                this.Output("   </tr>");
            }
            this.Output("   <tr>");
            this.Output("       <th width='300' rowspan='" + (1 + this.m_Manager.Mappings.Domains.Count * 2) + "'>Domains (Mappings)</th>");
            this.Output("       <td colspan='2'><strong>" + this.m_Manager.Mappings.Domains.Count + " total</strong></td>");
            this.Output("   </tr>");
            foreach (DomainMap dm in this.m_Manager.Mappings.Domains)
            {
                string q = dm.Question.ToString();
                string a = dm.Answer.ToString();
                if (q.Length > 80) q = q.Substring(0, 80) + "...";
                if (a.Length > 80) a = a.Substring(0, 80) + "...";
                this.Output("   <tr>");
                this.Output("       <td>&nbsp;</td>");
                this.Output("       <td>" + q + "</td>");
                this.Output("   </tr>");
                this.Output("   <tr>");
                this.Output("       <td> -&gt; </td>");
                this.Output("       <td>" + a + "</td>");
                this.Output("   </tr>");
            }
            this.Output("</table>");
        }

        protected override void OnPageExit()
        {
        }
    }
}
