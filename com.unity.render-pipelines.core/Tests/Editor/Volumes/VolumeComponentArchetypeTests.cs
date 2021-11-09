using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Rendering.Tests
{
    public static partial class ArbX
    {
        public partial class Arbitraries
        {
            public static Arbitrary<VolumeComponentArchetypeTests.GetAndAddExtensionsTestAction> CreateGetAndAddExtensionsTestActionArb()
                => new VolumeComponentArchetypeTests.GetAndAddExtensionsTestActionArb();
        }
    }

    public partial class VolumeComponentArchetypeTests
    {

        public readonly partial struct GetAndAddExtensionsTestAction
        {
            public enum Kind
            {
                Get,
                GetOrAdd,
            }

            readonly Kind m_Kind;
            readonly Type m_ExtensionType;

            public GetAndAddExtensionsTestAction(Kind kind, Type extensionType)
            {
                m_Kind = kind;
                m_ExtensionType = extensionType;
            }

            public void ApplyTo([DisallowNull] VolumeComponentArchetype archetype)
            {
                switch (m_Kind)
                {
                    case Kind.Get:
                        ApplyGetTo(archetype, m_ExtensionType);
                        break;
                    case Kind.GetOrAdd:
                        ApplyGetOrAddTo(archetype, m_ExtensionType);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private partial void ApplyGetTo([DisallowNull] VolumeComponentArchetype archetype, [DisallowNull] Type type);
            private partial void ApplyGetOrAddTo([DisallowNull] VolumeComponentArchetype archetype, [DisallowNull] Type type);

            public bool Check([DisallowNull] VolumeComponentArchetype archetype)
            {
                switch (m_Kind)
                {
                    case Kind.Get:
                        return CheckGetTo(archetype, m_ExtensionType);
                    case Kind.GetOrAdd:
                        return CheckGetOrAddTo(archetype, m_ExtensionType);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private partial bool CheckGetTo([DisallowNull] VolumeComponentArchetype archetype, [DisallowNull] Type type);
            private partial bool CheckGetOrAddTo([DisallowNull] VolumeComponentArchetype archetype, [DisallowNull] Type type);

        }

        internal class GetAndAddExtensionsTestActionArb : Arbitrary<GetAndAddExtensionsTestAction>
        {
            static Type[] s_Types = GeneratedExtensions.AllTypes;

            public override Gen<GetAndAddExtensionsTestAction> Generator => Gen.Elements(s_Types)
                .Zip(Gen.Choose(0, 1))
                .Select(tuple => new GetAndAddExtensionsTestAction((GetAndAddExtensionsTestAction.Kind)tuple.Item2, tuple.Item1));

        }
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
        public void GetAndAddExtensionsTest()
        {
            bool Property(VolumeComponentType[] types, GetAndAddExtensionsTestAction[] actions)
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);
                foreach (var action in actions)
                    action.ApplyTo(archetype);

                return actions.All(action => action.Check(archetype));
            }

            Prop.ForAll<VolumeComponentType[], GetAndAddExtensionsTestAction[]>(Property).QuickCheckThrowOnFailure();
        }


        [Test]
        public void EmptyIsEmpty()
        {
            Assert.AreEqual(0, VolumeComponentArchetype.Empty.AsArray().Length);
        }
    }
}
