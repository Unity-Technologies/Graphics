using System;
using System.Linq;

namespace UnityEngine.Rendering.Tests
{
    public static partial class VolumeComponentTestDataSet
    {
        public static readonly int[] intSeeds = Enumerable.Range(0, 10).ToArray();

        public static readonly Type[] csharpTypes = {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
        };

        public static readonly VolumeComponentType[] volumeComponentTypes = TestTypes.AllVolumeComponents
            .Select(VolumeComponentType.FromTypeUnsafe)
            .ToArray();

        public static readonly Type[] types = TestTypes.AllVolumeComponents.Take(20)
            .Union(new Type[] { null })
            .Union(csharpTypes)
            .ToArray();

        public static readonly VolumeComponentArchetype[] volumeComponentArchetypes = DefaultVolumeComponentArchetypes();
        static VolumeComponentArchetype[] DefaultVolumeComponentArchetypes()
        {
            // Arbitrary, but fixed seed
            Random.InitState(10653106);
            // Generate n entries
            var types = Enumerable.Range(0, 20)
                // For each entries generate m VolumeComponentType
                .Select(i => Enumerable.Range(0, Random.Range(0, volumeComponentTypes.Length - 1))
                    .Select(typeIndex => volumeComponentTypes[typeIndex])
                    .ToArray())
                .Select(VolumeComponentArchetype.FromTypes)
                .ToArray();
            return types;
        }
    }
}
