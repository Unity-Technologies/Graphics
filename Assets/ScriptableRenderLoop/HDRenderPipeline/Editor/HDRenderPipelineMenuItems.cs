using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEngine.Experimental.ScriptableRenderLoop;

namespace UnityEditor.Experimental.ScriptableRenderLoop
{
    public class HDRenderPipelineMenuItems
    {
        [UnityEditor.MenuItem("HDRenderPipeline/Create Scene Settings")]
        static void CreateSceneSettings()
        {
            CommonSettings[] settings = Object.FindObjectsOfType(typeof(CommonSettings)) as CommonSettings[];

            if (settings.Length == 0)
            {
                GameObject go = new GameObject();
                go.name = "SceneSettings";
                go.AddComponent(typeof(CommonSettings));
            }
            else
            {
                Debug.LogWarning("SceneSettings has already been created.");
            }
        }

        [UnityEditor.MenuItem("HDRenderPipeline/Synchronize all Layered materials")]
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
