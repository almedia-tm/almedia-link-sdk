using System.Collections.Generic;

namespace AlmediaSDK
{
    // Shared by the progress types in both namespaces: members compare by value, lists by sequence.
    internal static class ValueEquality
    {
        public static bool ListEquals<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!Equals(a[i], b[i])) return false;
            return true;
        }

        public static int Hash(params object[] parts)
        {
            unchecked
            {
                int hash = 17;
                foreach (var part in parts) hash = hash * 31 + (part?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static int ListHash<T>(IReadOnlyList<T> list)
        {
            unchecked
            {
                int hash = 17;
                foreach (var item in list) hash = hash * 31 + (item?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
