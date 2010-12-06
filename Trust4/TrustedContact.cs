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
using Daylight;

namespace Trust4
{
    public class TrustedContact : Contact
    {
        private decimal p_TrustAmount = 0;

        public TrustedContact(decimal trust, ID id, IPEndPoint endpoint) : base(id, endpoint)
        {
            this.p_TrustAmount = trust;
        }

        public decimal TrustAmount
        {
            get { return this.p_TrustAmount; }
        }
    }
}