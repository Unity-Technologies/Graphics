using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manages the default data set of the volume components
    /// </summary>
    class VolumeComponentTypeSetDefaultState : VolumeComponentTypeSetExtension
    {
        public struct Factory : IVolumeComponentTypeSetExtensionFactory<VolumeComponentTypeSetDefaultState>
        {
            public VolumeComponentTypeSetDefaultState Create(VolumeComponentTypeSet volumeComponentTypeSet)
            {
                var componentsDefaultState = volumeComponentTypeSet.AsArray()
                    .Select(type => (VolumeComponent)ScriptableObject.CreateInstance(type)).ToArray();

                return new VolumeComponentTypeSetDefaultState(componentsDefaultState);
            }
        }
        VolumeComponent[] m_ComponentsDefaultState { get; }

        VolumeComponentTypeSetDefaultState(VolumeComponent[] componentsDefaultState)
        {
            m_ComponentsDefaultState = componentsDefaultState;
        }

        // Faster version of OverrideData to force replace values in the global state
        public void ReplaceData([DisallowNull] VolumeStack stack)
        {
            foreach (var component in m_ComponentsDefaultState)
            {
                var target = stack.GetComponent(component.GetType());
                var count = component.parameters.Count;

                for (var i = 0; i < count; i++)
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

    static class VolumeComponentTypeSetDefaultStateExtension
    {
        public static bool GetOrAddDefaultState(
            [DisallowNull] this VolumeComponentTypeSet typeSet,
            [NotNullWhen(true)] out VolumeComponentTypeSetDefaultState extension)
            => typeSet.GetOrAddExtension<VolumeComponentTypeSetDefaultState, VolumeComponentTypeSetDefaultState.Factory>(out extension);
    }
}
