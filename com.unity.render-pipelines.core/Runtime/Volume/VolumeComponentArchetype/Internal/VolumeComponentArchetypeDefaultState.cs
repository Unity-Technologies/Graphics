using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manages the default data set of the volume components
    /// </summary>
    class VolumeComponentArchetypeDefaultState : VolumeComponentArchetypeExtension
    {
        public struct Factory : IVolumeComponentArchetypeExtensionFactory<VolumeComponentArchetypeDefaultState>
        {
            [return: System.Diagnostics.CodeAnalysis.NotNull]
            public VolumeComponentArchetypeDefaultState Create([DisallowNull] VolumeComponentArchetype volumeComponentArchetype)
            {
                var componentsDefaultState = volumeComponentArchetype.AsArray()
                    .Select(type => (VolumeComponent)ScriptableObject.CreateInstance(type.AsType())).ToArray();

                return new VolumeComponentArchetypeDefaultState(componentsDefaultState);
            }
        }
        VolumeComponent[] componentsDefaultState { get; }

        VolumeComponentArchetypeDefaultState(VolumeComponent[] componentsDefaultState)
        {
            this.componentsDefaultState = componentsDefaultState;
        }

        [MustUseReturnValue]
        public bool GetDefaultStateOf(
            [DisallowNull] VolumeComponentType type,
            [NotNullWhen(true)] out VolumeComponent instance,
            [NotNullWhen(false)] out Exception error
        )
        {
            foreach (var volumeComponent in componentsDefaultState)
            {
                if (volumeComponent.GetType() == type.AsType())
                {
                    instance = volumeComponent;
                    error = default;
                    return true;
                }
            }

            instance = default;
            error = new ArgumentException($"Type was not found in archetype {type.AsType().Name}");
            return false;
        }

        // Faster version of OverrideData to force replace values in the global state
        public void ReplaceData([DisallowNull] VolumeStack stack)
        {
            foreach (var component in componentsDefaultState)
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
}
