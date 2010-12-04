using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;

namespace Trust4.DataStorage
{
    public struct DataResult
    {
        public Contact Source;

        public byte[] Data;

        public bool Authoritative;
    }
}
