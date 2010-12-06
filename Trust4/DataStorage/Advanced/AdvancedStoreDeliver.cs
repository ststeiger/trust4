using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using DistributedServiceProvider.MessageConsumers;
using System.IO;

namespace Trust4.DataStorage.Advanced
{
    public partial class AdvancedStore<K, V>
        : MessageConsumer, IMergingDataStore<K, V>
    {
        private byte nextFlag = byte.MinValue;
        private Dictionary<byte, Action<Contact, MemoryStream>> messageConsumers = new Dictionary<byte,Action<Contact,MemoryStream>>();

        private byte RegisterConsumer(Action<Contact, MemoryStream> action)
        {
            byte key = nextFlag++;
            messageConsumers.Add(key, action);

            return key;
        }

        public override void Deliver(Contact source, byte[] message)
        {
            using (MemoryStream m = new MemoryStream(message))
            {
                byte flag = (byte)m.ReadByte();

                Action<Contact, MemoryStream> action;
                if (!messageConsumers.TryGetValue(flag, out action))
                    throw new ArgumentException("Unknown message type " + flag);

                action(source, m);
            }
        }
    }
}
