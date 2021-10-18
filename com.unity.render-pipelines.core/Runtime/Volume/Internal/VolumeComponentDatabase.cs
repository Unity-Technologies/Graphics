using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Database of all type loaded in memory.
    ///
    /// It must only depends on what is loaded in memory (thus it is a static class).
    /// </summary>
    static class VolumeComponentDatabase
    {
        static VolumeComponentDatabase()
        {
            // Get the types
            baseComponentTypeArray = CoreUtils.GetAllTypesDerivedFrom<VolumeComponent>()
                .Where(t => !t.IsAbstract).ToArray();

            // Call the init method if it exists.
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var type in baseComponentTypeArray)
            {
                var initMethod = type.GetMethod("Init", flags);
                if (initMethod != null)
                {
                    initMethod.Invoke(null, null);
                    Debug.LogWarning($"{type} has an Init method, it won't be called in future release. " +
                        $"Please add the RuntimeInitializeOnLoadMethod attribute instead.");
                }
            }
        }

        public static Type[] baseComponentTypeArray { get; }
    }
}
