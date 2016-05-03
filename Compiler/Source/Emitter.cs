using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Source
{
    /// <summary>
    /// Most basic kind of thing ... something that emits events
    /// </summary>
    /// <typeparam name="TBus"></typeparam>
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

        /// <summary>
        /// Emit an event into the world
        /// </summary>
        /// <typeparam name="T">The type of the value of the event</typeparam>
        /// <param name="name">The name of the event to emit</param>
        /// <param name="value">The value of the event</param>
        /// <returns></returns>
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

        /// <summary>
        /// Change the attachments of this emitter
        /// </summary>
        /// <param name="bus"></param>
        [DebuggerStepperBoundary]
        internal void ChangeAttachment(TBus bus)
        {
            _bus = bus;
        }
    }
}
