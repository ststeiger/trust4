using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.MessageConsumers;
using Trust4.DataStorage;

namespace DistributedServiceProvider.Stores
{
    /// <summary>
    /// A data store which stores no data and returns no results.
    /// </summary>
    public class NullDataStore : MessageConsumer, IDataStore
    {
        public NullDataStore(Guid consumerid)
            : base(consumerid)
        {
        }

        /// <summary>
        /// Puts a key-value pair into the local data store that's participating in the DHT.
        /// </summary>
        /// <param name="key">The unique key.</param>
        /// <param name="value">The value.</param>
        public void Put(Identifier512 key, byte[] value)
        {
        }

        /// <summary>
        /// Deletes a key-value pair from the local data store that's participating in the DHT.
        /// </summary>
        /// <param name="key">The unique key.</param>
        public void Delete(Identifier512 key)
        {
        }

        /// <summary>
        /// Returns a list of values retrieved from all of the connected peers for the selected
        /// key request.
        /// </summary>
        /// <param name="key">The unique key.</param>
        /// <param name="timeout">The timeout value for each peer.</param>
        public IEnumerable<DataResult> Get(Identifier512 key)
        {
            return new List<DataResult>();
        }

        /// <summary>
        /// Delivers the specified message.
        /// </summary>
        /// <param name="source">The source contact.</param>
        /// <param name="message">The message.</param>
        public override void Deliver(Contact source, byte[] message)
        {
        }
    }
}
