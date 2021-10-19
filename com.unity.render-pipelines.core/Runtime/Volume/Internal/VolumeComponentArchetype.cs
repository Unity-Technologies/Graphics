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
    public sealed class VolumeComponentArchetype
    {
        static Dictionary<IVolumeComponentFilter, VolumeComponentArchetype> s_Cache
            = new Dictionary<IVolumeComponentFilter, VolumeComponentArchetype>();

        Type[] typeArray { get; }
        HashSet<Type> typeSet { get; }

        Dictionary<(Type factory, Type extension), VolumeComponentArchetypeExtension> m_Extensions
            = new Dictionary<(Type factory, Type extension), VolumeComponentArchetypeExtension>();

        VolumeComponentArchetype([DisallowNull] Type[] typeArray)
        {
            this.typeArray = typeArray;
            typeSet = typeArray.ToHashSet();
        }

        [return: NotNull]
        public static VolumeComponentArchetype FromFilter<TFilter>([DisallowNull] in TFilter filter)
            where TFilter : IVolumeComponentFilter
        {
            if (!s_Cache.TryGetValue(filter, out var set))
            {
                var baseComponentTypeArray = VolumeComponentDatabase.baseComponentTypeArray
                    .Where(filter.IsAccepted).ToArray();
                set = new VolumeComponentArchetype(baseComponentTypeArray);
                s_Cache.Add(filter, set);
            }

            return set;
        }

        [return: NotNull]
        public Type[] AsArray() => typeArray;
        public bool ContainsType(Type type) => typeSet.Contains(type);

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
    }
}
