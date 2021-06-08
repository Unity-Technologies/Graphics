using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(DecalRendererFeature))]
    internal class DecalSettings : Editor
    {
        private struct Styles
        {
            public static GUIContent Technique = EditorGUIUtility.TrTextContent("Technique", "This option determines what method is used for rendering decals.");
            public static GUIContent MaxDrawDistance = EditorGUIUtility.TrTextContent("Max Draw Distance", "Maximum global draw distance of decals.");
            public static GUIContent SurfaceData = EditorGUIUtility.TrTextContent("Surface Data", "Allows specifying which decals surface data should be blended with surfaces.");
            public static GUIContent NormalBlend = EditorGUIUtility.TrTextContent("Normal Blend", "Controls the quality of normal reconstruction. The higher the value the more accurate normal reconstruction and the cost on performance.");
            public static GUIContent UseGBuffer = EditorGUIUtility.TrTextContent("Use GBuffer", "Uses traditional GBuffer decals, if renderer is set to deferred. Support only base color, normal and emission. Ignored when using forward rendering.");
        }

        private SerializedProperty m_Technique;
        private SerializedProperty m_MaxDrawDistance;
        private SerializedProperty m_DBufferSettings;
        private SerializedProperty m_DBufferSurfaceData;
        private SerializedProperty m_ScreenSpaceSettings;
        private SerializedProperty m_ScreenSpaceNormalBlend;
        private SerializedProperty m_ScreenSpaceUseGBuffer;

        private bool m_IsInitialized = false;

        private void Init()
        {
            if (m_IsInitialized)
                return;
            SerializedProperty settings = serializedObject.FindProperty("m_Settings");
            m_Technique = settings.FindPropertyRelative("technique");
            m_MaxDrawDistance = settings.FindPropertyRelative("maxDrawDistance");
            m_DBufferSettings = settings.FindPropertyRelative("dBufferSettings");
            m_DBufferSurfaceData = m_DBufferSettings.FindPropertyRelative("surfaceData");
            m_ScreenSpaceSettings = settings.FindPropertyRelative("screenSpaceSettings");
            m_ScreenSpaceNormalBlend = m_ScreenSpaceSettings.FindPropertyRelative("normalBlend");
            m_ScreenSpaceUseGBuffer = m_ScreenSpaceSettings.FindPropertyRelative("useGBuffer");
            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            Init();

            ValidateGraphicsApis();

            EditorGUILayout.PropertyField(m_Technique, Styles.Technique);

            DecalTechniqueOption technique = (DecalTechniqueOption)m_Technique.intValue;

            if (technique == DecalTechniqueOption.DBuffer)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DBufferSurfaceData, Styles.SurfaceData);
                EditorGUI.indentLevel--;
            }

            if (technique == DecalTechniqueOption.ScreenSpace)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ScreenSpaceNormalBlend, Styles.NormalBlend);
                EditorGUILayout.PropertyField(m_ScreenSpaceUseGBuffer, Styles.UseGBuffer);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_MaxDrawDistance, Styles.MaxDrawDistance);
        }

        private void ValidateGraphicsApis()
        {
            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);

            if (System.Array.FindIndex(graphicsAPIs, element => element == GraphicsDeviceType.OpenGLES2) >= 0)
            {
                EditorGUILayout.HelpBox("Decals are not supported with OpenGLES2.", MessageType.Warning);
            }
        }
    }
}
