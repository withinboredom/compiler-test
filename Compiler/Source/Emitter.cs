using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Source
{
    public class Emitter<TBus> where TBus : Bus, new()
    {
        protected TBus _bus;
        protected Guid _id;

        public Emitter()
        {
            _bus = new TBus();
            _id = Guid.NewGuid();
        }

        public Emitter(Guid id)
        {
            _bus = new TBus();
            _id = id;
        }

        protected Task Emit<T>(string name, T value)
        {
            return Task.FromResult(_bus.Apply(new Event()
            {
                FromGuid = _id,
                Name = name,
                Type = (typeof(T)).ToString(),
                Value = value
            }));
        }

        internal void ChangeAttachment(TBus bus)
        {
            _bus = bus;
        }
    }
}
