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
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Trust4
{
    public static class ByteString
    {
        public static string GetString(byte[] bytes)
        {
            string s = "";
            foreach (byte b in bytes)
                s += (char) b;
            return s;
        }

        public static byte[] GetBytes(string data)
        {
            List<byte> bytes = new List<byte>();
            foreach (char c in data)
                bytes.Add((byte) c);
            return bytes.ToArray();
        }

        public static string GetHexString(byte[] bytes)
        {
            string s = "";
            foreach (byte b in bytes)
                s += ( (int) b ).ToString("X2").ToUpper();
            return s;
        }

        public static byte[] GetHexBytes(string hexdata)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < hexdata.Length; i += 2)
            {
                char c1 = hexdata[i];
                char c2 = hexdata[i + 1];

                string cs = c1.ToString() + c2.ToString();
                cs = cs.ToUpper();
                byte bv;
                bool result = byte.TryParse(cs, NumberStyles.HexNumber, null as IFormatProvider, out bv);

                // Check for invalid hexadecimal representation.
                if (!result)
                    return null;
                bytes.Add(bv);
            }
            return bytes.ToArray();
        }

        public static string GetBase32String(byte[] bytes)
        {
            return ( new ZBase32Encoder() ).Encode(bytes);
        }

        public static byte[] GetBase32Bytes(string bdata)
        {
            return ( new ZBase32Encoder() ).Decode(bdata);
        }
    }
}

