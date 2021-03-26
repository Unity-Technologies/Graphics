using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class FrameSettingsUI
    {
        static readonly GUIContent frameSettingsHeaderContent = EditorGUIUtility.TrTextContent("Frame Settings Overrides", "Default values for Frame Settings are defined in the Global Settings asset located in Project Settings > Graphics > HDRP Settings.");

        const string renderingSettingsHeaderContent = "Rendering";
        const string lightSettingsHeaderContent = "Lighting";
        const string asyncComputeSettingsHeaderContent = "Asynchronous Compute Shaders";
        const string lightLoopSettingsHeaderContent = "Light Loop Debug";
    }
}
