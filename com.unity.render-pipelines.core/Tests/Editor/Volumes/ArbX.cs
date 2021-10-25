using System;
using System.Linq;
using System.Reflection;
using FsCheck;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    public static class ArbX
    {
        public class ArbitraryVolumeComponentType : Arbitrary<VolumeComponentType>
        {
            static VolumeComponentType[] Types = TypeCache.GetTypesDerivedFrom<VolumeComponent>()
                .Select(VolumeComponentType.FromTypeUnsafe)
                .ToArray();

            public override Gen<VolumeComponentType> Generator => Gen.Elements(Types);
        }

        public class Generators
        {
            public static Arbitrary<VolumeComponentType> GetVolumeComponentType()
                => new ArbitraryVolumeComponentType();
        }

        public class ArbitraryType : Arbitrary<Type>
        {
            static Type[] Types = TypeCache.GetTypesDerivedFrom<VolumeComponent>()
                .Take(20)
                .Union(new[] { null, typeof(int), typeof(uint), typeof(string), typeof(byte) })
                .Union(Assembly.GetAssembly(typeof(VolumeComponentTypeTests)).GetTypes().Take(20))
                .ToArray();

            public override Gen<Type> Generator => Gen.Elements(Types);
        }

        public static void Register()
        {
            Arb.Register<Generators>();
        }
    }
}
