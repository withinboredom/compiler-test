using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Source
{
    /// <summary>
    /// This class needs to be removed...
    /// </summary>
    public class Bus
    {
        internal TreeScheduler tree;

        [DebuggerNonUserCode]
        public bool Apply(Event ev)
        {
            tree?.ScheduleEmit(ev);
            return true;
        }
    }
}
