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
        /// <summary>
        /// An event handler tree
        /// </summary>
        private Dictionary<EventReference, List<CallReference>> _tree;

        /// <summary>
        /// A queue of events to process
        /// </summary>
        private readonly Queue _tasks = new Queue();

        /// <summary>
        /// A syncronization object ... would be better to make it lock free
        /// </summary>
        private List<EventReference> _expectedEvents = new List<EventReference>();

        /// <summary>
        /// The scheduler for tasks
        /// </summary>
        private readonly TaskScheduler _scheduler;

        /// <summary>
        /// The callback for when a handler needs to be called
        /// </summary>
        private readonly Action<Event> _callback;

        /// <summary>
        /// The transactions that are in play
        /// </summary>
        private readonly List<Guid> _transactions = new List<Guid>();

        /// <summary>
        /// Active transactions, in the order called
        /// </summary>
        private readonly Stack<Guid> _activeTransaction = new Stack<Guid>();

        /// <summary>
        /// Expected events from a given transaction
        /// </summary>
        private readonly Dictionary<Guid, List<EventReference>> _transactionEvents = new Dictionary<Guid, List<EventReference>>();

        /// <summary>
        /// The count of active transactions
        /// </summary>
        private int ActiveTransactionCount => _activeTransaction.Count;

        /// <summary>
        /// The number of waiting transactions
        /// </summary>
        private int WaitingTransactionCount => _tasks.Count;

        /// <summary>
        /// Is the currently active transaction still running
        /// </summary>
        private bool IsActiveTransactionAlive
            => _activeTransaction.Count > 0 && _transactions.Contains(_activeTransaction.Peek());

        /// <summary>
        /// Should a waiting transaction be taken off the queue
        /// </summary>
        private bool ShouldTakeWaitingTransaction => ActiveTransactionCount == 0 && WaitingTransactionCount > 0;

        /// <summary>
        /// Creates a tree scheduler
        /// </summary>
        /// <param name="tree">The tree to parse</param>
        /// <param name="callback">The callback to make when an event matches information from the provided tree</param>
        public TreeScheduler(Hashtable tree, Action<Event> callback)
        {
            BuildInternalTree(tree);
            _scheduler = TaskScheduler.Default;
            _callback = callback;
            Pump();
        }

        /// <summary>
        /// Builds an internal tree
        /// </summary>
        /// <param name="tree">A hashtable ... this needs to be refactored</param>
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

        /// <summary>
        /// Creates a new transaction
        /// </summary>
        /// <param name="reference">The event reference to create the transaction for</param>
        /// <returns>An id of the transaction</returns>
        private Guid CreateNewTransaction(EventReference reference)
        {
            var transaction = Guid.NewGuid();
            _transactions.Add(transaction);
            _activeTransaction.Push(transaction);
            _transactionEvents.Add(transaction, CalculateEvents(reference));
            return transaction;
        }

        /// <summary>
        /// Keeps events flowing through the system at a steady rate
        /// </summary>
        private void Pump()
        {
            var pump = new Task(() =>
            {
                lock (_expectedEvents)
                {
                    if (ActiveTransactionCount > 0 && !IsActiveTransactionAlive) _activeTransaction.Pop();
                    if (ShouldTakeWaitingTransaction)
                    {
                        var evt = _tasks.Dequeue() as Event;
                        var transaction = CreateNewTransaction(CreateEventReferenceFrom(evt));
                        ScheduleNow(WrapEvent(evt), transaction);
                    }

                    Task.Delay(50).Wait();
                    Pump();
                }
            });

            pump.Start(_scheduler);
        }

        /// <summary>
        /// Create an event reference from a given event
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        private static EventReference CreateEventReferenceFrom(Event evt)
        {
            return new EventReference { Name = evt.Name };
        }

        /// <summary>
        /// Determine if an event is currently expected at this stage in the tree
        /// </summary>
        /// <param name="evt">The event to check</param>
        /// <returns>True if it is expected, false if not</returns>
        private bool CurrentlyExpectingEvent(Event evt)
        {
            return _transactionEvents[_activeTransaction.Peek()].Contains(CreateEventReferenceFrom(evt));
        }

        /// <summary>
        /// Removes an expected event from the list of expected events
        /// </summary>
        /// <param name="evt">The event to remove</param>
        private void RemoveExpectedEventFromTransaction(Event evt)
        {
            _transactionEvents[_activeTransaction.Peek()].Remove(CreateEventReferenceFrom(evt));
        }

        /// <summary>
        /// Schedules an event emit as soon as possible
        /// </summary>
        /// <param name="evt">The event to emit</param>
        public void ScheduleEmit(Event evt)
        {
            lock (_expectedEvents)
            {
                // if this is the second entry and its an expected event
                if (IsActiveTransactionAlive &&
                    CurrentlyExpectingEvent(evt))
                {
                    RemoveExpectedEventFromTransaction(evt);
                    var transaction = CreateNewTransaction(CreateEventReferenceFrom(evt));
                    ScheduleNow(WrapEvent(evt), transaction);
                    return;
                }

                ScheduleLater(evt);
            }
        }

        /// <summary>
        /// Given a event reference, this will give you the expected events for that reference
        /// </summary>
        /// <param name="evt">The event reference</param>
        /// <returns>The expected events</returns>
        private List<EventReference> CalculateEvents(EventReference evt)
        {
            var reference = _tree[evt];

            return reference.SelectMany(callee => callee.Emits).ToList();
        }

        /// <summary>
        /// Wraps an event in a task to the callback. The task has not been started yet and you'll need to start it manually.
        /// </summary>
        /// <param name="evt">The event to wrap</param>
        /// <returns>An unstarted task</returns>
        [DebuggerStepperBoundary]
        private Task WrapEvent(Event evt)
        {
            return new Task(() =>
            {
                _callback(evt);
            });
        }

        /// <summary>
        /// Schedules a task to execute now
        /// </summary>
        /// <param name="t">The task to execute</param>
        /// <param name="transaction">The transaction to trace</param>
        [DebuggerStepperBoundary]
        private void ScheduleNow(Task t, Guid transaction)
        {
            t.Start(_scheduler);
            t.ContinueWith((m) =>
            {
                _transactions.Remove(transaction);
            });
        }

        /// <summary>
        /// Schedules an event to be emitted at a later time, FIFO
        /// </summary>
        /// <param name="t">The event to emit later</param>
        [DebuggerStepperBoundary]
        private void ScheduleLater(Event t)
        {
            _tasks.Enqueue(t);
        }

        /// <summary>
        /// A reference to an event
        /// </summary>
        [DebuggerDisplay("{Name}EventReference")]
        private class EventReference
        {
            /// <summary>
            /// The name of the event
            /// </summary>
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

        /// <summary>
        /// A reference to a state object
        /// </summary>
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

        /// <summary>
        /// A reference to a callable handler.
        /// </summary>
        [DebuggerDisplay("{Name}CallReference")]
        private class CallReference
        {
            /// <summary>
            /// The name of the handler
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The events the handler might emit
            /// </summary>
            public List<EventReference> Emits { get; set; }

            /// <summary>
            /// The state that it is required to read
            /// </summary>
            public List<StateReference> Reads { get; set; }

            /// <summary>
            /// The state that it is required to write
            /// </summary>
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
