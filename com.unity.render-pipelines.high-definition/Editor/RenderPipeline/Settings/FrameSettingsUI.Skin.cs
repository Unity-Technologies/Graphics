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
        
//forest-begin: customizable sorting flags
        static readonly GUIContent sortFlagsDepthPrepassContent = EditorGUIUtility.TrTextContent("Depth Prepass");
		static readonly GUIContent sortFlagsGBufferContent = EditorGUIUtility.TrTextContent("G-Buffer");
		static readonly GUIContent sortFlagsForwardContent = EditorGUIUtility.TrTextContent("Forward");
		static readonly GUIContent sortFlagsObjectMotionVectorsContent = EditorGUIUtility.TrTextContent("Object Motion Vectors");
//forest-end:
        const string lightLoopSettingsHeaderContent = "Light Loop Debug";
    }
}
