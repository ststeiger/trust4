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
using System.Net;
using System.Text;
using ARSoft.Tools.Net.Dns;

namespace Trust4
{
    public static class DnsSerializer
    {
        public static string ToStore(DnsQuestion question)
        {
            switch (question.RecordType)
            {
                case RecordType.A:
                    return "DNS!" + question.Name.ToLowerInvariant();
                case RecordType.CName:
                    return "DNS!" + question.Name.ToLowerInvariant();
                case RecordType.Mx:
                    return "DNS!" + question.Name.ToLowerInvariant();
                case RecordType.Ns:
                    return "DNS!" + question.Name.ToLowerInvariant();
                default:
                    throw new NotSupportedException("The specified DNS question is not supported by the serializer.");
            }
        }

        public static string ToStore(DnsRecordBase answer)
        {
            if (answer is ARecord)
                return "DNS!A!" + ( answer as ARecord ).Address.ToString();
            else if (answer is CNameRecord)
                return "DNS!CNAME!" + ( answer as CNameRecord ).CanonicalName.ToLowerInvariant();
            else if (answer is MxRecord)
                return "DNS!MX!" + ( answer as MxRecord ).Preference.ToString() + "!" + ( answer as MxRecord ).ExchangeDomainName.ToLowerInvariant();
            else if (answer is NsRecord)
                return "DNS!NS!" + ( answer as NsRecord ).NameServer.ToLowerInvariant();
            else
                throw new NotSupportedException("The specified DNS answer type is not supported by the serializer.");
        }

        public static DnsRecordBase FromStore(string domain, byte[] data)
        {
            string[] split = ByteString.GetString(data).Split(new char[] { '!' });
            if (split[0] != "DNS")
                return null;
            
            switch (split[1])
            {
                case "A":
                    // Only one field for this..
                    IPAddress ip;
                    if (IPAddress.TryParse(split[2], out ip))
                        return new ARecord(domain, 3600, ip);
                    else
                        return null;
                case "CNAME":
                    // Only one field for this..
                    return new CNameRecord(domain, 3600, split[2].ToLowerInvariant());
                case "MX":
                    // Grab the priority and domain.
                    return new MxRecord(domain, 3600, Convert.ToUInt16(split[2].ToLowerInvariant()), split[3].ToLowerInvariant());
                case "NS":
                    // Only one field for this..
                    return new NsRecord(domain, 3600, split[2].ToLowerInvariant());
                default:
                    return null;
            }
        }
    }
}

