using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class FrameSettingsUI
    {
        public static readonly GUIContent frameSettingsHeaderContent = EditorGUIUtility.TrTextContent("Frame Settings Overrides", "Default values for Frame Settings are defined in the Global Settings asset located in Project Settings > Graphics > HDRP Global Settings.");

        public static readonly GUIContent renderingSettingsHeaderContent = EditorGUIUtility.TrTextContent("Rendering");
        public static readonly GUIContent lightSettingsHeaderContent = EditorGUIUtility.TrTextContent("Lighting");
        public static readonly GUIContent asyncComputeSettingsHeaderContent = EditorGUIUtility.TrTextContent("Asynchronous Compute Shaders");
        public static readonly GUIContent lightLoopSettingsHeaderContent = EditorGUIUtility.TrTextContent("Light Loop Debug");
    }
}
