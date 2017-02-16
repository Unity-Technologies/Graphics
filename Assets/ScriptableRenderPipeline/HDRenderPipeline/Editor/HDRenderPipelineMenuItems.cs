using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipelineMenuItems
    {
        [MenuItem("HDRenderPipeline/Synchronize all Layered materials")]
        static void SynchronizeAllLayeredMaterial()
        {
            Object[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Object obj in materials)
            {
                Material mat = obj as Material;
                if (mat.shader.name == "HDRenderPipeline/LayeredLit" || mat.shader.name == "HDRenderPipeline/LayeredLitTessellation")
                {
                    LayeredLitGUI.SynchronizeAllLayers(mat);
                }
            }
        }

        [MenuItem("HDRenderPipeline/Create Scene Settings")]
        public static void CreateSceneSettings()
        {
            var allSettings = Object.FindObjectsOfType<SceneSettings>();
            var activeScene = SceneManager.GetActiveScene();
            foreach (SceneSettings setting in allSettings)
            {
                if (setting.gameObject.scene == activeScene)
                {
                    EditorUtility.DisplayDialog("Create Scene Settings", "Scene settings already exist on scene " + activeScene.name + ". \n\n If you want to create on another scene, set it active (double click) first, then select this option in the menu.", "Got It!");
                    return;
                }
            }

            var manager = new GameObject();
            manager.name = "Scene Settings";
            manager.AddComponent<SceneSettings>();
        }
    }
}
