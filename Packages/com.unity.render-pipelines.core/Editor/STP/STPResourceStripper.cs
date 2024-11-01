using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class STPResourceStripper : IRenderPipelineGraphicsSettingsStripper<STP.RuntimeResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(STP.RuntimeResources resources)
        {
            bool isStpUsed = false;

            foreach (var asset in CoreBuildData.instance.renderPipelineAssets)
            {
                if (asset is ISTPEnabledRenderPipeline stpEnabledAsset)
                    isStpUsed |= stpEnabledAsset.isStpUsed;
            }

            // We can strip STP's resources if it's not used by any pipeline assets
            return !isStpUsed;
        }
    }
}
