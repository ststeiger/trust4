// 
//  Copyright 2011  Trust4 Developers
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

namespace Trust4
{
    public static class UNIX
    {
        public static bool IsUnix
        {
            get
            {
                int p = (int) Environment.OSVersion.Platform;
                return ( ( p == 4 ) || ( p == 6 ) || ( p == 128 ) );
            }
        }

        /// <summary>
        /// Returns whether the system has suitable permissions for binding to ports under 1024, commonly associated
        /// with being the root user under UNIX systems.  On Windows, this function always returns true.
        /// </summary>
        public static bool HasRootPermissions()
        {
            if (UNIX.IsUnix)
            {
                // Return whether we are root.
                return ( Mono.Unix.Native.Syscall.getuid() == 0 );
            }
            else
                return true;
        }

        /// <summary>
        /// Checks the UNIX environment to ensure that Mono's XDG_CONFIG_HOME variable is set correctly.  On Windows,
        /// this function always returns true.
        /// </summary>
        public static bool CheckEnvironment()
        {
            if (UNIX.IsUnix)
            {
                // Ensure that the environment variable XDG_CONFIG_HOME is set correctly.
                if (Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") != ".")
                {
                    Console.WriteLine("Error!  You must set XDG_CONFIG_HOME to \".\" when running this application.  i.e. 'sudo XDG_CONFIG_HOME=. mono Trust4.exe'");
                    return false;
                }
                else
                    return true;
            }
            else
                return true;
        }

        /// <summary>
        /// Changes the current user and group ID of the running process on UNIX systems.  This function must not be
        /// called under Windows systems.
        /// </summary>
        /// <param name="uid">The user ID to change the process to.</param>
        /// <param name="gid">The group ID to change the process to.</param>
        public static bool UpdateUIDGID(uint uid, uint gid)
        {
            if (Mono.Unix.Native.Syscall.getuid() != 0 || ( uid == 0 && gid == 0 ))
            {
                // We don't need to lower / change permissions since we aren't root
                // or the requested UID / GID pair is root.
                return true;
            }

            int res = Mono.Unix.Native.Syscall.setregid(gid, gid);
            if (res != 0)
            {
                Console.WriteLine("Error!  Unable to lower effective and real group IDs to " + gid + ".  '" + Mono.Unix.Native.Stdlib.GetLastError().ToString() + "'");
                return false;
            }
            res = Mono.Unix.Native.Syscall.setreuid(uid, uid);
            if (res != 0)
            {
                Console.WriteLine("Error!  Unable to lower effective and real user IDs to " + uid + ".  '" + Mono.Unix.Native.Stdlib.GetLastError().ToString() + "'");
                return false;
            }
            return true;
        }
    }
}

