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

namespace Trust4
{
    public class Program
    {
        private static Manager p_Manager = null;

        public static void Main(string[] args)
        {
#if Release
            try
            {
#endif
                Program.p_Manager = new Manager();
#if Release
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
#endif
        }

        public static Manager Manager
        {
            get { return Program.p_Manager; }
        }
    }
}
