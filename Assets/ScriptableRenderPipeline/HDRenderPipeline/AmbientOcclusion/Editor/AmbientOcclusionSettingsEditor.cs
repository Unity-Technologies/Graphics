using UnityEditor;
using System.IO;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(AmbientOcclusionSettings))]
    class AmbientOcclusionSettingsEditor : Editor
    {
        SerializedProperty m_intensity;
        SerializedProperty m_radius;
        SerializedProperty m_sampleCount;
        SerializedProperty m_downsampling;

        void OnEnable()
        {
            m_intensity = serializedObject.FindProperty("intensity");
            m_radius = serializedObject.FindProperty("radius");
            m_sampleCount = serializedObject.FindProperty("sampleCount");
            m_downsampling = serializedObject.FindProperty("downsampling");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_intensity);
            EditorGUILayout.PropertyField(m_radius);
            EditorGUILayout.PropertyField(m_sampleCount);
            EditorGUILayout.PropertyField(m_downsampling);

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("Assets/Create/Ambient Occlusion Settings")]
        static void CreateSettings()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(path), "");
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/AOSettings.asset");

            var asset = CreateInstance<AmbientOcclusionSettings>();
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
