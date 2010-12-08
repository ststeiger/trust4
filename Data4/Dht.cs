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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Data4
{
    public class Dht
    {
        private Contact m_Self = null;
        private List<Contact> p_Contacts = new List<Contact>();
        private IFormatter p_Formatter = null;
        private UdpClient m_UdpClient = null;
        private Thread m_UdpThread = null;

        public event EventHandler<MessageEventArgs> OnReceived;

        public Dht(Guid identifier, IPEndPoint endpoint)
        {
            this.m_Self = new Contact(identifier, endpoint);
            this.p_Formatter = new BinaryFormatter();

            // Start listening for events.
            this.m_UdpThread = new Thread(delegate()
            {
                try
                {
                    IPEndPoint from = null;
                    this.m_UdpClient = new UdpClient(endpoint.Port, AddressFamily.InterNetwork);
                    while (true)
                    {
                        byte[] result = this.m_UdpClient.Receive(ref from);
                        this.Log(LogType.INFO, "Received a message from " + from.ToString());
                        this.OnReceive(endpoint, result);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            );
            //this.m_UdpThread.IsBackground = true;
            this.m_UdpThread.Start();
        }

        /// <summary>
        /// Handles receiving data through the UdpClient.
        /// </summary>
        /// <param name="endpoint">The endpoint from which the message was received.</param>
        /// <param name="result">The data that was received.</param>
        private void OnReceive(IPEndPoint endpoint, byte[] result)
        {
            using (MemoryStream stream = new MemoryStream(result))
            {
                Message message = this.p_Formatter.Deserialize(stream) as Message;
                MessageEventArgs e = new MessageEventArgs(message);
                message.Dht = this;
                if (this.OnReceived != null)
                    this.OnReceived(this, e);

                if (e.SendConfirmation && !(e.Message is ConfirmationMessage))
                {
                    ConfirmationMessage cm = new ConfirmationMessage(this, message.Source, "");
                    cm.Send();

                    // TODO: Make sure that the confirmation message is received.  Probably should
                    //       implement confirmation of confirmations in ConformationMessage class itself.
                }
            }
        }

        public void Close()
        {
            this.m_UdpThread.Abort();
        }

        public IFormatter Formatter
        {
            get { return this.p_Formatter; }
        }

        public Contact Self
        {
            get { return this.m_Self; }
        }

        public List<Contact> Contacts
        {
            get { return this.p_Contacts; }
        }

        public enum LogType
        {
            ERROR,
            WARNING,
            INFO
        }

        public void Log(LogType type, string msg)
        {
            switch (type)
            {
                case LogType.ERROR:
                    Console.WriteLine("ERROR  : " + this.m_Self.Identifier.ToString() + " : " + msg);
                    break;
                case LogType.WARNING:
                    Console.WriteLine("WARNING: " + this.m_Self.Identifier.ToString() + " : " + msg);
                    break;
                case LogType.INFO:
                    Console.WriteLine("INFO   : " + this.m_Self.Identifier.ToString() + " : " + msg);
                    break;
                default:
                    Console.WriteLine("UNKNOWN: " + this.m_Self.Identifier.ToString() + " : " + msg);
                    break;
            }
        }
    }
}

