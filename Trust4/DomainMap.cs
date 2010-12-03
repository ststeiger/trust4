using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using ARSoft.Tools.Net.Dns;

namespace Trust4
{
    public class DomainMap
    {
        private string p_Domain = null;
        private IPAddress p_ATarget = IPAddress.None;
        private string p_CNAMETarget = null;
        private RecordType p_Type = RecordType.Null;

        public DomainMap(string domain, IPAddress target)
        {
            this.p_Domain = domain;
            this.p_ATarget = target;
            this.p_Type = RecordType.A;
        }

        public DomainMap(string domain, string target)
        {
            this.p_Domain = domain;
            this.p_CNAMETarget = target;
            this.p_Type = RecordType.CName;
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

        public IPAddress ATarget
        {
            get
            {
                return this.p_ATarget;
            }
            set
            {
                this.p_ATarget = value;
            }
        }

        public string CNAMETarget
        {
            get
            {
                return this.p_CNAMETarget;
            }
            set
            {
                this.p_CNAMETarget = value;
            }
        }

        public RecordType Type
        {
            get
            {
                return this.p_Type;
            }
            set
            {
                this.p_Type = value;
            }
        }
    }
}
