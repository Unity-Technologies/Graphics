using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        static Dictionary<IFilter<VolumeComponentType>, VolumeComponentArchetype> s_CacheByFilter
            = new Dictionary<IFilter<VolumeComponentType>, VolumeComponentArchetype>();

        VolumeComponentType[] typeArray { get; }
        HashSet<VolumeComponentType> typeSet { get; }

        Dictionary<(Type factory, Type extension), VolumeComponentArchetypeExtension> m_Extensions
            = new Dictionary<(Type factory, Type extension), VolumeComponentArchetypeExtension>();

        VolumeComponentArchetype([DisallowNull] params VolumeComponentType[] typeArray)
        {
            this.typeArray = typeArray;
            typeSet = typeArray.ToHashSet();
        }

        [return: NotNull]
        public static VolumeComponentArchetype FromTypes([DisallowNull] params VolumeComponentType[] types)
            => new VolumeComponentArchetype(types);

        [NotNull]
        public static VolumeComponentArchetype Empty { get; } = new VolumeComponentArchetype();
        public static VolumeComponentArchetype Everything { get; } = FromFilter(new EverythingVolumeComponentFilter());

        [return: NotNull]
        public static VolumeComponentArchetype FromFilter<TFilter>([DisallowNull] in TFilter filter, [AllowNull] VolumeComponentDatabase database = null)
            where TFilter : IFilter<VolumeComponentType>
        {
            database ??= VolumeComponentDatabase.memoryDatabase;

            if (!s_CacheByFilter.TryGetValue(filter, out var set))
            {
                var baseComponentTypeArray = database.componentTypes
                    .Where(filter.IsAccepted).ToArray();
                set = new VolumeComponentArchetype(baseComponentTypeArray);
                s_CacheByFilter.Add(filter, set);
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

        public bool ContainsType([AllowNull] Type type)
        {
            return VolumeComponentType.FromType(type, out var volumeType) && ContainsType(volumeType);
        }

        /// <summary>
        /// Adds an extension if it does not exists
        /// </summary>
        /// <param name="extension"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MustUseReturnValue]
        internal bool AddExtension<TExtension, TFactory>([NotNullWhen(true)] out TExtension extension)
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

        [MustUseReturnValue]
        internal bool GetOrAddExtension<TExtension, TFactory>([NotNullWhen(true)] out TExtension extension)
            where TExtension : VolumeComponentArchetypeExtension
            where TFactory : struct, IVolumeComponentArchetypeExtensionFactory<TExtension>
        {
            return GetExtension<TExtension, TFactory>(out extension) || AddExtension<TExtension, TFactory>(out extension);
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
            return (typeSet != null ? typeSet.GetHashCode() : 0);
        }
    }
}
