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
            public static GUIContent Technique = L10n.TextContent("Technique", "This option determines what method is used for rendering decals.", null, null);
            public static GUIContent MaxDrawDistance = L10n.TextContent("Max Draw Distance", "Maximum global draw distance of decals.", null, null);
            public static GUIContent UseRenderingLayers = L10n.TextContent("Use Rendering Layers", "When enabled, you can configure specific Decal Projectors to affect only specific objects. The number of Rendering Layers affects the performance.", null, null);
            public static GUIContent SurfaceData = L10n.TextContent("Surface Data", "Allows specifying which decals surface data should be blended with surfaces.", null, null);
            public static GUIContent NormalBlend = L10n.TextContent("Normal Blend", "Controls the quality of normal reconstruction. The higher the value the more accurate normal reconstruction and the cost on performance.", null, null);
            public static readonly GUIContent OpenGlSupportWarning = L10n.TextContentWithIcon("Rendering Layers are not supported on OpenGL.", "This feature is incompatible with OpenGL. Disable 'Auto Graphics API' in Player Settings and remove OpenGL from the list.", MessageType.Warning, null);
        }

        private SerializedProperty m_Technique;
        private SerializedProperty m_MaxDrawDistance;
        private SerializedProperty m_DecalLayers;
        private SerializedProperty m_DBufferSettings;
        private SerializedProperty m_DBufferSurfaceData;
        private SerializedProperty m_ScreenSpaceSettings;
        private SerializedProperty m_ScreenSpaceNormalBlend;

        private bool m_IsInitialized = false;

        private void Init()
        {
            if (m_IsInitialized)
                return;
            SerializedProperty settings = serializedObject.FindProperty("m_Settings");
            m_Technique = settings.FindPropertyRelative("technique");
            m_MaxDrawDistance = settings.FindPropertyRelative("maxDrawDistance");
            m_DecalLayers = settings.FindPropertyRelative("decalLayers");
            m_DBufferSettings = settings.FindPropertyRelative("dBufferSettings");
            m_DBufferSurfaceData = m_DBufferSettings.FindPropertyRelative("surfaceData");
            m_ScreenSpaceSettings = settings.FindPropertyRelative("screenSpaceSettings");
            m_ScreenSpaceNormalBlend = m_ScreenSpaceSettings.FindPropertyRelative("normalBlend");
            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            Init();

            var gfxTypes = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);

            var isGLDevice = false;

            foreach (var type in gfxTypes)
            {
                if (type == GraphicsDeviceType.OpenGLES3 || type == GraphicsDeviceType.OpenGLCore)
                {
                    isGLDevice = true;
                    break;
                }
            }

            EditorGUILayout.PropertyField(m_Technique, Styles.Technique);

            DecalTechniqueOption technique = (DecalTechniqueOption)m_Technique.intValue;

            if (technique == DecalTechniqueOption.DBuffer)
            {
                if (isGLDevice)
                {
                    EditorGUILayout.HelpBox("DBuffer technique is not supported on OpenGL. Decals will not be rendered.", MessageType.Error);
                    EditorGUILayout.Space();
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DBufferSurfaceData, Styles.SurfaceData);
                EditorGUI.indentLevel--;
            }

            if (technique == DecalTechniqueOption.ScreenSpace)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ScreenSpaceNormalBlend, Styles.NormalBlend);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_MaxDrawDistance, Styles.MaxDrawDistance);

            if (isGLDevice)
            {
                if (m_DecalLayers.boolValue)
                {
                    m_DecalLayers.boolValue = false;
                    serializedObject.ApplyModifiedProperties();
                }

                GUI.enabled = false;
            }

            EditorGUILayout.PropertyField(m_DecalLayers, Styles.UseRenderingLayers);

            if (isGLDevice)
            {
                GUI.enabled = true;
                EditorGUILayout.HelpBox(Styles.OpenGlSupportWarning);
            }
        }
    }
}
