using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Trust4
{
    public class DomainMap
    {
        private string p_Domain = null;
        private IPAddress p_Target = IPAddress.None;

        public DomainMap(string domain, IPAddress target)
        {
            this.p_Domain = domain;
            this.p_Target = target;
        }

        public string Domain
        {
            get
            {
                return this.p_Domain;
            }
            set
            {
                this.p_Domain = value;
            }
        }

        public IPAddress Target
        {
            get
            {
                return this.p_Target;
            }
            set
            {
                this.p_Target = value;
            }
        }
    }
}
