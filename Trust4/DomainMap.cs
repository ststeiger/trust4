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
            get { return this.p_Question.Name; }
        }

        public IPAddress ATarget
        {
            get
            {
                if (this.p_Answer is ARecord)
                    return (this.p_Answer as ARecord).Address;
                else
                    return IPAddress.None;
            }
        }

        public string CNAMETarget
        {
            get
            {
                if (this.p_Answer is CNameRecord)
                    return (this.p_Answer as CNameRecord).CanonicalName;
                else
                    return null;
            }
        }

        public RecordType Type
        {
            get { return this.p_Question.RecordType; }
        }

        public DnsRecordBase Answer
        {
            get { return this.p_Answer; }
        }

        public DnsQuestion Question
        {
            get { return this.p_Question; }
        }
    }
}
