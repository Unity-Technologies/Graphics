using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Manage what to display to the users
    /// </summary>
    internal static class VolumeComponentTreeUI
    {
        // what archetype to use when displaying an Add Volume Override menu
        internal static VolumeComponentArchetype displayedArchetype { get; private set; }

        static VolumeComponentTreeUI()
        {
            RenderPipelineManager.activeRenderPipelineTypeChanged += RenderPipelineManagerOnactiveRenderPipelineTypeChanged;
            RenderPipelineManagerOnactiveRenderPipelineTypeChanged();
        }

        static void RenderPipelineManagerOnactiveRenderPipelineTypeChanged()
        {
            var renderPipeline = RenderPipelineManager.currentPipeline?.GetType();
            displayedArchetype = VolumeComponentArchetype.FromFilter(new IsSupportedVolumeComponentFilter(renderPipeline));
        }
    }
}
