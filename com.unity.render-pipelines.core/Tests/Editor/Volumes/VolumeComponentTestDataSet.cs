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

        public static readonly VolumeComponentType[][] volumeComponentTypesArray = Enumerable.Range(0, 19)
            .RandomInitState(32456754)
            .Select(count =>
                Enumerable.Range(0, count)
                    .Select(_ => volumeComponentTypes.RandomElement())
                    .ToArray()
            ).ToArray();

        public static readonly VolumeComponentType[][][] volumeComponentTypesArrayArray = Enumerable.Range(0, 19)
            .RandomInitState(78963216)
            .Select(count =>
                Enumerable.Range(0, count)
                    // For each item pick an entry randomly
                    .Select(_ => volumeComponentTypesArray.RandomElement())
                    .ToArray()
            ).ToArray();

        public static readonly Type[] types = TestTypes.AllVolumeComponents.Take(20)
            .Union(new Type[] { null })
            .Union(csharpTypes)
            .ToArray();

        public static readonly VolumeComponentArchetype[] volumeComponentArchetypes = Enumerable.Range(0, 20)
            .RandomInitState(6531782)
            .Select(_ => volumeComponentTypes.RandomEnumeration().ToArray())
            .Select(VolumeComponentArchetype.FromTypes)
            .ToArray();
    }
}
