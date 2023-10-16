using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPRayTracingResourcesStripper : IRenderPipelineGraphicsSettingsStripper<HDRPRayTracingResources>
    {
        public bool active => HDRPBuildData.instance.buildingPlayerForHDRenderPipeline;

        public bool CanRemoveSettings(HDRPRayTracingResources settings) => !HDRPBuildData.instance.playerNeedRaytracing;
    }
}
