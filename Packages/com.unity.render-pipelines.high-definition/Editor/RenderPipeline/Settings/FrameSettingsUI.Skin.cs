using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class FrameSettingsUI
    {
        public static readonly GUIContent frameSettingsHeaderContent = L10n.TextContent("Frame Settings Overrides", "Default values for Frame Settings are defined in the Global Settings asset located in Project Settings > Graphics > HDRP Global Settings.", null, null);

        public static readonly GUIContent renderingSettingsHeaderContent = L10n.TextContent("Rendering", null, null, null);
        public static readonly GUIContent lightSettingsHeaderContent = L10n.TextContent("Lighting", null, null, null);
        public static readonly GUIContent asyncComputeSettingsHeaderContent = L10n.TextContent("Asynchronous Compute Shaders", null, null, null);
        public static readonly GUIContent lightLoopSettingsHeaderContent = L10n.TextContent("Light Loop Debug", null, null, null);
    }
}
