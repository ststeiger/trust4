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
using System.Text;
using Data4;

namespace Admin4
{
    public class LogWriter : HttpServer.ILogWriter
    {
        private Dht m_Dht = null;

        public LogWriter(Dht dht)
        {
            this.m_Dht = dht;
        }

        public void Write(object source, HttpServer.LogPrio priority, string message)
        {
            Dht.LogType type = Dht.LogType.DEBUG;
            switch (priority)
            {
                case HttpServer.LogPrio.Fatal:
                    type = Dht.LogType.ERROR;
                    break;
                case HttpServer.LogPrio.Error:
                    type = Dht.LogType.ERROR;
                    break;
                case HttpServer.LogPrio.Warning:
                    type = Dht.LogType.WARNING;
                    break;
                case HttpServer.LogPrio.Info:
                    type = Dht.LogType.INFO;
                    break;
                case HttpServer.LogPrio.Debug:
                    type = Dht.LogType.DEBUG;
                    break;
                case HttpServer.LogPrio.Trace:
                    type = Dht.LogType.DEBUG;
                    break;
            }

            this.m_Dht.Log(type, message);
        }
    }
}
