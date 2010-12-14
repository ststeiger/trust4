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
using System.IO;

namespace Admin4.Pages
{
    public class DomainsPage : Page
    {
        public DomainsPage(Manager manager)
            : base(manager, new List<string> { "domains" })
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

            if (this.Parameters.Count == 1 && this.Parameters[1] == "save" && this.Manager.Dht != null)
            {
                // Attempt to add the peer.
                // TODO: Catch exceptions.
                string mappings = this.Request.Form["mappings"].Value.Trim();

                // Save mappings back to the file.
                using (StreamWriter writer = new StreamWriter(this.Manager.Mappings.Path, false))
                {
                    writer.WriteLine(mappings);
                }

                // Reload the domains.
                this.Manager.Mappings.Load();
            }

            return false;
        }

        protected override void OnPageHead()
        {
            this.Output("<title>Trust4 Administration Panel - Domain Management</title>");
        }

        protected override void OnPageBody()
        {
            // Peer management
            this.Output("<h2>Domain Mappings</h2>");
            this.Output("<form action='/domains/save' method='POST'>");
            this.Output("   <table cellpadding='0' border='0' width='100%'>");
            this.Output("       <tr>");
            this.Output("           <td>");
            this.Output("               <textarea name='mappings' style='height: 600px; font-family: \"Courier New\", Courier; color: #000; font-weight: normal;'>");
            using (StreamReader reader = new StreamReader(this.Manager.Mappings.Path))
            {
                this.Output(reader.ReadToEnd().Replace("\t", "        "));
            }
            this.Output("</textarea>");
            this.Output("           </td>");
            this.Output("           <td width='171' valign='top'>");
            this.Output("               <div style='width: auto;' align='right' class='buttons'><button class='positive' type='submit' style='margin-right: 0px; margin-left: 7px;'>Save Domain Mappings</button></div>");
            this.Output("           </td>");
            this.Output("       </tr>");
            this.Output("   </table>");
            this.Output("</form>");
        }

        protected override void OnPageExit()
        {
        }
    }
}
