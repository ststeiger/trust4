using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;

namespace Trust4.DataStorage
{
    public interface IDataStore
    {
        void Put(Identifier512 key, byte[] value);

        void Delete(Identifier512 key);

        IEnumerable<DataResult> Get(Identifier512 key);
    }
}
