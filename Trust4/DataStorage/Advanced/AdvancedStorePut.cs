using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using Trust4.Authentication;
using DistributedServiceProvider.Contacts;
using System.IO;
using DistributedServiceProvider.Base;

namespace Trust4.DataStorage.Advanced
{
    public partial class AdvancedStore<K, V>
        : MessageConsumer, IMergingDataStore<K, V>
    {
        private readonly byte remotePutFlag;

        public int Put(K key, V value, Func<V, Conflict<V>, V> merge, Pseudonym authentication)
        {
            //Get remote versions
            //ignore ones which do not authenticate correctly
            //merge each remote version with the local version
            //put the final merged version into the appropriate nodes
            throw new NotImplementedException();
        }

        private void ProcessRemotePut(Contact source, MemoryStream m)
        {
            throw new NotImplementedException();
        }
    }
}
