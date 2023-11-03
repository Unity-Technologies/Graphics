using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class GPUResidentDrawerResourcesStripper : IRenderPipelineGraphicsSettingsStripper<GPUResidentDrawerResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(GPUResidentDrawerResources settings) => !CoreBuildData.instance.pipelineSupportGPUResidentDrawer || !CoreBuildData.instance.playerNeedGPUResidentDrawer;
    }
}