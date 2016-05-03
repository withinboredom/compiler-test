using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Source
{
    [DebuggerDisplay("Event Name={Name} From={FromGuid} Type={Type}")]
    [DataContract]
    public class Event
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public object Value { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public Guid FromGuid { get; set; }
    }
}
