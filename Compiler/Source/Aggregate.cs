using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Source
{
    public class Aggregate<TBus> : Emitter<TBus>, IDisposable where TBus : Bus, new()
    {
        public Aggregate() : base()
        {
            _bus.Emitted += BusOnEmitted;
        }

        protected List<string> ListenTo = new List<string>();

        private void BusOnEmitted(object sender, Event @event)
        {
            foreach (var listener in ListenTo)
            {
                if (@event.Name == listener)
                {
                    var type = GetType();
                    var method = type.GetMethod(listener,
                        BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var invoke = method.Invoke(this, (new List<object>() {@event}).ToArray());
                }
            }
        }

        public Aggregate(Guid id) : base(id)
        {
            _bus.Emitted += BusOnEmitted;
        }

        public void Dispose()
        {
            _bus.Emitted -= BusOnEmitted;
        }

        public Emitter<TBus> Attach(Emitter<TBus> to)
        {
            to.ChangeAttachment(_bus);
            return to;
        }
    }
}
