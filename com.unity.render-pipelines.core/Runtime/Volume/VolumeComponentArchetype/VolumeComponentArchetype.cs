using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manage a set of volume component types.
    ///
    /// The managed set can be a subset defined by a filter.
    ///
    /// Immutable type
    /// </summary>
    public sealed class VolumeComponentArchetype : IEquatable<VolumeComponentArchetype>
    {
        [ExcludeFromCodeCoverage]
        static class StaticMembers
        {
            public static readonly Dictionary<VolumeComponentDatabase, Dictionary<IFilter<VolumeComponentType>, VolumeComponentArchetype>> cacheByDatabaseByFilter
                = new Dictionary<VolumeComponentDatabase, Dictionary<IFilter<VolumeComponentType>, VolumeComponentArchetype>>();

            public static readonly Dictionary<HashSet<VolumeComponentType>, VolumeComponentArchetype> cacheByTypeSet =
                new Dictionary<HashSet<VolumeComponentType>, VolumeComponentArchetype>();
        }

        VolumeComponentType[] typeArray { get; }
        HashSet<VolumeComponentType> typeSet { get; }

        Dictionary<(Type factory, Type extension), VolumeComponentArchetypeExtension> m_Extensions
            = new Dictionary<(Type factory, Type extension), VolumeComponentArchetypeExtension>();

        VolumeComponentArchetype([DisallowNull] params VolumeComponentType[] typeArray)
        {
            typeSet = typeArray.ToHashSet();
            this.typeArray = typeSet.ToArray();
        }

        [return: NotNull]
        public static VolumeComponentArchetype FromTypes([DisallowNull] params VolumeComponentType[] types)
            => new VolumeComponentArchetype(types);

        [return: NotNull]
        public static VolumeComponentArchetype FromTypesCached([DisallowNull] params VolumeComponentType[] types)
        {
            using (HashSetPool<VolumeComponentType>.Get(out var typeSet))
            {
                typeSet.UnionWith(types);
                if (!StaticMembers.cacheByTypeSet.TryGetValue(typeSet, out var instance))
                {
                    instance = FromTypes(types);
                    StaticMembers.cacheByTypeSet.Add(new HashSet<VolumeComponentType>(typeSet), instance);
                }

                return instance;
            }
        }

        [NotNull]
        public static VolumeComponentArchetype Empty { get; } = new VolumeComponentArchetype();

        [return: NotNull]
        public static VolumeComponentArchetype FromFilterCached<TFilter>([DisallowNull] in TFilter filter, [AllowNull] VolumeComponentDatabase database = null)
            where TFilter : IFilter<VolumeComponentType>
        {
            database ??= VolumeComponentDatabase.memoryDatabase;

            if (!StaticMembers.cacheByDatabaseByFilter.TryGetValue(database, out var byFilter))
            {
                byFilter = new Dictionary<IFilter<VolumeComponentType>, VolumeComponentArchetype>();
                StaticMembers.cacheByDatabaseByFilter.Add(database, byFilter);
            }

            if (!byFilter.TryGetValue(filter, out var set))
            {
                var baseComponentTypeArray = database.componentTypes
                    .Where(filter.IsAccepted).ToArray();
                set = new VolumeComponentArchetype(baseComponentTypeArray);
                byFilter.Add(filter, set);
            }

            return set;
        }

        [return: NotNull]
        public static VolumeComponentArchetype FromIncludeExclude(
            [DisallowNull] IReadOnlyCollection<VolumeComponentArchetype> includes,
            [DisallowNull] IReadOnlyCollection<VolumeComponentArchetype> excludes)
        {
            using (HashSetPool<VolumeComponentType>.Get(out var included))
            using (HashSetPool<VolumeComponentType>.Get(out var excluded))
            {
                foreach (var include in includes)
                    included.UnionWith(include.typeSet);
                foreach (var exclude in excludes)
                    excluded.UnionWith(exclude.typeSet);

                included.ExceptWith(excluded);
                return new VolumeComponentArchetype(included.ToArray());
            }
        }

        [return: NotNull]
        public VolumeComponentType[] AsArray() => typeArray;
        public bool ContainsType(VolumeComponentType type) => typeSet.Contains(type);

        [ExcludeFromCodeCoverage] // Trivial
        public override string ToString()
        {
            return $"{nameof(VolumeComponentArchetype)}({typeArray.ToDebugString()})";
        }

        /// <summary>
        /// Adds an extension if it does not exists
        /// Get an extension it exists
        /// </summary>
        /// <param name="extension"></param>
        /// <typeparam name="TExtension"></typeparam>
        /// <typeparam name="TFactory"></typeparam>
        /// <returns></returns>
        [MustUseReturnValue]
        internal bool GetOrAddExtension<TExtension, TFactory>([NotNullWhen(true)] out TExtension extension)
            where TExtension : VolumeComponentArchetypeExtension
            where TFactory : struct, IVolumeComponentArchetypeExtensionFactory<TExtension>
        {
            if (GetExtension<TExtension, TFactory>(out extension))
                return true;

            extension = new TFactory().Create(this);
            m_Extensions.Add((typeof(TFactory), typeof(TExtension)), extension);
            return true;
        }

        [MustUseReturnValue]
        internal bool GetExtension<TExtension, TFactory>([NotNullWhen(true)] out TExtension extension)
            where TExtension : VolumeComponentArchetypeExtension
            where TFactory : struct, IVolumeComponentArchetypeExtensionFactory<TExtension>
        {
            if (m_Extensions.TryGetValue((typeof(TFactory), typeof(TExtension)), out var extensionBase))
            {
                extension = (TExtension)extensionBase;
                return true;
            }

            extension = default;
            return false;
        }

        public bool Equals(VolumeComponentArchetype other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return typeSet.SetEquals(other.typeSet);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is VolumeComponentArchetype other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (typeSet != null ? typeSet.Aggregate(0, (acc, v) => acc + v.AsType().GetHashCode()) : 0);
        }

        public static bool operator ==(VolumeComponentArchetype l, VolumeComponentArchetype r) => l?.Equals(r) ?? ReferenceEquals(null, r);
        public static bool operator !=(VolumeComponentArchetype l, VolumeComponentArchetype r) => !(l?.Equals(r) ?? ReferenceEquals(null, r));
    }
}
