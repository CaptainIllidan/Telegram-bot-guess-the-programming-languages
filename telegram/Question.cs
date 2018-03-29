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
    public class Question
    {
        [DataMember]
        public readonly int Id;
        [DataMember]
        public readonly string Text;
        public Question (int id, string text)
        {
            Id = id;
            Text = text;
        }

        public Question() { }

        public override bool Equals(object obj)
        {
            return ((Question)obj).Id.Equals(Id);
        }
    }
}
