using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.MessageConsumers;
using DistributedServiceProvider.Contacts;
using System.Security.Cryptography;
using ProtoBuf;
using System.IO;
using DistributedServiceProvider.Base.Extensions;

namespace Trust4.Authentication
{
    public class Pseudonym
        :MessageConsumer
    {
        private RNGCryptoServiceProvider randomNumberGenerator;
        private RSACryptoServiceProvider cryptoProvider;

        private const bool F_OAP = true;

        [LinkedConsumer(Callback.GUID_STRING)]
        public Callback Callback;

        public Pseudonym(Guid nameId, RSACryptoServiceProvider crypto)
            :base(nameId)
        {
            if (crypto.PublicOnly)
                throw new ArgumentException("Crypto provider must have private key data");

            cryptoProvider = crypto;
            randomNumberGenerator = new RNGCryptoServiceProvider();
        }

        public override void Deliver(Contact source, byte[] message)
        {
            using (MemoryStream m = new MemoryStream(message))
            {
                PacketFlag flag = (PacketFlag)m.ReadByte();
                switch (flag)
                {
                    case PacketFlag.Challenge:
                        HandleRemoteChallenge(source, m);
                        break;
                    case PacketFlag.SecureMessage:
                        HandleRemoteSecureMessage(source, m);
                        break;
                    default:
                        break;
                }
            }
        }

        public class FloodException
            :Exception
        {
            public TimeSpan SuggestedBackoff;

            public FloodException(TimeSpan backoff)
                :base("This node is flooded with requests and suggests you try again in " + backoff)
            {
                SuggestedBackoff = backoff;
            }
        }

        private enum PacketFlag
            :byte
        {
            Challenge,
            SecureMessage,
        }

        #region secure messaging
        public void SendSecureMessage(Contact destination, Guid consumerId, byte[] data, RSACryptoServiceProvider remotePublicKey)
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(m))
                {
                    w.Write((byte)PacketFlag.SecureMessage);

                    var encryptedData = remotePublicKey.EncryptLargeData(data, F_OAP);
                    w.Write(encryptedData.Length);
                    w.Write(encryptedData, 0, encryptedData.Length);

                    var guidBytes = consumerId.ToByteArray();
                    w.Write(guidBytes.Length);
                    w.Write(guidBytes, 0, guidBytes.Length);
                }
            }
        }

        private void HandleRemoteSecureMessage(Contact source, MemoryStream m)
        {
            using (BinaryReader r = new BinaryReader(m))
            {
                int dataLength = r.ReadInt32();
                byte[] decryptedData = cryptoProvider.DecryptLargeData(r.ReadBytes(dataLength), F_OAP);

                int guidLength = r.ReadInt32();
                Guid consumerId = new Guid(r.ReadBytes(guidLength));

                RoutingTable.Deliver(source, consumerId, decryptedData);
            }
        }
        #endregion

        #region challenge
        /// <summary>
        /// Returns true if the given peer has the private key for the given public key
        /// </summary>
        /// <param name="peer">The peer.</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="id">The ID of the psuedonym.</param>
        public bool Challenge(Contact peer, Guid id, RSACryptoServiceProvider publicKey, int size, int timeout)
        {
            byte[] challenge = new byte[size];
            lock (randomNumberGenerator) { randomNumberGenerator.GetBytes(challenge); }

            byte[] encrypted = publicKey.EncryptLargeData(challenge, F_OAP);

            var token = Callback.AllocateToken();

            try
            {
                //send challenge
                using (MemoryStream m = new MemoryStream())
                {
                    m.WriteByte((byte)PacketFlag.Challenge);

                    Serializer.SerializeWithLengthPrefix<ChallengePacket>(m, new ChallengePacket() { Challenge = challenge, Callback = token.Id });
                    peer.Send(RoutingTable.LocalContact, id, m.ToArray());
                }

                if (!token.Wait(timeout))
                    throw new TimeoutException("Peer did not respond in a timely manner");

                if (token.Response == null)
                    return false;

                ChallengeResponse response;
                using (MemoryStream m = new MemoryStream(token.Response))
                    response = Serializer.DeserializeWithLengthPrefix<ChallengeResponse>(m);

                if (response.Flooded)
                    throw new FloodException(response.Backoff);

                if (response.Response.Length != challenge.Length)
                    return false;

                return response.Response.Zip(challenge, (a, b) => a == b).Aggregate((a, b) => a && b);
            }
            finally
            {
                Callback.FreeToken(token);
            }
        }

        private void HandleRemoteChallenge(Contact source, Stream message)
        {
            var p = Serializer.DeserializeWithLengthPrefix<ChallengePacket>(message);

            var decrypted = cryptoProvider.DecryptLargeData(p.Challenge, F_OAP);

            using (MemoryStream m = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix<ChallengeResponse>(m, new ChallengeResponse()
                {
                    Flooded = false,
                    Response = decrypted
                });

                Callback.SendResponse(RoutingTable.LocalContact, source, p.Callback, m.ToArray());
            }
        }

        [ProtoContract]
        private class ChallengePacket
        {
            [ProtoMember(1)]
            public byte[] Challenge;

            [ProtoMember(2)]
            public long Callback;
        }

        private class ChallengeResponse
        {
            [ProtoMember(1)]
            public bool Flooded;

            [ProtoMember(2)]
            private long backoffTicks;

            public TimeSpan Backoff
            {
                get
                {
                    return TimeSpan.FromTicks(backoffTicks);
                }
                set
                {
                    backoffTicks = value.Ticks;
                }
            }

            [ProtoMember(2)]
            public byte[] Response;
        }
        #endregion
    }
}
