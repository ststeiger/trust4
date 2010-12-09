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

namespace Admin4
{
    public abstract class Page : HttpServer.HttpModules.HttpModule
    {
        private List<string> m_Names = new List<string>();
        private Dictionary<int, string> p_Params = null;
        private HttpServer.IHttpRequest p_Request = null;
        private HttpServer.IHttpResponse p_Response = null;

        internal Page(string name)
        {
            this.m_Names.Add(name);
        }

        internal Page(List<string> names)
        {
            this.m_Names = names;
        }

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
            this.OnPageInit();
            this.p_Response.Body.Write(Page.m_PreHead, 0, Page.m_PreHead.Length);
            this.OnPageHead();
            this.p_Response.Body.Write(Page.m_PostHead, 0, Page.m_PostHead.Length);
            this.Output("<h1>&nbsp;</h1>");
            this.Output("<div id='status'>");
            this.Output("You are online.<br/>");
            this.Output("Your node is not public.<br/>");
            this.Output("You have 6 peers and 2 domains.<br/>");
            this.Output("The direct peer state is healthy.<br/>");
            this.Output("</div>");
            this.Output("<div id='menubar'>&nbsp;</div>");
            this.Output("<div id='content'>");
            try
            {
                this.OnPageBody();
            }
            catch (Exception e)
            {
                this.Output(e.ToString());
            }
            this.Output("</div>");
            this.Output("<div id='footer'>Trust4 is licensed under the Apache License, Version 2.0.  See <a href='http://code.google.com/p/trust4/'>http://code.google.com/p/trust4/</a> for more information and source code.</div>");
            this.p_Response.Body.Write(Page.m_PostBody, 0, Page.m_PostBody.Length);
            this.OnPageExit();

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

        protected abstract void OnPageInit();
        protected abstract void OnPageHead();
        protected abstract void OnPageBody();
        protected abstract void OnPageExit();
    }
}
