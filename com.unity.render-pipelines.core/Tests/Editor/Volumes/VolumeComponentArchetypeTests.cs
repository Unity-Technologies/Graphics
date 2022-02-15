using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    using TSet = VolumeComponentTestDataSet;

    public partial class VolumeComponentArchetypeTests
    {
        static class Properties
        {
            public static bool AsArrayHasAllTypes(
                 VolumeComponentType[] types
            )
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);

                using (HashSetPool<VolumeComponentType>.Get(out var expectedTypes))
                {
                    expectedTypes.UnionWith(types);

                    return expectedTypes.SetEquals(archetype.AsArray());
                }
            }

            public static bool CanGetExistingExtension(
                VolumeComponentType[] types
            )
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);

                return archetype.GetOrAddDefaultState(out var extension)
                    && archetype.GetDefaultState(out var extension2)
                    && ReferenceEquals(extension, extension2);
            }

            public static bool FromEverything(
                VolumeComponentType[] types
                )
            {
                var database = VolumeComponentDatabase.FromTypes(types);
                var archetype = VolumeComponentArchetype.FromFilterCached(new EverythingVolumeComponentFilter(), database);

                using (HashSetPool<VolumeComponentType>.Get(out var expectedTypes))
                {
                    expectedTypes.UnionWith(types);

                    return expectedTypes.SetEquals(archetype.AsArray());
                }
            }

            public static bool FromIsExplicitlySupportedFilter(
                VolumeComponentType supportTarget,
                VolumeComponentType[] types
                )
            {
                var database = VolumeComponentDatabase.FromTypes(types);
                var archetype = VolumeComponentArchetype.FromFilterCached(
                    IsExplicitlySupportedVolumeComponentFilter.FromType(supportTarget.AsType()), database);

                using (HashSetPool<VolumeComponentType>.Get(out var expectedTypes))
                {
                    expectedTypes.UnionWith(types.Where(
                        type => IsSupportedOn.IsExplicitlySupportedBy(type.AsType(), supportTarget.AsType())));

                    return expectedTypes.SetEquals(archetype.AsArray());
                }
            }

            public static bool FromIncludeExclude(
                VolumeComponentType[][] includes,
                VolumeComponentType[][] excludes
            )
            {
                var includeArchetypes = includes.Select(VolumeComponentArchetype.FromTypes).ToList();
                var excludeArchetypes = excludes.Select(VolumeComponentArchetype.FromTypes).ToList();
                var archetype = VolumeComponentArchetype.FromIncludeExclude(includeArchetypes, excludeArchetypes);

                using (HashSetPool<VolumeComponentType>.Get(out var types))
                {
                    foreach (var include in includes)
                        types.UnionWith(include);
                    foreach (var exclude in excludes)
                        types.ExceptWith(exclude);

                    return types.SetEquals(archetype.AsArray());
                }
            }

            public static bool ContainsVolumeType(
                VolumeComponentType[] types,
                VolumeComponentType toLookFor
                )
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


            public static bool GetAndAddExtensionsTest(
                VolumeComponentType[] types,
                GetAndAddExtensionsTestAction[] actions
                )
            {
                var archetype = VolumeComponentArchetype.FromTypes(types);
                foreach (var action in actions)
                    action.ApplyTo(archetype);

                return actions.All(action => action.Check(archetype));
            }
        }

        [Test]
        public void AsArrayHasAllTypesProperty(
             [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))] VolumeComponentType[] types
        ) => Assert.True(Properties.AsArrayHasAllTypes(types));

        [Test]
        public void CanGetExistingExtensionProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.CanGetExistingExtension(types));

        [Test]
        public void FromEverythingProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.FromEverything(types));


        [Test]
        public void FromIsExplicitlySupportedFilterProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType supportTarget,
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types
        ) => Assert.True(Properties.FromIsExplicitlySupportedFilter(supportTarget, types));

        [Test]
        public void FromIncludeExcludeProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArrayArray))]
            VolumeComponentType[][] includes,
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArrayArray))]
            VolumeComponentType[][] excludes
        ) => Assert.True(Properties.FromIncludeExclude(includes, excludes));

        [Test]
        public static void ContainsVolumeTypeProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types,
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypes))]
            VolumeComponentType toLookFor
        ) => Assert.True(Properties.ContainsVolumeType(types, toLookFor));


        [Test]
        public void GetAndAddExtensionsTestProperty(
            [ValueSource(typeof(TSet), nameof(TSet.volumeComponentTypesArray))]
            VolumeComponentType[] types,
            [ValueSource(typeof(VolumeComponentArchetypeTests), nameof(k_GetAndAddExtensionsTestAction))]
            GetAndAddExtensionsTestAction[] actions
        ) => Assert.True(Properties.GetAndAddExtensionsTest(types, actions));

        // Helper to generate behaviour
        public readonly partial struct GetAndAddExtensionsTestAction
        {
            public enum Kind
            {
                Get,
                GetOrAdd,
            }

            readonly Kind m_Kind;
            readonly Type m_ExtensionType;

            public override string ToString() => $"{m_Kind}<{m_ExtensionType.Name}>";

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

        static readonly GetAndAddExtensionsTestAction[][] k_GetAndAddExtensionsTestAction = Enumerable.Range(0, 20)
            .RandomInitState(8318902)
            .Select(_ => GeneratedExtensions.AllTypes
                // Associate a generated action kind
                .Zip(Enumerable.Range(0, GeneratedExtensions.AllTypes.Length - 1)
                    .Select(_ => Random.Range(0, Enum.GetValues(typeof(GetAndAddExtensionsTestAction.Kind)).Length - 1)), (l,r) => (l,r))
                // Transform to GetAndAddExtensionsTestAction
                .Select(tuple => new GetAndAddExtensionsTestAction((GetAndAddExtensionsTestAction.Kind)tuple.Item2, tuple.Item1))
                .ToArray())
            .Take(3)
            .ToArray();


        [Test]
        public void EmptyIsEmpty()
        {
            Assert.AreEqual(0, VolumeComponentArchetype.Empty.AsArray().Length);
        }
    }
}
