using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class FrameSettingsUI
    {
        static readonly GUIContent frameSettingsHeaderContent = EditorGUIUtility.TrTextContent("Frame Settings Overrides", "Default FrameSettings are defined in your Unity Project's HDRP Asset.");

        const string renderingSettingsHeaderContent = "Rendering";
        const string lightSettingsHeaderContent = "Lighting";
        const string asyncComputeSettingsHeaderContent = "Asynchronous Compute Shaders";
        const string lightLoopSettingsHeaderContent = "Light Loop Debug";
    }
}
