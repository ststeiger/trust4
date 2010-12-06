using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using Trust4.Authentication;
using DistributedServiceProvider.MessageConsumers;

namespace Trust4.DataStorage.Advanced
{
    /// <summary>
    /// A merging datastore.
    /// This store is built upon the fact that a distributed storage system will *not* provide consistency.
    /// Put operations provide a merge operation to reconcile differing concurrent versions
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public partial class AdvancedStore<K, V>
        :MessageConsumer, IMergingDataStore<K, V>
    {
        readonly Func<byte[], V> deserialise;
        readonly Func<V, byte[]> serialise;
        readonly Func<K, Identifier512> keySelector;

        public AdvancedStore(Guid id, Func<byte[], V> deserialise, Func<V, byte[]> serialise, Func<K, Identifier512> keySelector)
            :base(id)
        {
            this.serialise = serialise;
            this.deserialise = deserialise;
            this.keySelector = keySelector;

            remotePutFlag = RegisterConsumer(ProcessRemotePut);
            remoteGetFlag = RegisterConsumer(ProcessRemoteGet);
            remoteDeleteFlag = RegisterConsumer(ProcessRemoteDelete);
        }

        public Identifier512 CreateKey(K key)
        {
            return keySelector(key);
        }

        public byte[] Serialise(V value)
        {
            return serialise(value);
        }

        public V Deserialise(byte[] value)
        {
            return deserialise(value);
        }

        private IEnumerable<Identifier512> GetReplicas(Identifier512 key)
        {
            while (true)
            {
                key = key.GetHashedKey();
                yield return key;
            }
        }
    }
}
