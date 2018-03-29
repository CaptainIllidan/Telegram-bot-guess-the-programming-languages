using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace telegram
{
    [DataContract]
    public class ProgrammingLanguage 
    {
        [DataMember]
        public readonly int Id;
        [DataMember]
        public readonly string Name;
        [DataMember]
        public string IconUrl;

        public ProgrammingLanguage(int id, string name, string iconUrl=null)
        {
            Id = id;
            Name = name;
            IconUrl = iconUrl;
        }

        public ProgrammingLanguage() { }

        public override bool Equals(object obj)
        {
            return ((ProgrammingLanguage)obj).Id.Equals(Id);
        }
    }
}
