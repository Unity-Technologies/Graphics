using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.UIElements;

namespace UnityEditor.Graphing.Util
{
    static class UIUtilities
    {
        public static void Synchronize<T>(this IEnumerable<VisualElement> elements, IEnumerable<T> source, Action<T> addAction, Action<VisualElement> removeAction)
        {
            var sourceSet = new HashSet<T>(source);
            var elementsToRemove = new List<VisualElement>();

            foreach (var element in elements)
            {
                if (!(element.userData is T data))
                {
                    continue;
                }

                if (!sourceSet.Remove(data))
                {
                    elementsToRemove.Add(element);
                }
            }

            foreach (var element in elementsToRemove)
            {
                removeAction(element);
            }

            foreach (var item in sourceSet)
            {
                addAction(item);
            }
        }

        public static void Synchronize<T>(this VisualElement container, IEnumerable<T> source, Action<T> addAction, Action<VisualElement> removeAction)
        {
            container.Children().Synchronize(source, addAction, removeAction);

            var indexMap = new Dictionary<object, int>();
            var i = 0;
            foreach (var item in source)
            {
                indexMap[item] = i++;
            }

            container.Sort((v1, v2) =>
                indexMap.TryGetValue(v1.userData, out var i1) && indexMap.TryGetValue(v2.userData, out var i2)
                    ? i1 - i2
                    : 0);
        }

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
    }
}
