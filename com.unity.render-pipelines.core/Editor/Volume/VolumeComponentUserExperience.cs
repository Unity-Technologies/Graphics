using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    // Do not make this public
    // Ongoing refactor will provide a better way to define components
    // that can be added to a volume profile

    // High level API bound to static resource/API. Hard to test, but used API should be tested
    [ExcludeFromCodeCoverage]
    internal static class VolumeComponentUserExperience
    {
        static readonly VolumeComponentContext k_DisplayedContext = new VolumeComponentContext();

        [JetBrains.Annotations.NotNull]
        public static VolumeComponentArchetype displayedArchetype
        {
            get
            {
                // workaround: at domain reload, the current pipeline is not set
                //   so we must lazily check if it is the case to appropriately update
                //   the archetypes.
                OnPipelineChanged.UpdateDisplayContextIfRequired();
                return k_DisplayedContext.contextualArchetype;
            }
        }

        // High level API bound to static resource/API. Hard to test, but used API should be tested
        [ExcludeFromCodeCoverage]
        [InitializeOnLoad]
        static class OnPipelineChanged
        {
            static Type s_LastPipelineType;
            static VolumeComponentArchetype s_PreviousArchetype = null;

            static OnPipelineChanged()
            {
                RenderPipelineManager.activeRenderPipelineTypeChanged += OnPipelineChangedCallback;
                OnPipelineChangedCallback();
            }

            static void OnPipelineChangedCallback()
            {
                var renderPipeline = RenderPipelineManager.currentPipeline?.GetType();

                if (s_PreviousArchetype != null)
                {
                    k_DisplayedContext.RemoveIncludeArchetype(s_PreviousArchetype);
                    s_PreviousArchetype = null;
                }

                s_PreviousArchetype = VolumeComponentArchetype.FromFilterCached(IsExplicitlySupportedVolumeComponentFilter.FromType(renderPipeline));
                k_DisplayedContext.AddIncludeArchetype(s_PreviousArchetype);
                s_LastPipelineType = renderPipeline;
            }

            public static void UpdateDisplayContextIfRequired()
            {
                if (s_LastPipelineType != RenderPipelineManager.currentPipeline?.GetType())
                    OnPipelineChangedCallback();
            }
        }
    }
}
