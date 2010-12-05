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
using DistributedServiceProvider.Base;

namespace Trust4.DataStorage
{
    public interface IDataStore
    {
        /// <summary>
        /// Puts a key-value pair into the local data store that's participating in the DHT.
        /// </summary>
        /// <param name="key">The unique key.</param>
        /// <param name="value">The value.</param>
        void Put(Identifier512 key, byte[] value);

        /// <summary>
        /// Deletes a key-value pair from the local data store that's participating in the DHT.
        /// </summary>
        /// <param name="key">The unique key.</param>
        void Delete(Identifier512 key);

        /// <summary>
        /// Returns a list of values retrieved from all of the connected peers for the selected
        /// key request.
        /// </summary>
        /// <param name="key">The unique key.</param>
        /// <param name="timeout">The timeout value for each peer.</param>
        IEnumerable<DataResult> Get(Identifier512 key);
    }
}
