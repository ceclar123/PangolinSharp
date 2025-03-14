using System.Collections;

namespace Pangolin.Utility
{
    public class CollectionUtil
    {
        private CollectionUtil()
        {
        }

        public static bool IsEmpty(ICollection? coll)
        {
            return coll == null || coll.Count == 0;
        }

        public static bool IsNotEmpty(ICollection? coll)
        {
            return coll != null && coll.Count > 0;
        }

        public static List<T> EmptyList<T>()
        {
            return new List<T>();
        }
    }
}