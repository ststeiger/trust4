using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trust4;
using System.Net;
using Data4;

namespace Admin4.Pages
{
    class AutomaticConfigurationPage : Page
    {
        public static readonly Dictionary<ID, IPEndPoint> m_TrustCaches = new Dictionary<ID, IPEndPoint>
        {
            {
                new ID(
                    new Guid("58f7ec2e-60e8-c124-86ef-a5f1f050f8e5"),
                    new Guid("1e1928da-eaab-9b27-85eb-eb0dfea4fbc7"),
                    new Guid("34b01e5d-b257-9d73-923f-f3f33adb7f26"),
                    new Guid("c12af3be-6110-f143-e0e1-26171ab5535d")
                    ),  
                new IPEndPoint(IPAddress.Parse("74.207.247.199"), 12000)
            }
        };

        public AutomaticConfigurationPage(Manager manager)
            : base(manager, new List<string> { "", "autoconfig" })
        {
        }

        protected override bool OnPageInit()
        {
            if (this.Parameters.Count == 2 && this.Parameters[1] == "ajax")
            {
                switch (this.Parameters[2])
                {
                    case "address":
                        try
                        {
                            IPAddress ip = this.Manager.Settings.LoadDynamicIp();
                            this.Manager.Settings.LocalIP = ip;
                            this.Output("success");
                        }
                        catch (Exception e)
                        {
                            this.Output("failed");
                            Console.WriteLine(e);
                        }
                        return true;
                    case "ports":
                        try
                        {
                            this.Manager.Settings.P2PPort = 12000;
                            this.Manager.Settings.DNSPort = 12000;
                            this.Output("success");
                        }
                        catch (Exception e)
                        {
                            this.Output("failed");
                            Console.WriteLine(e);
                        }
                        return true;
                    case "routing":
                        try
                        {
                            this.Manager.Settings.RoutingIdentifier = new Data4.ID(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
                            this.Output("success");
                        }
                        catch (Exception e)
                        {
                            this.Output("failed");
                            Console.WriteLine(e);
                        }
                        return true;
                    case "start":
                        this.Output("failed");
                        return true;
                    case "peers":
                        try
                        {
                            this.Manager.Dht.Contacts.Clear();
                            foreach (KeyValuePair<ID, IPEndPoint> kv in AutomaticConfigurationPage.m_TrustCaches)
                                this.Manager.Dht.Contacts.Add(new Contact(kv.Key, kv.Value));
                            this.Output("success");
                        }
                        catch (Exception e)
                        {
                            this.Output("failed");
                            Console.WriteLine(e);
                        }
                        return true;
                }
            }

            return false;
        }

        protected override void OnPageHead()
        {
            this.Output("<title>Trust4 Administration Panel - Automatic Configuration</title>");
        }

        protected override void OnPageBody()
        {
            // Show the "automatic configuration in progress" page.
            this.Output("<h2>Automatic Configuration</h2>");
            this.Output("<p id='confstatus'>Trust4 is now automatically configuring your node...</p>");

            this.Output("<ul class='progress'>");
            this.Output("   <li id='address' class='waiting'>Detect IP address</li>");
            this.Output("   <li id='ports' class='waiting'>Configure ports</li>");
            this.Output("   <li id='routing' class='waiting'>Generate routing identifier</li>");
            this.Output("   <li id='start' class='waiting'>Start node</li>");
            this.Output("   <li id='peers' class='waiting'>Seed initial list of peers</li>");
            this.Output("</ul>");

            this.Output("<script type='text/javascript' src='/static/javascript.js'></script>");
            this.Output("<script type='text/javascript'>");
            this.Output("   StartAutoConfig();");
            this.Output("</script>");
        }

        protected override void OnPageExit()
        {
        }
    }
}
