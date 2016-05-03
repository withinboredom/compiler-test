using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Source
{
    [DebuggerDisplay("Emitter NumEvents={NumberEventsEmitted}")]
    public class Emitter<TBus> where TBus : Bus, new()
    {
        protected TBus _bus;
        protected Guid _id;

        private int NumberEventsEmitted { get; set; }

        [DebuggerStepperBoundary]
        public Emitter()
        {
            _bus = new TBus();
            _id = Guid.NewGuid();
        }

        [DebuggerStepperBoundary]
        public Emitter(Guid id)
        {
            _bus = new TBus();
            _id = id;
        }

        [DebuggerStepperBoundary]
        protected Task Emit<T>(string name, T value)
        {
            NumberEventsEmitted += 1;
            return Task.FromResult(_bus.Apply(new Event()
            {
                FromGuid = _id,
                Name = name,
                Type = (typeof(T)).ToString(),
                Value = value
            }));
        }

        [DebuggerStepperBoundary]
        internal void ChangeAttachment(TBus bus)
        {
            _bus = bus;
        }
    }
}
