using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using System.Net;

namespace Trust4
{
    public class TrustedContact : UdpContact
    {
        private decimal p_TrustAmount = 0;

        public TrustedContact(decimal trust, Identifier512 id, Guid network, IPAddress ip, int port)
            : base(id, network, ip, port)
        {
            this.p_TrustAmount = trust;
        }

        public decimal TrustAmount
        {
            get
            {
                return this.p_TrustAmount;
            }
        }
    }
}
