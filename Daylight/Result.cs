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
namespace Daylight
{
    public class Result
    {
        Contact p_Contact = null;
        string p_Data = null;

        public Result(Contact contact, string data)
        {
            this.p_Contact = contact;
            this.p_Data = data;
        }

        public Contact Contact
        {
            get
            {
                return this.p_Contact;
            }
        }

        public string Data
        {
            get
            {
                return this.p_Data;
            }
        }
    }
}

