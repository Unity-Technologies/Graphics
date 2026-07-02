using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceReflectionRendererFeature))]
    internal class ScreenSpaceReflectionRendererFeatureEditor : Editor
    {
        SerializedProperty m_AfterOpaque;

        public void OnEnable()
        {
            m_AfterOpaque = serializedObject.FindProperty(nameof(ScreenSpaceReflectionRendererFeature.afterOpaque));
        }

        public override void OnInspectorGUI()
        {
#if UNITY_WEBGL
            GraphicsDeviceType[] graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            if (Array.FindIndex(graphicsApis, x => x == GraphicsDeviceType.WebGPU) == -1)
                EditorGUILayout.HelpBox("WebGL is not supported for Screen Space Reflection.", MessageType.Warning);
#endif
            EditorGUILayout.PropertyField(m_AfterOpaque);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Additional settings for Screen Space Reflection can be found in the Screen Space Reflection volume override on your Volume Profile.", MessageType.Info);
        }
    }
}
