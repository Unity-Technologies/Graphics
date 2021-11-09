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
        public void FromIncludeExclude()
        {
            bool Property(VolumeComponentType[][] includes, VolumeComponentType[][] excludes)
            {
                var includeArchetypes = includes.Select(VolumeComponentArchetype.FromTypes).ToList();
                var excludeArchetypes = excludes.Select(VolumeComponentArchetype.FromTypes).ToList();
                var archetype = VolumeComponentArchetype.FromIncludeExclude(includeArchetypes, excludeArchetypes);

                foreach (var types in includes)
                {
                    foreach (var type in types)
                    {
                        if (!archetype.ContainsType(type))
                            return false;
                    }
                }

                foreach (var types in excludes)
                {
                    foreach (var type in types)
                    {
                        if (archetype.ContainsType(type))
                            return false;
                    }
                }

                return true;
            }

            Prop.ForAll<VolumeComponentType[][], VolumeComponentType[][]>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void ContainsVolumeType()
        {
            bool Property(VolumeComponentType[] types, VolumeComponentType toLookFor)
            {
                using (HashSetPool<VolumeComponentType>.Get(out var set))
                {
                    set.UnionWith(types);

                    var archetype = VolumeComponentArchetype.FromTypes(types);
                    var value = archetype.ContainsType(toLookFor);
                    var expected = set.Contains(toLookFor);

                    return value == expected;
                }
            }

            Prop.ForAll<VolumeComponentType[], VolumeComponentType>(Property).QuickCheckThrowOnFailure();
        }

        [Test]
        public void EmptyIsEmpty()
        {
            Assert.AreEqual(0, VolumeComponentArchetype.Empty.AsArray().Length);
        }
    }
}
