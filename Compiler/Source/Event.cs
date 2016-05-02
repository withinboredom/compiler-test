using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Source
{
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
