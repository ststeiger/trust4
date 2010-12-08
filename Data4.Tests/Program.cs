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
using System.Net;
using System.Threading;
using Data4;

namespace Data4.Tests
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            bool result = true;

            if (result)
                result = Program.DoMessageTest();
            if (result)
                result = Program.DoStorageTest();

            return (result == true) ? 0 : 1;
        }

        public static bool DoMessageTest()
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
            
            dht1.Close();
            dht2.Close();

            if (all1 && all2)
            {
                Console.WriteLine("PASS: All messages were delivered successfully.");
                return true;
            }
            else
            {
                Console.WriteLine("FAIL: " + count1 + " / 5 messages delivered for the first node.");
                Console.WriteLine("    : " + count2 + " / 5 messages delivered for the second node.");
                return false;
            }
        }

        public static bool DoStorageTest()
        {
            ID guid1 = ID.NewRandom();
            ID guid2 = ID.NewRandom();
            Dht dht1 = new Dht(guid1, new IPEndPoint(IPAddress.Loopback, 12000));
            Dht dht2 = new Dht(guid2, new IPEndPoint(IPAddress.Loopback, 13000));
            //dht1.Debug = true;
            //dht2.Debug = true;

            dht1.Contacts.Add(dht2.Self);
            dht2.Contacts.Add(dht1.Self);

            // Add some data to node 1.
            ID id = ID.NewRandom();
            dht1.Put(id, "storage test 1");
            dht1.Put(id, "storage test 2");

            // Retrieve the data via node 2.
            int count1 = 0;
            int count2 = 0;
            IList<Entry> entries = dht2.Get(id);
            foreach (Entry e in entries)
            {
                if (e.Value == "storage test 1")
                    count1 += 1;
                if (e.Value == "storage test 2")
                    count2 += 2;
                Console.WriteLine(e.Owner + " gave '" + e.Value + "'.");
            }

            if (count1 == 1 && count2 == 1)
            {
                Console.WriteLine("PASS: All storage tests were stored and retrieved successfully.");
                return true;
            }

            else
            {
                Console.WriteLine("FAIL: Storage test 1 exists " + count1 + " times in the DHT.");
                Console.WriteLine("    : Storage test 2 exists " + count2 + " times in the DHT.");
                return false;
            }
        }
    }
}

