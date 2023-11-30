using System;
using System.Reflection;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class ReflectionUtils
    {
        internal static void ForEachFieldOfType<T>(this object instance, Action<T> callback, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var fields = type.GetFields(flags);
            if (fields.Length == 0)
                return;

            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.GetValue(instance) is T fieldValue)
                {
                    callback(fieldValue);
                }
            }
        }
    }
}
