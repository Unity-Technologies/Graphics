using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(HDRPDefaultVolumeProfileSettings))]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class HDRPDefaultVolumeProfileSettingsPropertyDrawer : DefaultVolumeProfileSettingsPropertyDrawer
    {
        protected override GUIContent defaultVolumeProfileAssetLabel => L10n.TextContent("Default Profile",
            "Settings that will be applied project-wide to all Volumes by default when HDRP is active.", null, null);

        protected override GUIContent volumeInfoBoxLabel => L10n.TextContent(
            "The values in the Default Volume can be overridden by a Volume Profile assigned to HDRP asset and Volumes inside scenes.", null, null, null);

        protected override VisualElement CreateHeader()
        {
            var label = new Label("Default");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            return label;
        }

        protected override VisualElement CreateAssetFieldUI()
        {
            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            return DrawDefaultVolumeObjectField<HDRenderPipeline, HDRenderPipelineAsset, HDRPDefaultVolumeProfileSettings>(defaultVolumeProfileSettings);
        }

        public class HDRPDefaultVolumeProfileSettingsContextMenu : DefaultVolumeProfileSettingsContextMenu2<HDRPDefaultVolumeProfileSettings, HDRenderPipeline>
        {
            protected override string defaultVolumeProfilePath
            {
                get
                {
                    if (EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<HDRenderPipelineEditorAssets, HDRenderPipeline>(out var rpgs))
                        return VolumeUtils.GetDefaultNameForVolumeProfile(rpgs.defaultVolumeProfile);
                    return String.Empty;
                }
            }
        }
    }
}
