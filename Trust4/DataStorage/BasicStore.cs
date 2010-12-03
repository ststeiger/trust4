using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using System.Threading;
using System.Collections.Concurrent;
using DistributedServiceProvider.Base;
using System.IO;
using ProtoBuf;
using DistributedServiceProvider.Contacts;

namespace Trust4.DataStorage
{
    public class BasicStore
        : MessageConsumer, IDataStore
    {
        #region fields
        [LinkedConsumer(Callback.GUID_STRING)]
        Callback callback;

        [LinkedConsumer(Callback.GUID_STRING)]
        GetClosestNodes getClosest;

        private ConcurrentDictionary<Identifier512, byte[]> localCache = new ConcurrentDictionary<Identifier512, byte[]>();
        private ConcurrentDictionary<Identifier512, byte[]> localAuthoritativeData = new ConcurrentDictionary<Identifier512, byte[]>();
        #endregion

        #region constructors
        public BasicStore(Guid consumerId)
            :base(consumerId)
        {

        }
        #endregion

        public IEnumerable<DataResult> Get(Identifier512 key)
        {
            throw new NotImplementedException();
        }

        public override void Deliver(Contact source, byte[] message)
        {
            using (MemoryStream m = new MemoryStream(message))
            {
                PacketFlag flag = (PacketFlag)m.ReadByte();

                switch (flag)
                {
                    case PacketFlag.PutRequest:
                        HandleRemotePut(source, m);
                        break;
                    default:
                        break;
                }
            }
        }

        private enum PacketFlag
        {
            PutRequest,
        }

        #region put
        private void HandleRemotePut(Contact source, MemoryStream m)
        {
            var p = Serializer.Deserialize<PutRequest>(m);

            PutResponse.Response responseCode = PutResponse.Response.Success;
            try
            {
                Put(p.Key, p.Data);
            }
            catch (KeyCollisionException)
            {
                responseCode = PutResponse.Response.DuplicateKey;
            }

            PutResponse response = new PutResponse() { ResponseCode = responseCode };

            using (MemoryStream mResponse = new MemoryStream())
            {
                Serializer.Serialize<PutResponse>(mResponse, response);

                callback.SendResponse(RoutingTable.LocalContact, source, p.CallbackId, m.ToArray());
            }
        }

        /// <summary>
        /// Puts the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="KeyCollisionException">Thrown if you try to put an already existing value</exception>
        /// <exception cref="KeyNotFoundException">Thrown if you try to put null into a non existant key</exception>
        public void Put(DistributedServiceProvider.Base.Identifier512 key, byte[] value)
        {
            if (value == null)
            {
                byte[] removed;
                localCache.TryRemove(key, out removed);
            }
            else
                localCache.AddOrUpdate(key, value, (a, b) => value);

            var closest = getClosest.GetClosestContacts(key, null).GetEnumerator();
            while (closest.MoveNext())
            {
                if (closest.Current.Identifier == RoutingTable.LocalIdentifier)
                {
                    if (value == null)
                    {
                        byte[] removed;
                        if (!localAuthoritativeData.TryRemove(key, out removed))
                            throw new KeyNotFoundException("No such key in local authoritative data to delete");
                    }
                    else
                    {
                        localAuthoritativeData.AddOrUpdate(key, value,
                            (a, b) => { throw new KeyCollisionException("Key " + key + " already exists in local authoritative data"); }
                        );
                    }
                }
                else
                {
                    using (MemoryStream mStream = new MemoryStream())
                    {
                        mStream.WriteByte((byte)PacketFlag.PutRequest);

                        var token = callback.AllocateToken();

                        try
                        {
                            Serializer.Serialize<PutRequest>(mStream, new PutRequest(key, value, token.Id));

                            closest.Current.Send(RoutingTable.LocalContact, ConsumerId, mStream.ToArray());

                            if (!token.Wait(RoutingTable.Configuration.LookupTimeout))
                                continue;

                            using (MemoryStream m = new MemoryStream(token.Response))
                            {
                                var r = Serializer.Deserialize<PutResponse>(m);
                                switch (r.ResponseCode)
                                {
                                    case PutResponse.Response.Success:
                                        return;
                                    case PutResponse.Response.DuplicateKey:
                                        throw new KeyCollisionException("Key " + key + " already exists in remote authoritative data");
                                    case PutResponse.Response.KeyNotFound:
                                        throw new KeyNotFoundException("Key " + key + " not found to delete in remote authoritative data");
                                    default:
                                        break;
                                }

                            }
                        }
                        finally
                        {
                            callback.FreeToken(token);
                        }
                    }
                }
            }
        }

        [ProtoContract]
        private class PutRequest
        {
            [ProtoMember(1)]
            public Identifier512 Key;

            [ProtoMember(2)]
            public byte[] Data;

            [ProtoMember(3)]
            public long CallbackId;

            public PutRequest(Identifier512 key, byte[] data, long callbackId)
            {
                Key = key;
                Data = data;
                CallbackId = callbackId;
            }
        }

        [ProtoContract]
        private class PutResponse
        {
            public Response ResponseCode;

            public enum Response
                :byte
            {
                Success,
                DuplicateKey,
                KeyNotFound,
            }
        }
        #endregion

        #region delete
        public void Delete(Identifier512 key)
        {
            Put(key, null);
        }
        #endregion
    }
}
