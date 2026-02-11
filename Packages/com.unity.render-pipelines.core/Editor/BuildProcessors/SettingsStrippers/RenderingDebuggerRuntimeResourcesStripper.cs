using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderingDebuggerRuntimeResourcesStripper : IRenderPipelineGraphicsSettingsStripper<RenderingDebuggerRuntimeResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(RenderingDebuggerRuntimeResources settings) => !CoreBuildData.instance.developmentBuild;
    }
}
