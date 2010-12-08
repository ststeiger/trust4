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
using System.Linq;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Data4
{
    [Serializable()]
    public class ID : ISerializable
    {
        private byte[] m_Bytes;

        public ID(Guid a, Guid b, Guid c, Guid d)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(a.ToByteArray());
            bytes.AddRange(b.ToByteArray());
            bytes.AddRange(c.ToByteArray());
            bytes.AddRange(d.ToByteArray());
            this.m_Bytes = bytes.ToArray();
        }

        public ID(SerializationInfo info, StreamingContext context)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < 64; i += 1)
                bytes.Add(info.GetByte("k" + i.ToString()));
            this.m_Bytes = bytes.ToArray();
        }

        public static ID NewRandom()
        {
            return new ID(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        }

        public static bool operator ==(ID a, ID b)
        {
            if (object.ReferenceEquals(a, b))
                return true;
            if (object.ReferenceEquals(a, null))
                return false;
            if (object.ReferenceEquals(b, null))
                return false;

            if (a.m_Bytes.Length != b.m_Bytes.Length)
                return false;

            bool same = true;
            for (int i = 0; i < a.m_Bytes.Length; i += 1)
            {
                if (a.m_Bytes[i] != b.m_Bytes[i])
                {
                    same = false;
                    break;
                }
            }

            return same;
        }

        public static bool operator !=(ID a, ID b)
        {
            if (object.ReferenceEquals(a, b))
                return false;
            if (object.ReferenceEquals(a, null))
                return true;
            if (object.ReferenceEquals(b, null))
                return true;

            if (a.m_Bytes.Length != b.m_Bytes.Length)
                return true;

            bool same = true;
            for (int i = 0; i < a.m_Bytes.Length; i += 1)
            {
                if (a.m_Bytes[i] != b.m_Bytes[i])
                {
                    same = false;
                    break;
                }
            }

            return !same;
        }

        public override string ToString()
        {
            if (this.m_Bytes == null)
                return new Guid().ToString() + " " + new Guid().ToString() + " " + new Guid().ToString() + " " + new Guid().ToString();

            return new Guid(this.m_Bytes.Skip(0).Take(16).ToArray()).ToString() + " " +
                new Guid(this.m_Bytes.Skip(16).Take(16).ToArray()).ToString() + " " +
                new Guid(this.m_Bytes.Skip(32).Take(16).ToArray()).ToString() + " " +
                new Guid(this.m_Bytes.Skip(48).Take(16).ToArray()).ToString();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            for (int i = 0; i < 64; i += 1)
                info.AddValue("k" + i.ToString(), this.m_Bytes[i]);
        }
    }
}

