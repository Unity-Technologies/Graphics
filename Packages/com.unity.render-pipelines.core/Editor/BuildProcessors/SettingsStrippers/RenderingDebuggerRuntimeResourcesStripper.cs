using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderingDebuggerRuntimeResourcesStripper : IRenderPipelineGraphicsSettingsStripper<RenderingDebuggerRuntimeResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(RenderingDebuggerRuntimeResources settings)
        {
            // When building for any SRP, we assume that we support the Rendering Debugger
            // But, if we are building for retail builds, we strip those runtime UI resources from the final player
            if (!CoreBuildData.instance.buildingPlayerForRenderPipeline) return true;
            return !CoreBuildData.instance.isDevelopmentBuild;
        }
    }
}
