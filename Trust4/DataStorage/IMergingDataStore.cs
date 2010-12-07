using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using Trust4.Authentication;

namespace Trust4.DataStorage
{
    public interface IMergingDataStore<K, V>
    {
        /// <summary>
        /// Put some new data into the DHT
        /// </summary>
        /// <param name="key">The key for the data</param>
        /// <param name="value">the value of the data</param>
        /// <param name="merge">An algorithm to merge conflicting values which already exist within the system</param>
        /// <param name="authentication">The pseudonym to use to sign this data. Only someone with the same pseudonym will be able to delete or modify the data</param>
        /// <returns>Returns the count of merges which were run during this put</returns>
        int Put(K key, V value, Func<V, Conflict<V>, V> merge, Pseudonym authentication);

        /// <summary>
        /// Attempts to delete all records with the given key
        /// </summary>
        /// <param name="key"></param>
        void Delete(K key, Pseudonym authentication);

        /// <summary>
        /// Delete data with a specific key stored on a specific peer
        /// </summary>
        /// <param name="key"></param>
        /// <param name="peer"></param>
        void Delete(K key, Contact peer, Pseudonym authentication);

        /// <summary>
        /// Get the replications of this item, paired with the contact which they came from
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IEnumerable<KeyValuePair<Contact, SignedValue<V>>> Get(K key);

        /// <summary>
        /// Create an identifier from a key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Identifier512 CreateKey(K key);

        /// <summary>
        /// Serialise a value into bytes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        byte[] Serialise(V value);

        /// <summary>
        /// Deserialise an array of bytes into a value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        V Deserialise(byte[] value);
    }

    public struct Conflict<V>
    {
        public readonly V ConflictingValue;
        public readonly Contact Source;

        public Conflict(V conflictingValue, Contact source)
        {
            ConflictingValue = conflictingValue;
            Source = source;
        }
    }

    public struct SignedValue<V>
    {
        public readonly V Value;
        public readonly byte[] Signature;

        public SignedValue(V value, byte[] signature)
        {
            Value = value;
            Signature = signature;
        }
    }
}
