using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Source
{
    public class Aggregate<TBus> : Emitter<TBus>, IDisposable where TBus : Bus, new()
    {
        public Aggregate() : base()
        {
            Init();
            _bus.tree = new TreeScheduler(Tree, @event =>
            {
                BusOnEmitted(this, @event);
            });
        }

        protected virtual void Init() { }

        protected List<string> ListenTo = new List<string>();
        protected Hashtable Tree;

        [DebuggerStepperBoundary]
        private void BusOnEmitted(object sender, Event @event)
        {
            if (Tree == null) return;

            var on = Tree["on"] as Hashtable;
            var evt = @on?[@event.Name] as List<Hashtable>;

            if (evt == null) return;

            foreach (var handler in evt)
            {
                var callee = handler?["call"] as string;
                if (callee == null) continue;

                var type = GetType();
                var method = type.GetMethod(callee,
                    BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.Instance);
                method.Invoke(this, (new List<object> { @event }).ToArray());
            }
        }

        public Aggregate(Guid id) : base(id)
        {
        }

        public void Dispose()
        {
        }

        public Emitter<TBus> Attach(Emitter<TBus> to)
        {
            to.ChangeAttachment(_bus);
            return to;
        }
    }
}
