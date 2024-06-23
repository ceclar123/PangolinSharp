using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangolin.Utility
{
    public class CollectionUtil
    {
        private CollectionUtil()
        { }
        public static bool IsEmpty([AllowNull] ICollection coll)
        {
            return coll == null || coll.Count == 0;
        }

        public static bool IsNotEmpty([AllowNull] ICollection coll)
        {
            return coll != null && coll.Count > 0;
        }

        public static List<T> EmptyList<T>()
        {
            return new List<T>();
        }
    }
}
