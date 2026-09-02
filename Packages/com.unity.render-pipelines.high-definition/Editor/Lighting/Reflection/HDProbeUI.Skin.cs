using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDProbeUI
    {
        static readonly GUIContent k_ProxyVolumeContent = L10n.TextContent("Proxy Volume", null, null, null);

        static readonly string k_NoProxyHelpBoxText = L10n.Tr("Influence shape will be used as Projection shape too.", null);
        static readonly string k_NoProxyInfiniteHelpBoxText = L10n.Tr("Projection will be at infinite.", null);
        static readonly string k_ProxyInfluenceShapeMismatchHelpBoxText = L10n.Tr("Proxy volume and influence volume have different shapes, this is not supported.", null);
        internal static readonly string k_UnsupportedPresetPropertiesMessage = L10n.Tr("Presets of HDRP Reflection Probe are not supported.", null);

        internal static readonly GUIContent k_ProxySettingsHeader = L10n.TextContent("Projection Settings", null, null, null);
        internal static readonly GUIContent k_InfluenceVolumeHeader = L10n.TextContent("Influence Volume", null, null, null);
        internal static readonly GUIContent k_CaptureSettingsHeader = L10n.TextContent("Capture Settings", null, null, null);
        internal static readonly GUIContent k_CustomSettingsHeader = L10n.TextContent("Render Settings", null, null, null);

        internal static readonly GUIContent k_BakeTypeContent = L10n.TextContent("Type",
            "'Baked' uses the 'Auto Baking' mode from the Lighting window. \n" +
            "If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n" +
            "'Custom' can be used if a custom capture is wanted. \n" +
            "'Realtime' can be used to dynamically re-render the capture during runtime (every frame).", null, null);
        internal static readonly GUIContent k_TimeSlicingContent = L10n.TextContent("Time Slicing",
            "Enable time slicing to distribute realtime updates over 7 frames (one for each cubemap face then one to process the result) instead of fully updating in a single frame.", null, null);
        internal static readonly GUIContent k_CustomTextureContent = L10n.TextContent("Texture", null, null, null);

        const string IconReflectionProbeGizmoId =
            "PreMatSphere"
        ;
    }
}
