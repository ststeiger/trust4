using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using DistributedServiceProvider.Contacts;
using System.IO;

namespace Trust4.DataStorage.Advanced
{
    public partial class AdvancedStore<K, V>
        : MessageConsumer, IMergingDataStore<K, V>
    {
        private readonly byte remoteGetFlag;

        public IEnumerable<KeyValuePair<Contact, SignedValue<V>>> Get(K key)
        {
            throw new NotImplementedException();
        }

        private void ProcessRemoteGet(Contact source, MemoryStream m)
        {
            throw new NotImplementedException();
        }
    }
}
