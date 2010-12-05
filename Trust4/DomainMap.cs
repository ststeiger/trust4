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
		private DnsQuestion p_Question = null;
		private DnsRecordBase p_Answer = null;

        public DomainMap(DnsQuestion question, DnsRecordBase answer)
        {
            this.p_Question = question;
            this.p_Answer = answer;
        }

        public string Domain
        {
            get
            {
                return this.p_Question.Name;
            }
        }

        public IPAddress ATarget
        {
            get
            {
                return (this.p_Answer as ARecord).Address;
            }
        }

        public string CNAMETarget
        {
            get
            {
                return (this.p_Answer as CNameRecord).CanonicalName;
            }
        }

        public RecordType Type
        {
            get
            {
                return this.p_Question.RecordType;
            }
        }

        public DnsRecordBase Answer
        {
            get
            {
                return this.p_Answer;
            }
        }
    }
}
