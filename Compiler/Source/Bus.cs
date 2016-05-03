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
    public class Bus
    {
        internal ConcurrentQueue<Event> Events = new ConcurrentQueue<Event>();
        internal ConcurrentExclusiveSchedulerPair scheduler = new ConcurrentExclusiveSchedulerPair();
        internal TreeScheduler tree;

        [DebuggerNonUserCode]
        public bool Apply(Event ev)
        {
            tree?.ScheduleEmit(ev);
            return true;
        }
    }
}
