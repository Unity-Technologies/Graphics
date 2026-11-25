#if VOLUMETRIC_FOG

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(FogVolume))]
    [CanEditMultipleObjects]
    sealed class FogVolumeEditor : Editor
    {
        static readonly Color k_GizmoColor = new Color(180.0f / 255.0f, 180.0f / 255.0f, 180.0f / 255.0f).gamma;

        static class Styles
        {
            public static readonly GUIContent densityMode = EditorGUIUtility.TrTextContent("Density Mode", "How fog density is distributed inside the fog volume.");
            public static readonly GUIContent density = EditorGUIUtility.TrTextContent("Density", "How thick the fog is inside the fog volume. Higher values reduce visibility.");
            public static readonly GUIContent albedo = EditorGUIUtility.TrTextContent("Albedo", "The color of the light that the fog scatters.");
            public static readonly GUIContent densityTexture = EditorGUIUtility.TrTextContent("Density Texture", "A 3D texture whose RGB channels tint the albedo and whose alpha channel scales the density. Alpha-only textures only scale the density.");
            public static readonly GUIContent densityTextureTiling = EditorGUIUtility.TrTextContent("Tiling", "How many times the density texture repeats across the fog volume along each axis.");
            public static readonly GUIContent densityTextureScrollSpeed = EditorGUIUtility.TrTextContent("Scroll Speed", "How fast the density texture moves through the fog volume along each axis, in texture repeats per second.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How this fog volume combines with the global fog and overlapping fog volumes.");
            public static readonly GUIContent blendPriority = EditorGUIUtility.TrTextContent("Blend Priority", "Overlapping fog volumes blend in order, lowest to highest.");
        }

        SerializedProperty m_DensityMode;
        SerializedProperty m_Density;
        SerializedProperty m_Albedo;
        SerializedProperty m_DensityTexture;
        SerializedProperty m_DensityTextureTiling;
        SerializedProperty m_DensityTextureScrollSpeed;
        SerializedProperty m_BlendMode;
        SerializedProperty m_BlendPriority;

        void OnEnable()
        {
            m_DensityMode = serializedObject.FindProperty("m_DensityMode");
            m_Density = serializedObject.FindProperty("m_Density");
            m_Albedo = serializedObject.FindProperty("m_Albedo");
            m_DensityTexture = serializedObject.FindProperty("m_DensityTexture");
            m_DensityTextureTiling = serializedObject.FindProperty("m_DensityTextureTiling");
            m_DensityTextureScrollSpeed = serializedObject.FindProperty("m_DensityTextureScrollSpeed");
            m_BlendMode = serializedObject.FindProperty("m_BlendMode");
            m_BlendPriority = serializedObject.FindProperty("m_BlendPriority");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DensityMode, Styles.densityMode);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_Density, Styles.density);
                EditorGUILayout.PropertyField(m_Albedo, Styles.albedo);

                if (!m_DensityMode.hasMultipleDifferentValues &&
                    m_DensityMode.intValue == (int)FogVolumeDensityMode.Texture)
                {
                    TextureParameterDrawer.TextureField(m_DensityTexture, Styles.densityTexture, TextureDimension.Tex3D);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(m_DensityTextureTiling, Styles.densityTextureTiling);
                        EditorGUILayout.PropertyField(m_DensityTextureScrollSpeed, Styles.densityTextureScrollSpeed);
                    }
                }

                EditorGUILayout.PropertyField(m_BlendMode, Styles.blendMode);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_BlendPriority, Styles.blendPriority);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/Rendering/Fog Volume", priority = CoreUtils.Priorities.gameObjectMenuPriority)]
        static void CreateFogVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("Fog Volume", menuCommand.context);
            go.AddComponent<FogVolume>();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmosSelected(FogVolume fogVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(k_GizmoColor, fogVolume.transform.localToWorldMatrix))
            {
                Handles.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }
}

#endif
