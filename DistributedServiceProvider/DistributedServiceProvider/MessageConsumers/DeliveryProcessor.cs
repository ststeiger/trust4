using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistributedServiceProvider.Contacts;
using System.IO;

namespace DistributedServiceProvider.MessageConsumers
{
    public class DeliveryProcessor
        :Attribute
    {
        public readonly Action<Contact, MemoryStream> Action;

        public DeliveryProcessor(Action<Contact, MemoryStream> action)
        {
            Action = action;
        }
    }
}
