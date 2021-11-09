using System;
using System.Linq;
using System.Reflection;
using FsCheck;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    public static partial class ArbX
    {
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
            static Type[] s_Types = TestTypes.AllVolumeComponents
                .Union(new[] { null, typeof(int), typeof(uint), typeof(string), typeof(byte) })
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
