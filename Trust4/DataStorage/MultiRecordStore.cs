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
using DistributedServiceProvider.Base;

namespace Trust4.DataStorage
{
    public class MultiRecordStore : BasicStore
    {
        public MultiRecordStore(Guid consumerid) : base(consumerid)
        {
        }

        public override IEnumerable<DataResult> Get(Identifier512 key)
        {
            IEnumerable<DataResult> original = base.Get(key);
            List<DataResult> split = new List<DataResult>();

            // Now split the results into multiple records based on the values.
            foreach (DataResult o in original)
            {
                int i = 0;
                List<byte> buf = new List<byte>();
                while (i < o.Data.Length)
                {
                    byte b = o.Data[i];
                    if (b != 0)
                        buf.Add(b);
                    else
                    {
                        // Reached NUL seperator.
                        DataResult r = new DataResult();
                        r.Authoritative = o.Authoritative;
                        r.Data = buf.ToArray();
                        r.Source = o.Source;
                        split.Add(r);
                        buf.Clear();
                    }
                }

                if (buf.Count > 0)
                {
                    // Reached end-of-value.
                    DataResult r = new DataResult();
                    r.Authoritative = o.Authoritative;
                    r.Data = buf.ToArray();
                    r.Source = o.Source;
                    split.Add(r);
                    buf.Clear();
                }
            }

            return split;
        }

        public override void Put(Identifier512 key, byte[] value)
        {
            try
            {
                base.Put(key, value);
            }
            catch (KeyCollisionException)
            {
                // Find the original value.
                IEnumerable<DataResult> original = base.Get(key);

                foreach (DataResult o in original)
                {
                    // Only get the value from ourselves.
                    if (o.Source.Identifier == this.RoutingTable.LocalIdentifier)
                    {
                        List<byte> m = new List<byte>(o.Data);
                        m.Add(0);
                        m.AddRange(value);

                        // Now try and add the key.
                        try
                        {
                            base.Delete(key);
                            base.Put(key, m.ToArray());
                            return;
                        }
                        catch (KeyCollisionException)
                        {
                            throw;
                        }
                    }
                }

                throw;
            }
        }
    }
}

