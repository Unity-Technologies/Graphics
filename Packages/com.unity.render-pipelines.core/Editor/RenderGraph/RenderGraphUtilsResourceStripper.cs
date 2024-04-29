using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace UnityEditor.Rendering.RenderGraphModule.Util
{
    class RenderGraphUtilsResourcesStripper : IRenderPipelineGraphicsSettingsStripper<RenderGraphUtilsResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(RenderGraphUtilsResources settings) => false;
    }
}
