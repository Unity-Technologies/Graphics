using System;
using System.Linq;
using System.Reflection;
using FsCheck;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    public static partial class ArbX
    {
        static readonly Type[] k_DefaultTypes = {
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
            typeof(string), };

        public class ArbitraryVolumeComponentType : Arbitrary<VolumeComponentType>
        {
            static VolumeComponentType[] s_Types = TestTypes.AllVolumeComponents
                .Select(VolumeComponentType.FromTypeUnsafe)
                .ToArray();

            public override Gen<VolumeComponentType> Generator => Gen.Elements(s_Types);
        }

        public partial class Arbitraries
        {
            public static Arbitrary<VolumeComponentType> GetVolumeComponentType()
                => new ArbitraryVolumeComponentType();
        }

        class ArbitraryType : Arbitrary<Type>
        {
            static Type[] s_Types = TestTypes.AllVolumeComponents.Take(20)
                .Union(new Type[] { null })
                .Union(k_DefaultTypes)
                .ToArray();

            public override Gen<Type> Generator => Gen.Elements(s_Types);
        }

        public static Arbitrary<Type> CreateTypeArbitrary() => new ArbitraryType();

        public static void Register()
        {
            Arb.Register<Arbitraries>();
        }
    }
}
