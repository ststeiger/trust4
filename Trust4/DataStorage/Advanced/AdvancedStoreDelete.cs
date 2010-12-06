using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using System.IO;
using DistributedServiceProvider.MessageConsumers;
using Trust4.Authentication;

namespace Trust4.DataStorage.Advanced
{
    public partial class AdvancedStore<K, V>
        : MessageConsumer, IMergingDataStore<K, V>
    {
        private readonly byte remoteDeleteFlag;

        public void Delete(K key, Pseudonym authentication)
        {
            throw new NotImplementedException();
        }

        public void Delete(K key, Contact peer, Pseudonym authentication)
        {
            throw new NotImplementedException();
        }

        private void ProcessRemoteDelete(Contact source, MemoryStream m)
        {
            throw new NotImplementedException();
        }
    }
}
