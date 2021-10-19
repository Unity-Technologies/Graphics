using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manage a set of volume component types and their default state.
    ///
    /// The managed set can be a subset defined by a filter.
    ///
    /// Immutable type
    /// </summary>
    sealed class VolumeComponentSet
    {
        [System.Diagnostics.CodeAnalysis.NotNull]
        public Type[] baseComponentTypeArray { get; }
        VolumeComponent[] m_ComponentsDefaultState;

        List<IVolumeComponentSetExtension> m_Extensions;

        VolumeComponentSet([DisallowNull] Type[] baseComponentTypeArray, [DisallowNull] VolumeComponent[] componentsDefaultState)
        {
            Assert.IsNotNull(baseComponentTypeArray);
            Assert.IsNotNull(componentsDefaultState);
            Assert.AreEqual(baseComponentTypeArray.Length, componentsDefaultState.Length);

            m_ComponentsDefaultState = componentsDefaultState;
            this.baseComponentTypeArray = baseComponentTypeArray;
            m_Extensions = new List<IVolumeComponentSetExtension>();
        }

        [return: System.Diagnostics.CodeAnalysis.NotNull]
        public static VolumeComponentSet CreateSetFromFilter([DisallowNull] Func<Type, bool> filter)
        {
            var baseComponentTypeArray = VolumeComponentDatabase.baseComponentTypeArray
                .Where(filter).ToArray();
            var componentsDefaultState = baseComponentTypeArray
                .Select(type => (VolumeComponent)ScriptableObject.CreateInstance(type)).ToArray();
            return new VolumeComponentSet(baseComponentTypeArray, componentsDefaultState);
        }

        // Faster version of OverrideData to force replace values in the global state
        public void ReplaceData([DisallowNull] VolumeStack stack)
        {
            foreach (var component in m_ComponentsDefaultState)
            {
                var target = stack.GetComponent(component.GetType());
                int count = component.parameters.Count;

                for (int i = 0; i < count; i++)
                {
                    if (target.parameters[i] != null)
                    {
                        target.parameters[i].overrideState = false;
                        target.parameters[i].SetValue(component.parameters[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Adds an extension if it does not exists
        /// </summary>
        /// <param name="extension"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MustUseReturnValue]
        internal bool AddExtension<T>([NotNullWhen(true)] out T extension) where T : class, IVolumeComponentSetExtension, new()
        {
            extension = null;
            if (m_Extensions.Any(ext => ext.GetType() == typeof(T)))
                return false;

            extension = new T();
            extension.Initialize(this);
            m_Extensions.Add(extension);

            return true;
        }

        [MustUseReturnValue]
        internal bool GetExtension<T>([NotNullWhen(true)] out T extension) where T : class, IVolumeComponentSetExtension, new()
        {
            extension = m_Extensions.FirstOrDefault(ext => ext.GetType() == typeof(T)) as T;
            return extension != null;
        }
    }
}
