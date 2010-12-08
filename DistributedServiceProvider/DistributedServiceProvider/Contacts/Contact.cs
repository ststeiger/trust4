using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using DistributedServiceProvider.Base;
using DistributedServiceProvider.Contacts;
using System.Threading;

namespace DistributedServiceProvider.Contacts
{
    /// <summary>
    /// A contact point for a remote routing table
    /// </summary>
    [ProtoContract, ProtoInclude(3, typeof(LocalContact))]
    public abstract class Contact
        :Extensible
    {
        /// <summary>
        /// The identifier of the remote routing table
        /// </summary>
        [ProtoMember(1)]
        public readonly Identifier512 Identifier;

        [ProtoMember(2)]
        private byte[] networkIdBytes;
        /// <summary>
        /// The id of the network which the routing table oeprates on
        /// </summary>
        public Guid NetworkId
        {
            get
            {
                return new Guid(networkIdBytes);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Contact"/> is trusted.
        /// If set to false, this contact will be removed from contact buckets and will never be returned in queries
        /// </summary>
        /// <value><c>true</c> if trusted; otherwise, <c>false</c>.</value>
        public bool Trusted
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Contact"/> class.
        /// </summary>
        /// <param name="identifier">The identifier of the DistributedRoutingTable this contact represents</param>
        /// <param name="networkId">The network id.</param>
        public Contact(Identifier512 identifier, Guid networkId)
        {
            Identifier = identifier;
            networkIdBytes = networkId.ToByteArray();

            Trusted = true; //default to true
        }

        protected Contact()
        {

        }

        /// <summary>
        /// Sends a message to the consumer with the given Id
        /// </summary>
        /// <param name="consumerId">The consumer id.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response fromthe remote consumer, or null if there was no response</returns>
        public abstract void Send(Contact source, Guid consumerId, byte[] message, bool reliable = true, bool ordered = true, int channel = 1);

        /// <summary>
        /// Pings this instance.
        /// </summary>
        /// <returns>The response time, or Timespan.MaxValue if it timed out</returns>
        public abstract TimeSpan Ping(Contact source, TimeSpan timeout);
    }
}
