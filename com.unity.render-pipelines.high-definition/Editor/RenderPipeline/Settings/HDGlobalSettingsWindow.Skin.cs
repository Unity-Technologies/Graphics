using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineGlobalSettings>;

    internal partial class HDGlobalSettingsPanelIMGUI
    {
        public class Styles
        {
            public const int labelWidth = 220;
            internal static GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.largeLabel) { richText = true, fontSize = 18, fixedHeight = 42 };
            internal static GUIStyle subSectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);

            internal static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Default Volume Profile Asset");
            internal static readonly GUIContent lookDevVolumeProfileLabel = EditorGUIUtility.TrTextContent("LookDev Volume Profile Asset");

            internal static readonly GUIContent frameSettingsLabel = EditorGUIUtility.TrTextContent("Frame Settings (Default Values)");
            internal static readonly GUIContent frameSettingsLabel_Camera = EditorGUIUtility.TrTextContent("Camera");
            internal static readonly GUIContent frameSettingsLabel_RTProbe = EditorGUIUtility.TrTextContent("Realtime Reflection");
            internal static readonly GUIContent frameSettingsLabel_BakedProbe = EditorGUIUtility.TrTextContent("Baked or Custom Reflection");
            internal static readonly GUIContent renderingSettingsHeaderContent = EditorGUIUtility.TrTextContent("Rendering");
            internal static readonly GUIContent lightSettingsHeaderContent = EditorGUIUtility.TrTextContent("Lighting");
            internal static readonly GUIContent asyncComputeSettingsHeaderContent = EditorGUIUtility.TrTextContent("Asynchronous Compute Shaders");
            internal static readonly GUIContent lightLoopSettingsHeaderContent = EditorGUIUtility.TrTextContent("Light Loop Debug");

            internal static readonly GUIContent volumeComponentsLabel = EditorGUIUtility.TrTextContent("Volume Profiles");
            internal static readonly GUIContent customPostProcessOrderLabel = EditorGUIUtility.TrTextContent("Custom Post Process Orders");

            internal static readonly GUIContent resourceLabel = EditorGUIUtility.TrTextContent("Resources");
            internal static readonly GUIContent renderPipelineResourcesContent = EditorGUIUtility.TrTextContent("Player Resources", "Set of resources that need to be loaded when creating stand alone");
            internal static readonly GUIContent renderPipelineRayTracingResourcesContent = EditorGUIUtility.TrTextContent("Ray Tracing Resources", "Set of resources that need to be loaded when using ray tracing");
            internal static readonly GUIContent renderPipelineEditorResourcesContent = EditorGUIUtility.TrTextContent("Editor Resources", "Set of resources that need to be loaded for working in editor");

            internal static readonly GUIContent generalSettingsLabel = EditorGUIUtility.TrTextContent("Miscellaneous");

            internal static readonly GUIContent layerNamesLabel = EditorGUIUtility.TrTextContent("Layers Names");
            internal static readonly GUIContent lightLayersLabel = EditorGUIUtility.TrTextContent("Light Layer Names", "When enabled, HDRP allocates memory for processing Light Layers. For deferred rendering, this allocation includes an extra render target in memory and extra cost. See the Quality Settings window to enable Light Layers on your Render pipeline asset.");
            internal static readonly GUIContent lightLayerName0 = EditorGUIUtility.TrTextContent("Light Layer 0", "The display name for Light Layer 0. This is purely cosmetic, and can be used to articulate intended use of Light Layer 0");
            internal static readonly GUIContent lightLayerName1 = EditorGUIUtility.TrTextContent("Light Layer 1", "The display name for Light Layer 1. This is purely cosmetic, and can be used to articulate intended use of Light Layer 1");
            internal static readonly GUIContent lightLayerName2 = EditorGUIUtility.TrTextContent("Light Layer 2", "The display name for Light Layer 2. This is purely cosmetic, and can be used to articulate intended use of Light Layer 2");
            internal static readonly GUIContent lightLayerName3 = EditorGUIUtility.TrTextContent("Light Layer 3", "The display name for Light Layer 3. This is purely cosmetic, and can be used to articulate intended use of Light Layer 3");
            internal static readonly GUIContent lightLayerName4 = EditorGUIUtility.TrTextContent("Light Layer 4", "The display name for Light Layer 4. This is purely cosmetic, and can be used to articulate intended use of Light Layer 4");
            internal static readonly GUIContent lightLayerName5 = EditorGUIUtility.TrTextContent("Light Layer 5", "The display name for Light Layer 5. This is purely cosmetic, and can be used to articulate intended use of Light Layer 5");
            internal static readonly GUIContent lightLayerName6 = EditorGUIUtility.TrTextContent("Light Layer 6", "The display name for Light Layer 6. This is purely cosmetic, and can be used to articulate intended use of Light Layer 6");
            internal static readonly GUIContent lightLayerName7 = EditorGUIUtility.TrTextContent("Light Layer 7", "The display name for Light Layer 7. This is purely cosmetic, and can be used to articulate intended use of Light Layer 7");

            internal static readonly GUIContent decalLayersLabel = EditorGUIUtility.TrTextContent("Decal Layer Names", "When enabled, HDRP allocates Shader variants and memory to the decals buffer and cluster decal. See the Quality Settings window to enable Decal Layers on your Render pipeline asset.");
            internal static readonly GUIContent decalLayerName0 = EditorGUIUtility.TrTextContent("Decal Layer 0", "The display name for Decal Layer 0. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 0");
            internal static readonly GUIContent decalLayerName1 = EditorGUIUtility.TrTextContent("Decal Layer 1", "The display name for Decal Layer 1. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 1");
            internal static readonly GUIContent decalLayerName2 = EditorGUIUtility.TrTextContent("Decal Layer 2", "The display name for Decal Layer 2. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 2");
            internal static readonly GUIContent decalLayerName3 = EditorGUIUtility.TrTextContent("Decal Layer 3", "The display name for Decal Layer 3. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 3");
            internal static readonly GUIContent decalLayerName4 = EditorGUIUtility.TrTextContent("Decal Layer 4", "The display name for Decal Layer 4. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 4");
            internal static readonly GUIContent decalLayerName5 = EditorGUIUtility.TrTextContent("Decal Layer 5", "The display name for Decal Layer 5. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 5");
            internal static readonly GUIContent decalLayerName6 = EditorGUIUtility.TrTextContent("Decal Layer 6", "The display name for Decal Layer 6. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 6");
            internal static readonly GUIContent decalLayerName7 = EditorGUIUtility.TrTextContent("Decal Layer 7", "The display name for Decal Layer 7. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 7");

            internal static readonly GUIContent shaderVariantLogLevelLabel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level logging in of shader variants information is outputted when a build is performed. Information appears in the Unity Console when the build finishes..");

            internal static readonly GUIContent lensAttenuationModeContentLabel = EditorGUIUtility.TrTextContent("Lens Attenuation Mode", "Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier.");

            internal static readonly GUIContent useDLSSCustomProjectIdLabel = EditorGUIUtility.TrTextContent("Use DLSS Custom Project Id", "Set to utilize a custom project Id for the NVIDIA Deep Learning Super Sampling extension.");
            internal static readonly GUIContent DLSSProjectIdLabel = EditorGUIUtility.TrTextContent("DLSS Custom Project Id", "The custom project ID string to utilize for the NVIDIA Deep Learning Super Sampling extension.");

            internal static readonly GUIContent diffusionProfileSettingsLabel = EditorGUIUtility.TrTextContent("Diffusion Profile Assets");

            internal static readonly string warningHdrpNotActive = "Project graphics settings do not refer to a HDRP Asset. Check the settings: Graphics > Scriptable Render Pipeline Settings, Quality > Render Pipeline Asset.";
            internal static readonly string warningGlobalSettingsMissing = "The Settings property does not contain a valid HDRP Global Settings asset. There might be issues in rendering. Select a valid HDRP Global Settings asset.";
            internal static readonly string infoGlobalSettingsMissing = "Select a HDRP Global Settings asset.";

            internal static readonly GUIContent newAssetButtonLabel = EditorGUIUtility.TrTextContent("New", "Create a HD Global Settings Asset in your default resource folder (defined in Wizard)");
            internal static readonly GUIContent cloneAssetButtonLabel = EditorGUIUtility.TrTextContent("Clone", "Clone a HD Global Settings Asset in your default resource folder (defined in Wizard)");
            internal static readonly GUIContent newVolumeProfileLabel = EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)");

            internal static readonly GUIContent resetButtonLabel = EditorGUIUtility.TrTextContent("Reset");
            internal static readonly GUIContent resetAllButtonLabel = EditorGUIUtility.TrTextContent("Reset All");
        }
    }
}
