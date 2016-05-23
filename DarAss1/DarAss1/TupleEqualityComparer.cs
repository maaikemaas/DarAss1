using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarAss1
{
    class TupleEqualityComparer : IEqualityComparer<Tuple<int, int>>
    {
        public bool Equals(Tuple<int, int> t1, Tuple<int, int> t2)
        {
            if (t1 == null && t2 == null) return true;
            else if (t1 == null || t2 == null) return false;
            else if (t1.Item1 == t2.Item1) return true;
            else return false;
        }

        public int GetHashCode(Tuple<int, int> t)
        {
            int h = t.Item1 ^ t.Item2;
            return h.GetHashCode();
        }
    }           // Custom equality comparer om de Intersection en Union te doen
}
