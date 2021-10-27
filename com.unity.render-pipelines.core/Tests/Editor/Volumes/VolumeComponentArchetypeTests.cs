using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Rendering.Tests
{
    class VolumeComponentArchetypeTests
    {
        [OneTimeSetUp]
        public static void SetupFixture()
        {
            ArbX.Register();
        }

        [Test]
        public void AsArrayHasAllTypes()
        {
            bool Property(VolumeComponentType[] types)
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);

                using (HashSetPool<VolumeComponentType>.Get(out var expectedTypes))
                {
                    expectedTypes.UnionWith(types);

                    return expectedTypes.SetEquals(archetype.AsArray());
                }
            }

            Prop.ForAll<VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ContainsTypeIsValid()
        {
            bool Property(Type checkedType, VolumeComponentType[] types)
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);

                var contains = archetype.ContainsType(checkedType);
                var expectContains = types.Any(type => type.AsType() == checkedType);
                return contains == expectContains;
            }

            Prop.ForAll<Type, VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void FromEverything()
        {
            bool Property(VolumeComponentType[] types)
            {
                var database = VolumeComponentDatabase.FromTypes(types);
                var archetype = VolumeComponentArchetype.FromFilter(new EverythingVolumeComponentFilter(), database);

                using (HashSetPool<VolumeComponentType>.Get(out var expectedTypes))
                {
                    expectedTypes.UnionWith(types);

                    return expectedTypes.SetEquals(archetype.AsArray());
                }
            }

            Prop.ForAll<VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void FromIsExplicitlySupportedFilter()
        {
            bool Property(VolumeComponentType supportTarget, VolumeComponentType[] types)
            {
                var database = VolumeComponentDatabase.FromTypes(types);
                var archetype = VolumeComponentArchetype.FromFilter(
                    IsExplicitlySupportedVolumeComponentFilter.FromType(supportTarget.AsType()), database);

                using (HashSetPool<VolumeComponentType>.Get(out var expectedTypes))
                {
                    expectedTypes.UnionWith(types.Where(
                        type => IsSupportedOn.IsExplicitlySupportedBy(type.AsType(), supportTarget.AsType())));

                    return expectedTypes.SetEquals(archetype.AsArray());
                }
            }

            Prop.ForAll<VolumeComponentType, VolumeComponentType[]>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void EmptyIsEmpty()
        {
            Assert.AreEqual(0, VolumeComponentArchetype.Empty.AsArray().Length);
        }
    }
}
