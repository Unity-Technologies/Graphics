using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDRenderPipelineMenuItems
    {
        [MenuItem("HDRenderPipeline/Create Scene Settings")]
        static void CreateSceneSettings()
        {
            CommonSettings[] settings = Object.FindObjectsOfType<CommonSettings>();

            if (settings.Length == 0)
            {
                GameObject go = new GameObject { name = "SceneSettings" };
                go.AddComponent<CommonSettings>();
                go.AddComponent<PostProcessing>();
            }
            else
            {
                Debug.LogWarning("SceneSettings has already been created.");
            }
        }

        [MenuItem("HDRenderPipeline/Synchronize all Layered materials")]
        static void SynchronizeAllLayeredMaterial()
        {
            Object[] materials = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Object obj in materials)
            {
                Material mat = obj as Material;
                if(mat.shader.name == "HDRenderLoop/LayeredLit")
                {
                    LayeredLitGUI.SynchronizeAllLayers(mat);
                }
            }
        }
    }
}
