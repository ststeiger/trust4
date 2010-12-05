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

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.IO;

namespace Trust4
{
    public static class Extensions
    {
        public static IEnumerable<string> OmitComments(this IEnumerable<string> strings, params string[] commentCharacters)
        {
            foreach (var line in strings.Where(l => l.Length > 0))
            {
                int i = int.MaxValue;
                foreach (var commentMarker in commentCharacters)
                {
                    int index = line.IndexOf(commentMarker);
                    i = index == -1 ? i : index;
                }
                
                if (i == int.MaxValue || i == -1)
                    yield return line;
                else if (i > 0)
                    yield return line.Substring(0, i);
            }
        }

        public static byte[] EncryptLargeData(this RSACryptoServiceProvider rsa, byte[] data, bool fOAEP)
        {
            SymmetricAlgorithm symmetric = RijndaelManaged.Create();
            symmetric.GenerateIV();
            symmetric.GenerateKey();

            var encryptedKey = rsa.Encrypt(symmetric.Key, fOAEP);
            var encryptedIv = rsa.Encrypt(symmetric.IV, fOAEP);

            using (MemoryStream m = new MemoryStream())
            {
                BinaryWriter w = new BinaryWriter(m);
                w.Write(encryptedKey.Length);
                w.Write(encryptedKey);

                w.Write(encryptedIv.Length);
                w.Write(encryptedIv);

                using (var cryptStream = new CryptoStream(m, symmetric.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    using (BinaryWriter wc = new BinaryWriter(cryptStream))
                    {
                        wc.Write(data.Length);
                        wc.Write(data);
                    }
                }

                return m.ToArray();
            }
        }

        public static byte[] DecryptLargeData(this RSACryptoServiceProvider rsa, byte[] encryptedData, bool fOAEP)
        {
            SymmetricAlgorithm symmetric = RijndaelManaged.Create();

            using (MemoryStream m = new MemoryStream(encryptedData))
            {
                BinaryReader r = new BinaryReader(m);
                symmetric.Key = rsa.Decrypt(r.ReadBytes(r.ReadInt32()), fOAEP);
                symmetric.IV = rsa.Decrypt(r.ReadBytes(r.ReadInt32()), fOAEP);

                using (CryptoStream cryptStream = new CryptoStream(m, symmetric.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (BinaryReader rc = new BinaryReader(cryptStream))
                    {
                        return rc.ReadBytes(rc.ReadInt32());
                    }
                }
            }
        }
    }
}
