using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitorUtils
{
    internal class InstanceChecker
    {
        static readonly Mutex mutex = new Mutex(false, "monitorutilsbydvgm");
        static bool taken;
        public static bool TakeMemory()
        {
            return taken = mutex.WaitOne(0, true);
        }
        public static void ReleaseMemory()
        {
            if (taken)
                try { mutex.ReleaseMutex(); } catch { }
        }
    }
}
