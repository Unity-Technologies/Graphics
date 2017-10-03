using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SceneSettingsManagementWindow : EditorWindow
    {
        [MenuItem("HDRenderPipeline/Scene Settings Management")]
        static void SceneSettingsManagement()
        {
            GetWindow<SceneSettingsManagementWindow>().Show();
        }

        static private string m_LastCreationPath = "Assets";

        void CreateAsset<AssetType>(string assetName) where AssetType : ScriptableObject
        {
            string assetPath = EditorUtility.SaveFilePanel("Create new Asset", m_LastCreationPath, assetName, "asset");
            if (!string.IsNullOrEmpty(assetPath))
            {
                assetPath = assetPath.Substring(assetPath.LastIndexOf("Assets"));
                m_LastCreationPath = System.IO.Path.GetDirectoryName(assetPath);
                var instance = CreateInstance<AssetType>();
                AssetDatabase.CreateAsset(instance, assetPath);
            }
        }

        void OnGUI()
        {
            // Keep it there temporarily until it's back to an "engine" setting in the HDRenderPipeline asset.
            EditorGUILayout.Space();

            if (GUILayout.Button("Create new Common Settings"))
            {
                CreateAsset<CommonSettings>("NewCommonSettings");
            }

            if (GUILayout.Button("Create new HDRI Sky Settings"))
            {
                CreateAsset<HDRISkySettings>("NewHDRISkySettings");
            }

            if (GUILayout.Button("Create new Procedural Sky Settings"))
            {
                CreateAsset<ProceduralSkySettings>("NewProceduralSkySettings");
            }

            if (GUILayout.Button("Create new Ambient Occlusion Settings"))
            {
                CreateAsset<ScreenSpaceAmbientOcclusionSettings>("NewAmbientOcclusionSettings");
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Scene Settings"))
            {
                var manager = new GameObject();
                manager.name = "Scene Settings";
                manager.AddComponent<SceneSettings>();
            }
        }
    }
}
