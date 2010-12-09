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
using System.Runtime.Serialization;

namespace Data4
{
    [Serializable()]
    public class Entry : ISerializable
    {
        private Contact p_Owner;
        private ID p_Key;
        private string p_Value;
        // TODO: Add expiry and ownership properties.

        public Entry(Contact owner, ID key, string value)
        {
            this.p_Owner = owner;
            this.p_Key = key;
            this.p_Value = value;
        }

        public Entry(SerializationInfo info, StreamingContext context)
        {
            this.p_Key = info.GetValue("key", typeof(ID)) as ID;
            this.p_Value = info.GetString("value");
        }

        public Contact Owner
        {
            get { return this.p_Owner; }
            set
            {
                if (this.p_Owner == null)
                    this.p_Owner = value;
                else
                    throw new ArgumentException("Can not change owner of Entry once assigned.");
            }
        }

        public ID Key
        {
            get { return this.p_Key; }
        }

        public string Value
        {
            get { return this.p_Value; }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("key", this.p_Key, typeof(ID));
            info.AddValue("value", this.p_Value);
        }
    }
}

