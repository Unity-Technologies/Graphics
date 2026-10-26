using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(URPDefaultVolumeProfileSettings))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class URPDefaultVolumeProfileSettingsPropertyDrawer : DefaultVolumeProfileSettingsPropertyDrawer
    {
        protected override GUIContent defaultVolumeProfileAssetLabel => L10n.TextContent("Default Profile",
            "Settings that will be applied project-wide to all Volumes by default when URP is active.", null, null);

        protected override GUIContent volumeInfoBoxLabel => L10n.TextContent(
            "The values in the Default Volume can be overridden by a Volume Profile assigned to URP asset and Volumes inside scenes.", null, null, null);

        protected override VisualElement CreateAssetFieldUI()
        {
            return DrawDefaultVolumeObjectField<UniversalRenderPipeline, UniversalRenderPipelineAsset, URPDefaultVolumeProfileSettings>();
        }

        public class URPDefaultVolumeProfileSettingsContextMenu : DefaultVolumeProfileSettingsContextMenu2<
            URPDefaultVolumeProfileSettings, UniversalRenderPipeline>
        {
            protected override string defaultVolumeProfilePath => "Assets/VolumeProfile_Default.asset";
        }
    }
}
