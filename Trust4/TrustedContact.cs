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
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;

namespace Trust4
{
    public class TrustedContact : DistributedServiceProvider.Contacts.UdpContact
    {
        private decimal p_TrustAmount = 0;
        public decimal TrustAmount
        {
            get { return this.p_TrustAmount; }
        }

        public TrustedContact(decimal trust, Identifier512 id, Guid network, IPAddress ip, int port)
            :base(id, network, ip, port)
        {
            this.p_TrustAmount = trust;
        }

        public override void Send(Contact source, Guid consumerId, byte[] message, bool reliable, bool ordered, int channel)
        {
            base.Send(source, consumerId, message, reliable, ordered, channel);
        }
    }
}
