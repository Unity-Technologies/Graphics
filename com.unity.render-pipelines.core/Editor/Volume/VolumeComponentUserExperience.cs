using System;
using JetBrains.Annotations;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    // Do not make this public
    // Ongoing refactor will provide a better way to define components
    // that can be added to a volume profile
    internal static class VolumeComponentUserExperience
    {
        static readonly VolumeComponentContext k_DisplayedContext = new VolumeComponentContext();

        [NotNull]
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

                s_PreviousArchetype = VolumeComponentArchetype.FromFilter(IsExplicitlySupportedVolumeComponentFilter.FromType(renderPipeline));
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
