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
    }
}
