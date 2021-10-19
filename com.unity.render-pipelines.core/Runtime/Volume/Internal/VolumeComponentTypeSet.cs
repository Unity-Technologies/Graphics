using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manage a set of volume component types and their default state.
    ///
    /// The managed set can be a subset defined by a filter.
    ///
    /// Immutable type
    /// </summary>
    sealed class VolumeComponentTypeSet
    {
        Type[] baseComponentTypeArray { get; }
        HashSet<Type> baseComponentTypeSet { get; }

        Dictionary<(Type factory, Type extension), VolumeComponentTypeSetExtension> m_Extensions
            = new Dictionary<(Type factory, Type extension), VolumeComponentTypeSetExtension>();

        VolumeComponentTypeSet([DisallowNull] Type[] baseComponentTypeArray)
        {
            this.baseComponentTypeArray = baseComponentTypeArray;
            baseComponentTypeSet = baseComponentTypeArray.ToHashSet();
        }

        [return: NotNull]
        public static VolumeComponentTypeSet CreateSetFromFilter([DisallowNull] Func<Type, bool> filter)
        {
            var baseComponentTypeArray = VolumeComponentDatabase.baseComponentTypeArray
                .Where(filter).ToArray();
            return new VolumeComponentTypeSet(baseComponentTypeArray);
        }

        [return: NotNull]
        public Type[] AsArray() => baseComponentTypeArray;
        public bool ContainsType(Type type) => baseComponentTypeSet.Contains(type);

        /// <summary>
        /// Adds an extension if it does not exists
        /// </summary>
        /// <param name="extension"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MustUseReturnValue]
        internal bool AddExtension<TExtension, TFactory>([NotNullWhen(true)] out TExtension extension)
            where TExtension : VolumeComponentTypeSetExtension
            where TFactory : struct, IVolumeComponentTypeSetExtensionFactory<TExtension>
        {
            if (GetExtension<TExtension, TFactory>(out extension))
                return true;

            extension = new TFactory().Create(this);
            m_Extensions.Add((typeof(TFactory), typeof(TExtension)), extension);
            return true;
        }

        [MustUseReturnValue]
        internal bool GetExtension<TExtension, TFactory>([NotNullWhen(true)] out TExtension extension)
            where TExtension : VolumeComponentTypeSetExtension
            where TFactory : struct, IVolumeComponentTypeSetExtensionFactory<TExtension>
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
            where TExtension : VolumeComponentTypeSetExtension
            where TFactory : struct, IVolumeComponentTypeSetExtensionFactory<TExtension>
        {
            return GetExtension<TExtension, TFactory>(out extension) || AddExtension<TExtension, TFactory>(out extension);
        }
    }
}
