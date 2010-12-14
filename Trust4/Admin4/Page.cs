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
using System.IO;
using Trust4;

namespace Admin4
{
    public abstract class Page : HttpServer.HttpModules.HttpModule
    {
        private Manager p_Manager = null;
        private List<string> m_Names = new List<string>();
        private Dictionary<int, string> p_Params = null;
        private HttpServer.IHttpRequest p_Request = null;
        private HttpServer.IHttpResponse p_Response = null;

        private static readonly byte[] m_PreHead = Encoding.ASCII.GetBytes(
"<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">" + "\n" +
"<html xmlns='http://www.w3.org/1999/xhtml'>" + "\n" +
"<head>" + "\n" +
"   <link rel='stylesheet' type='text/css' href='/static/stylesheet.css' />" + "\n");
        private static readonly byte[] m_PostHead = Encoding.ASCII.GetBytes(
"</head>" + "\n" +
"<body>" + "\n");
        private static readonly byte[] m_PostBody = Encoding.ASCII.GetBytes(
"</body>" + "\n" +
"</html>" + "\n");

        internal Page(Manager manager, string name)
        {
            this.p_Manager = manager;
            this.m_Names.Add(name);
        }

        internal Page(Manager manager, List<string> names)
        {
            this.p_Manager = manager;
            this.m_Names = names;
        }

        protected Manager Manager
        {
            get { return this.p_Manager; }
        }

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            // Set up the parameters.
            this.p_Params = new Dictionary<int, string>();
            int i = 1;
            bool skip = true;
            foreach (string s in request.UriParts)
            {
                if (!skip)
                {
                    this.p_Params.Add(i, s);
                    i += 1;
                }
                else
                    skip = false;
            }

            bool matched = false;
            foreach (string s in this.m_Names)
            {
                if ((request.UriParts.Length == 0 && s == string.Empty) ||
                    (request.UriParts[0].ToLowerInvariant() == s.ToLowerInvariant()))
                {
                    matched = true;
                    break;
                }
            }
            if (!matched)
                return false;

            // Set the request / response properties.
            this.p_Request = request;
            this.p_Response = response;

            // Fire the events and add data as needed.
            try
            {
                if (this.OnPageInit())
                    return true;
                this.p_Response.Body.Write(Page.m_PreHead, 0, Page.m_PreHead.Length);
                this.OnPageHead();
                this.p_Response.Body.Write(Page.m_PostHead, 0, Page.m_PostHead.Length);
                this.Output("<h1>&nbsp;</h1>");
                this.Output("<div id='status'>");
                if (this.p_Manager.Online)
                    this.Output("You are <strong style='color: #060;'>online</strong>.<br/>");
                else
                    this.Output("You are <strong style='color: #F00;'>offline</strong>.<br/>");
                if (this.p_Manager.Online)
                {
                    if (this.p_Manager.Settings.Public)
                        this.Output("Your node is <strong>public</strong>.<br/>");
                    else
                        this.Output("Your node is <strong>not public</strong>.<br/>");
                    if (this.Manager.Dht != null)
                    {
                        this.Output("You have <strong>" + this.Manager.Dht.Contacts.Count +
                            " peer" + ((this.Manager.Dht.Contacts.Count == 1) ? "" : "s") + "</strong> and <strong>" + this.Manager.Mappings.Domains.Count +
                            " domain " + ((this.Manager.Mappings.Domains.Count == 1) ? "entry" : "entries") + "</strong>.<br/>");
                    }
                }
                else if (!this.p_Manager.Settings.Configured)
                    this.Output("Your node is <strong>not configured</strong>.<br/>");
                this.Output("</div>");
                this.Output("<div id='menubar'>");
                this.Output("   <a href='/'" + ((this is Pages.OverviewPage) ? " class='selected'" : "") + ">OVERVIEW</a>");
                this.Output("   <a href='/peers'" + ((this is Pages.PeersPage) ? " class='selected'" : "") + ">PEERS</a>");
                this.Output("   <a href='/domains'" + ((this is Pages.DomainsPage) ? " class='selected'" : "") + ">DOMAINS</a>");
                this.Output("</div>");
                this.Output("<div id='content'>");
                this.OnPageBody();
                this.Output("</div>");
                this.Output("<div id='footer'>Trust4 is licensed under the Apache License, Version 2.0.  See <a href='http://code.google.com/p/trust4/'>http://code.google.com/p/trust4/</a> for more information and source code.</div>");
                this.p_Response.Body.Write(Page.m_PostBody, 0, Page.m_PostBody.Length);
                this.OnPageExit();
            }
            catch (Exception e)
            {
                this.Output(e.ToString());
            }

            return true;
        }

        protected Dictionary<int, string> Parameters
        {
            get { return this.p_Params; }
        }

        protected HttpServer.IHttpRequest Request
        {
            get { return this.p_Request; }
        }

        protected HttpServer.IHttpResponse Response
        {
            get { return this.p_Response; }
        }

        protected void Output(string html)
        {
            byte[] r = Encoding.ASCII.GetBytes(html);
            this.p_Response.Body.Write(r, 0, r.Length);
        }

        protected abstract bool OnPageInit();
        protected abstract void OnPageHead();
        protected abstract void OnPageBody();
        protected abstract void OnPageExit();
    }
}
