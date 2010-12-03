using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DistributedServiceProvider.MessageConsumers
{
    public class LinkedConsumerAttribute
        :Attribute
    {
        public readonly Guid Id;
        public readonly bool Optional = false;
        
        public LinkedConsumerAttribute(string guid) : this(guid, false)
        {
        }

        public LinkedConsumerAttribute(string guid, bool optional)
        {
            this.Id = new Guid(guid);
            this.Optional = optional;
        }
    }
}
