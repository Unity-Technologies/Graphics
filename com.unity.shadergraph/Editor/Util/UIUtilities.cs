using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Graphing.Util
{
    static class UIUtilities
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
            return GetHashCode(objects.AsEnumerable());
        }

        public static int GetHashCode<T>(IEnumerable<T> objects)
        {
            var hashCode = 17;
            foreach (var @object in objects)
            {
                hashCode = hashCode * 31 + (@object == null ? 79 : @object.GetHashCode());
            }
            return hashCode;
        }

        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }

        public static void Add<T>(this VisualElement visualElement, T elementToAdd, Action<T> action)
            where T : VisualElement
        {
            visualElement.Add(elementToAdd);
            action(elementToAdd);
        }

        public static IEnumerable<Type> GetTypesOrNothing(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }

        public static Vector2 CalculateCentroid(IEnumerable<Vector2> nodePositions)
        {
            Vector2 centroid = Vector2.zero;
            int count = 1;
            foreach (var position in nodePositions)
            {
                centroid = centroid + (position - centroid) / count;
                ++count;
            }
            return centroid;
        }
    }
}
