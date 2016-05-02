using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Source
{
    public class Bus
    {
        internal ConcurrentQueue<Event> Events = new ConcurrentQueue<Event>();
        internal ReaderWriterLockSlim locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public bool Apply(Event ev)
        {
            locker.EnterReadLock();
            Events.Enqueue(ev);
            locker.ExitReadLock();

            Pump();

            return true;
        }

        public event EventHandler<Event> Emitted;

        public void Pump()
        {
            Task.Run(() =>
            {
                if (locker.WaitingWriteCount == 0 && locker.RecursiveWriteCount == 0)
                {
                    locker.EnterWriteLock();
                    while (Events.Count > 0)
                    {
                        Event ev;
                        if (Events.TryDequeue(out ev))
                            Emitted?.Invoke(null, ev);
                    }
                    Task.Delay(50);

                    locker.ExitWriteLock();
                }
            });
        }
    }
}
