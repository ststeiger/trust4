using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trust4;
using System.Net;
using Data4;

namespace Admin4.Pages
{
    class ControlPage : Page
    {
        public ControlPage(Manager manager)
            : base(manager, new List<string> { "control" })
        {
        }

        protected override bool OnPageInit()
        {
            // Ensure we have the correct amount of parameters.
            if (this.Parameters.Count != 1 || this.Manager.Settings.Initializing)
            {
                Dht.LogS(Dht.LogType.ERROR, "Invalid request to /control!");
                this.Response.Status = HttpStatusCode.Redirect;
                this.Response.AddHeader("Location", "/");
                return true;
            }

            if (this.Parameters[1] == "online")
            {
                // Go online.
                if (!this.Manager.Settings.Online)
                {
                    // Initalize the DNS service.
                    if (!this.Manager.InitalizeDNS())
                    {
                        this.Response.Status = HttpStatusCode.Redirect;
                        this.Response.AddHeader("Location", "/");
                        return true;
                    }

                    // Couldn't lower permissions from root; exit immediately.
                    // Initalize the DHT service.
                    if (!this.Manager.InitalizeDHT())
                    {
                        this.Response.Status = HttpStatusCode.Redirect;
                        this.Response.AddHeader("Location", "/");
                        return true;
                    }

                    // Initalize the contacts.
                    this.Manager.InitalizeDomains();

                    // Now go online.
                    this.Manager.Settings.Online = true;
                }
            }
            else if (this.Parameters[1] == "offline")
            {
                // Go offline.
                if (this.Manager.Settings.Online)
                {
                    // Close the DHT.
                    this.Manager.ShutdownDHT();

                    // Now go online.
                    this.Manager.Settings.Online = false;
                }
            }

            this.Response.Status = HttpStatusCode.Redirect;
            this.Response.AddHeader("Location", "/");
            return true;
        }

        protected override void OnPageHead()
        {
            this.Output("<title>Trust4 Administration Panel - Control</title>");
        }

        protected override void OnPageBody()
        {
            // Show the "automatic configuration in progress" page.
            this.Output("<h2>Trust4 Control</h2>");
            this.Output("<p><a href='/'>Back to overview.</a></p>");
        }

        protected override void OnPageExit()
        {
        }
    }
}
