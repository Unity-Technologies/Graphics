using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An immutable database of volume component types.
    /// </summary>
    public class VolumeComponentDatabase
    {
        /// <summary>
        /// Database of all type loaded in memory.
        ///
        /// It must only depends on what is loaded in memory (thus it is a static).
        /// </summary>
        [NotNull]
        public static VolumeComponentDatabase memoryDatabase { get; }

        static VolumeComponentDatabase()
        {
            // Get the types
            var componentTypes = CoreUtils.GetAllTypesDerivedFrom<VolumeComponent>()
                .Where(t => !t.IsAbstract).Select(VolumeComponentType.FromTypeUnsafe).ToArray();

            // Call the init method if it exists.
            StaticInitializeComponents(componentTypes);

            memoryDatabase = new VolumeComponentDatabase(componentTypes);
        }

        static void StaticInitializeComponents(VolumeComponentType[] componentTypes)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var type in componentTypes)
            {
                var initMethod = type.AsType().GetMethod("Init", flags);
                if (initMethod != null)
                {
                    initMethod.Invoke(null, null);
                    Debug.LogWarning($"{type} has an Init method, it won't be called in future release. " +
                        $"Please add the RuntimeInitializeOnLoadMethod attribute instead.");
                }
            }
        }

        public static VolumeComponentDatabase FromTypes([DisallowNull] VolumeComponentType[] types)
            => new VolumeComponentDatabase(types);

        VolumeComponentDatabase([DisallowNull] VolumeComponentType[] componentTypes) => this.componentTypes = componentTypes;

        [NotNull]
        public VolumeComponentType[] componentTypes { get; }
    }
}
