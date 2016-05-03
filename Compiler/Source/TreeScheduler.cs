using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Source
{
    [DebuggerDisplay("TreeScheduler Active={ActiveTransactionCount} Waiting={WaitingTransactionCount}")]
    internal class TreeScheduler
    {
        private Dictionary<EventReference, List<CallReference>> _tree;
        private readonly Queue _tasks = new Queue();
        private List<EventReference> _expectedEvents = new List<EventReference>();
        private readonly TaskScheduler _scheduler;
        private readonly Action<Event> _callback;
        private readonly List<Guid> _transactions = new List<Guid>();
        private readonly Stack<Guid> _activeTransaction = new Stack<Guid>();
        private readonly Dictionary<Guid, List<EventReference>> _transactionEvents = new Dictionary<Guid, List<EventReference>>();

        private int ActiveTransactionCount => _activeTransaction.Count;
        private int WaitingTransactionCount => _tasks.Count;

        public TreeScheduler(Hashtable tree, Action<Event> callback)
        {
            BuildInternalTree(tree);
            _scheduler = TaskScheduler.Default;
            _callback = callback;
            Pump();
        }

        private void BuildInternalTree(Hashtable tree)
        {
            if (_tree == null)
            {
                _tree = new Dictionary<EventReference, List<CallReference>>();
            }

            var on = tree["on"] as Hashtable;
            Debug.Assert(on != null, "on != null");

            var references = new Dictionary<EventReference, List<CallReference>>();

            foreach (var evt in on.Keys)
            {
                var refer = new EventReference() { Name = evt as string };

                if (!references.ContainsKey(refer))
                {
                    references.Add(refer, new List<CallReference>());
                }

                var listOfCallees = on[evt] as List<Hashtable>;
                Debug.Assert(listOfCallees != null, "listOfCallees != null");
                foreach (var callee in listOfCallees)
                {
                    var callName = callee["call"] as string;
                    var callRef = new CallReference { Name = callName, Emits = new List<EventReference>() };
                    var emitsName = callee["emits"] as List<string>;
                    Debug.Assert(emitsName != null, "emitsName != null");
                    foreach (var emit in emitsName)
                    {
                        if (!references.ContainsKey(new EventReference() { Name = emit }))
                        {
                            references.Add(new EventReference() { Name = emit }, new List<CallReference>());
                        }
                        var r = (from j in references.Keys
                                 where j.Name == emit
                                 select j).FirstOrDefault();
                        callRef.Emits.Add(r);
                    }

                    references[refer].Add(callRef);

                    var state = callee["state"] as Hashtable;
                    Debug.Assert(state != null, "state != null");
                    var statereads = state["reads"] as List<string>;
                    var statewrite = state["writes"] as List<string>;
                }
            }

            _tree = references;
        }

        private Guid createNewTransaction(EventReference reference)
        {
            var transaction = Guid.NewGuid();
            _transactions.Add(transaction);
            _activeTransaction.Push(transaction);
            _transactionEvents.Add(transaction, CalculateEvents(reference));
            return transaction;
        }

        private void Pump()
        {
            var pump = new Task(() =>
            {
                lock (_expectedEvents)
                {
                    if (ActiveTransactionCount > 0 && !_transactions.Contains(_activeTransaction.Peek())) _activeTransaction.Pop();
                    if (ActiveTransactionCount == 0 && WaitingTransactionCount > 0)
                    {
                        var evt = _tasks.Dequeue() as Event;
                        var reference = new EventReference() {Name = evt.Name};
                        var transaction = createNewTransaction(reference);
                        ScheduleNow(WrapEvent(evt), transaction);
                    }

                    Task.Delay(50);
                    Pump();
                }
            });

            pump.Start(_scheduler);
        }

        public void ScheduleEmit(Event evt)
        {
            lock (_expectedEvents)
            {
                var reference = new EventReference() { Name = evt.Name };

                // if this is the first entry
                /*if (_activeTransaction.Count == 0)
                {
                    var transaction = createNewTransaction(reference);
                    ScheduleNow(WrapEvent(evt), transaction);
                    return;
                }*/

                // if this is the second entry and its an expected event
                if (ActiveTransactionCount > 0 &&
                    _transactionEvents.ContainsKey(_activeTransaction.Peek()) &&
                    _transactionEvents[_activeTransaction.Peek()].Contains(reference))
                {
                    _transactionEvents[_activeTransaction.Peek()].Remove(reference);
                    var transaction = createNewTransaction(reference);
                    ScheduleNow(WrapEvent(evt), transaction);
                    return;
                }

                // if the transaction has completed and we haven't any other transactions, pop this transaction and try again
                /*if (!_transactions.Contains(_activeTransaction.Peek()))
                {
                    _activeTransaction.Pop();
                    ScheduleLater(evt);
                    return;
                }*/

                ScheduleLater(evt);
            }
        }

        private List<EventReference> CalculateEvents(EventReference evt)
        {
            var reference = _tree[evt];

            return reference.SelectMany(callee => callee.Emits).ToList();
        }

        [DebuggerStepperBoundary]
        private Task WrapEvent(Event evt)
        {
            return new Task(() =>
            {
                _callback(evt);
            });
        }

        [DebuggerStepperBoundary]
        private void ScheduleNow(Task t, Guid transaction)
        {
            t.Start(_scheduler);
            t.ContinueWith((m) =>
            {
                _transactions.Remove(transaction);
            });
        }

        [DebuggerStepperBoundary]
        private void ScheduleLater(Event t)
        {
            _tasks.Enqueue(t);
        }

        [DebuggerDisplay("{Name}EventReference")]
        private class EventReference
        {
            public string Name { get; set; }
            public override string ToString()
            {
                return $"Evt: {Name}";
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var s = obj as string;
                if (s != null && s == Name) return true;
                var refer = obj as EventReference;
                if (refer == null) return false;
                return refer.Name == Name;
            }
        }

        [DebuggerDisplay("")]
        private class StateReference
        {
            public string Name { get; set; }

            public override string ToString()
            {
                return $"Stt: {Name}";
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var s = obj as string;
                if (s != null && s == Name) return true;
                var refer = obj as StateReference;
                if (refer == null) return false;
                return refer.Name == Name;
            }
        }

        [DebuggerDisplay("{Name}CallReference")]
        private class CallReference
        {
            public string Name { get; set; }
            public List<EventReference> Emits { get; set; }
            public List<StateReference> Reads { get; set; }
            public List<StateReference> Write { get; set; }

            public override string ToString()
            {
                return $"Call: {Name}";
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var s = obj as string;
                if (s != null && s == Name) return true;
                var refer = obj as CallReference;
                if (refer == null) return false;
                return refer.Name == Name;
            }
        }
    }
}
