using System.Collections.Generic;

namespace UnityEditor.Graphing.Util
{
    public static class UIUtilities
    {
        public static bool ItemsReferenceEquals<T>(this IList<T> first, IList<T> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (!ReferenceEquals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetHashCode(params object[] objects)
        {
            var hashCode = 17;
            foreach (var @object in objects)
            {
                hashCode = hashCode * 31 + (@object == null ? 79 : @object.GetHashCode());
            }
            return hashCode;
        }
    }
}
