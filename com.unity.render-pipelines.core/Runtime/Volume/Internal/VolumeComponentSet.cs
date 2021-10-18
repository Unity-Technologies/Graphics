using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manage a set of volume component types and their default state.
    /// </summary>
    class VolumeComponentSet
    {
        public Type[] baseComponentTypeArray { get; }
        VolumeComponent[] m_ComponentsDefaultState;

        VolumeComponentSet([DisallowNull] Type[] baseComponentTypeArray, [DisallowNull] VolumeComponent[] componentsDefaultState)
        {
            Assert.IsNotNull(baseComponentTypeArray);
            Assert.IsNotNull(componentsDefaultState);
            Assert.AreEqual(baseComponentTypeArray.Length, componentsDefaultState.Length);

            m_ComponentsDefaultState = componentsDefaultState;
            this.baseComponentTypeArray = baseComponentTypeArray;
        }

        [return: NotNull]
        public static VolumeComponentSet CreateSetFromFilter([DisallowNull] Func<Type, bool> filter)
        {
            var baseComponentTypeArray = VolumeComponentDatabase.baseComponentTypeArray
                .Where(filter).ToArray();
            var componentsDefaultState = baseComponentTypeArray
                .Select(type => (VolumeComponent)ScriptableObject.CreateInstance(type)).ToArray();
            return new VolumeComponentSet(baseComponentTypeArray, componentsDefaultState);
        }

        // Faster version of OverrideData to force replace values in the global state
        public void ReplaceData(VolumeStack stack)
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
    }
}
