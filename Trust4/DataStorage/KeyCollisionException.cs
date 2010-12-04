using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trust4.DataStorage
{
    public class KeyCollisionException
        :Exception
    {
        public KeyCollisionException(string msg)
            :base(msg)
        {
        }
    }
}
