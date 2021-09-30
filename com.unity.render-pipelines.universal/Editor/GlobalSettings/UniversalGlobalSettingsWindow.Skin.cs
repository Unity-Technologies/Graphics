using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalGlobalSettingsPanelIMGUI
    {
        internal class Styles
        {
            public const int labelWidth = 260;

            public static readonly GUIContent lightLayersLabel = EditorGUIUtility.TrTextContent("Light Layer Names (3D)", "If the Light Layers feature is enabled in the URP Asset, Unity allocates memory for processing Light Layers. In the Deferred Rendering Path, this allocation includes an extra render target in GPU memory, which reduces performance.");
            public static readonly GUIContent lightLayerName0 = EditorGUIUtility.TrTextContent("Light Layer 0", "The display name for Light Layer 0.");
            public static readonly GUIContent lightLayerName1 = EditorGUIUtility.TrTextContent("Light Layer 1", "The display name for Light Layer 1.");
            public static readonly GUIContent lightLayerName2 = EditorGUIUtility.TrTextContent("Light Layer 2", "The display name for Light Layer 2.");
            public static readonly GUIContent lightLayerName3 = EditorGUIUtility.TrTextContent("Light Layer 3", "The display name for Light Layer 3.");
            public static readonly GUIContent lightLayerName4 = EditorGUIUtility.TrTextContent("Light Layer 4", "The display name for Light Layer 4.");
            public static readonly GUIContent lightLayerName5 = EditorGUIUtility.TrTextContent("Light Layer 5", "The display name for Light Layer 5.");
            public static readonly GUIContent lightLayerName6 = EditorGUIUtility.TrTextContent("Light Layer 6", "The display name for Light Layer 6.");
            public static readonly GUIContent lightLayerName7 = EditorGUIUtility.TrTextContent("Light Layer 7", "The display name for Light Layer 7.");

            public static readonly GUIContent miscSettingsLabel = EditorGUIUtility.TrTextContent("Shader Stripping", "Shader Stripping settings");
            public static readonly GUIContent stripDebugVariantsLabel = EditorGUIUtility.TrTextContent("Strip Debug Variants", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");
            public static readonly GUIContent stripUnusedPostProcessingVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Post Processing Variants", "Controls whether strips automatically post processing shader variants based on VolumeProfile components. It strips based on VolumeProfiles in project and not scenes that actually uses it.");
            public static readonly GUIContent stripUnusedVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Variants", "Controls whether strip disabled keyword variants if the feature is enabled.");

            public static readonly string warningUrpNotActive = "Project graphics settings do not refer to a URP Asset. Check the settings: Graphics > Scriptable Render Pipeline Settings, Quality > Render Pipeline Asset.";
            public static readonly string warningGlobalSettingsMissing = "The Settings property does not contain a valid URP Global Settings asset. There might be issues in rendering. Select a valid URP Global Settings asset.";
            public static readonly string infoGlobalSettingsMissing = "Select a URP Global Settings asset.";

            public static readonly GUIContent newAssetButtonLabel = EditorGUIUtility.TrTextContent("New", "Create a URP Global Settings asset in the Assets folder.");
            public static readonly GUIContent cloneAssetButtonLabel = EditorGUIUtility.TrTextContent("Clone", "Clone a URP Global Settings asset in the Assets folder.");
            public static readonly GUIContent fixAssetButtonLabel = EditorGUIUtility.TrTextContent("Fix", "Ensure a URP Global Settings Asset is assigned.");
        }
    }
}
