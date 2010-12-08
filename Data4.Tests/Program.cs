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
using Data4;
using System.Net;
using System.Threading;

namespace Data4.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ID guid1 = ID.NewRandom();
            ID guid2 = ID.NewRandom();
            Dht dht1 = new Dht(guid1, new IPEndPoint(IPAddress.Loopback, 12000));
            Dht dht2 = new Dht(guid2, new IPEndPoint(IPAddress.Loopback, 13000));
            int count1 = 0;
            int count2 = 0;
            bool all1 = false;
            bool all2 = false;

            dht1.OnReceived += delegate(object sender, MessageEventArgs e)
            {
                if (e.Message is ConfirmationMessage)
                    return;

                Console.WriteLine("DHT1 received: " + e.Message);
                count1 += 1;
                all1 = ( count1 == 5 );
            };
            dht2.OnReceived += delegate(object sender, MessageEventArgs e)
            {
                if (e.Message is ConfirmationMessage)
                    return;
                
                Console.WriteLine("DHT2 received: " + e.Message.ToString());
                count2 += 1;
                all2 = ( count2 == 5 );
            };

            dht1.Contacts.Add(dht2.Self);
            dht2.Contacts.Add(dht1.Self);

            for (int i = 0; i < 5; i += 1)
            {
                DirectMessage dm = new DirectMessage(dht1, dht2.Self, "This is a message to the second node [" + i + "] !");
                Console.WriteLine("DHT1 sent: " + dm);
                dm.Send();
            }
            for (int i = 0; i < 5; i += 1)
            {
                DirectMessage dm = new DirectMessage(dht2, dht1.Self, "This is a message to the first node [" + i + "] !");
                Console.WriteLine("DHT2 sent: " + dm);
                dm.Send();
            }

            // Wait up to 10 seconds for all messages to be received.
            int wait = 0;
            while (wait < 10000)
            {
                if (all1 && all2)
                    break;
                wait += 100;
                Thread.Sleep(100);
            }

            if (all1 && all2)
                Console.WriteLine("PASS: All messages were delivered successfully.");
            else
            {
                Console.WriteLine("FAIL: " + count1 + " / 5 messages delivered for the first DHT.");
                Console.WriteLine("    : " + count2 + " / 5 messages delivered for the second DHT.");
            }

            dht1.Close();
            dht2.Close();
        }
    }
}

