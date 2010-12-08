// 
//  Copyright 2010  Trust4 Developers
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.IO;

namespace Data4
{
    [Serializable()]
    public abstract class Message : ISerializable
    {
        private Dht p_Dht = null;
        private Contact p_Source = null;
        private List<Contact> p_Seen = new List<Contact>();
        private string p_Data = null;

        private Guid m_Identifier = Guid.Empty;
        private bool m_Sent = false;
        private bool m_Recieved = false;

        public Message(Dht dht, string data)
        {
            this.Dht = dht;
            this.p_Source = this.p_Dht.Self;
            this.p_Data = data;
        }

        public Message(SerializationInfo info, StreamingContext context)
        {
            this.p_Source = info.GetValue("message.source", typeof(Contact)) as Contact;
            this.p_Seen = info.GetValue("message.seen", typeof(List<Contact>)) as List<Contact>;
            this.p_Data = info.GetString("message.data");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("message.source", this.p_Source, this.p_Source.GetType());
            info.AddValue("message.seen", this.p_Seen, typeof(List<Contact>));
            info.AddValue("message.data", this.p_Data);
        }

        /// <summary>
        /// Sends the message to it's target through the peer-to-peer network.  Returns a duplicate of
        /// this message, but without the send-specific information such as unique identifier and event
        /// handler attached.  The duplicated message should be used when sending a message to multiple
        /// receipients.
        /// </summary>
        /// <param name="target">The target to send the message to.</param>
        /// <returns>A new message that duplicates the properties of the one being sent.</returns>
        protected Message Send(Contact target)
        {
            if (this.p_Dht == null)
                throw new InvalidOperationException("The message could not be sent because there is no DHT associated with the message.");

            if (this.m_Sent)
                throw new InvalidOperationException("Messages can not be resent with Sent().  Use the duplicated message object from Sent() to resend a message.");
            this.m_Sent = true;

            Message duplicate = this.Clone();

            // TODO: Give this more randomization to ensure a higher degree of uniqueness.
            this.m_Identifier = Guid.NewGuid();

            // Send the message to the target.
            UdpClient udp = new UdpClient();
            using (MemoryStream writer = new MemoryStream())
            {
                this.Dht.Log(Dht.LogType.INFO, "Sending -");
                this.Dht.Log(Dht.LogType.INFO, "          Message - " + this.ToString());
                this.Dht.Log(Dht.LogType.INFO, "          Target - " + target.ToString());
                this.Dht.Formatter.Serialize(writer, this);
                int bytes = udp.Send(writer.GetBuffer(), writer.GetBuffer().Length, target.EndPoint);
                this.Dht.Log(Dht.LogType.INFO, bytes + " total bytes sent.");
            }

            return duplicate;
        }

        /// <summary>
        /// Clones the current message without providing any send information in the returned message.
        /// </summary>
        protected abstract Message Clone();

        /// <summary>
        /// This event is raised when the Dht receives a message.  It is used by the
        /// Message class to detect confirmation replies.
        /// </summary>
        public void OnReceive(object sender, MessageEventArgs e)
        {
            if (!this.m_Sent)
                return;

            if (e.Message is ConfirmationMessage && e.Message.m_Identifier == this.m_Identifier)
            {
                this.m_Recieved = true;
            }
        }

        /// <summary>
        /// Returns whether or not this message has already seen the specified contact.
        /// </summary>
        /// <param name="c">The contact to test.</param>
        /// <returns>Whether or not this message has already seen the specified contact.</returns>
        public bool SeenBy(Contact c)
        {
            return ( this.p_Seen.Contains(c) || this.p_Source == c );
        }

        /// <summary>
        /// The DHT associated with this message.  A DHT must be associated to use the
        /// Send() and Received() functions.
        /// </summary>
        public Dht Dht
        {
            get { return this.p_Dht; }
            set
            {
                if (this.p_Dht != null)
                    this.p_Dht.OnReceived -= this.OnReceive;
                this.p_Dht = value;
                if (this.p_Dht != null)
                    this.p_Dht.OnReceived += this.OnReceive;
            }
        }

        /// <summary>
        /// Whether the message has been sent with Send() and hence can not be resent.
        /// </summary>
        public bool Sent
        {
            get { return this.m_Sent; }
        }

        /// <summary>
        /// Whether the message was received by the recipient specified as the argument
        /// to Send().
        /// </summary>
        public bool Received
        {
            get { return this.m_Recieved; }
        }

        /// <summary>
        /// The source of this message.
        /// </summary>
        public Contact Source
        {
            get { return this.p_Source; }
        }

        /// <summary>
        /// The nodes that this message has passed through, excluding the creator and
        /// the current node.
        /// </summary>
        public ReadOnlyCollection<Contact> Seen
        {
            get { return this.p_Seen.AsReadOnly(); }
        }

        /// <summary>
        /// The data associated with this message.
        /// </summary>
        public string Data
        {
            get { return this.p_Data; }
        }

        public override string ToString()
        {
            return string.Format("[Message: Dht={0}, Source={1}, Seen={2}, Data={3}]", this.Dht, this.Source, this.Seen, this.Data);
        }
    }
}

