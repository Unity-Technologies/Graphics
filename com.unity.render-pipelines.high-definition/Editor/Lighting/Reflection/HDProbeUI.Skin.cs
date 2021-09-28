using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDProbeUI
    {
        static readonly GUIContent k_ProxyVolumeContent = EditorGUIUtility.TrTextContent("Proxy Volume");

        static readonly string k_NoProxyHelpBoxText = L10n.Tr("Influence shape will be used as Projection shape too.");
        static readonly string k_NoProxyInfiniteHelpBoxText = L10n.Tr("Projection will be at infinite.");
        static readonly string k_ProxyInfluenceShapeMismatchHelpBoxText = L10n.Tr("Proxy volume and influence volume have different shapes, this is not supported.");
        internal static readonly string k_UnsupportedPresetPropertiesMessage = L10n.Tr("Presets of HDRP Reflection Probe are not supported.");

        internal static readonly GUIContent k_ProxySettingsHeader = EditorGUIUtility.TrTextContent("Projection Settings");
        internal static readonly GUIContent k_InfluenceVolumeHeader = EditorGUIUtility.TrTextContent("Influence Volume");
        internal static readonly GUIContent k_CaptureSettingsHeader = EditorGUIUtility.TrTextContent("Capture Settings");
        internal static readonly GUIContent k_CustomSettingsHeader = EditorGUIUtility.TrTextContent("Render Settings");

        internal static readonly GUIContent k_BakeTypeContent = EditorGUIUtility.TrTextContent("Type",
            "'Baked' uses the 'Auto Baking' mode from the Lighting window. \n" +
            "If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n" +
            "'Custom' can be used if a custom capture is wanted. \n" +
            "'Realtime' can be used to dynamically re-render the capture during runtime (every frame).");
        internal static readonly GUIContent k_CustomTextureContent = EditorGUIUtility.TrTextContent("Texture");

        static readonly Dictionary<ToolBar, GUIContent> k_ToolbarContents = new Dictionary<ToolBar, GUIContent>
        {
            { ToolBar.InfluenceShape,  EditorGUIUtility.TrIconContent("EditCollider", "Modify the base shape.") },
            { ToolBar.Blend,  EditorGUIUtility.TrIconContent("PreMatCube", "Modify the influence volume.") },
            { ToolBar.NormalBlend,  EditorGUIUtility.TrIconContent("SceneViewOrtho", "Modify the influence normal volume.") },
            { ToolBar.CapturePosition,  EditorGUIUtility.TrIconContent("MoveTool", "Change the capture position.") },
            { ToolBar.MirrorPosition,  EditorGUIUtility.TrIconContent("MoveTool", "Change the mirror position.") },
            { ToolBar.MirrorRotation,  EditorGUIUtility.TrIconContent("RotateTool", "Change the mirror rotation.") },
            { ToolBar.ShowChromeGizmo,  EditorGUIUtility.TrIconContent(IconReflectionProbeGizmoId, "Display the chrome gizmo.") },
        };

        const string IconReflectionProbeGizmoId =
#if UNITY_2019_3_OR_NEWER
            "PreMatSphere"
#else
            "ReflectionProbe Gizmo"
#endif
        ;
    }
}
