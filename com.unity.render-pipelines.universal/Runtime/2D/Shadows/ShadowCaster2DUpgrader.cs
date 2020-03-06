
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class ShadowCaster2DUpgrader : MonoBehaviour
    {
        //=================================================================================================================================
        // Some of this was copied from the Renderer2DUpgrader. We need to share this code but I'm wating for an upgrader merge to happen.
        //=================================================================================================================================
        static Material s_SpriteLitDefault = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

        delegate void Upgrader<T>(T toUpgrade) where T : Object;

        static void ProcessAssetDatabaseObjects<T>(string searchString, Upgrader<T> upgrader) where T : Object
        {
            string[] prefabNames = AssetDatabase.FindAssets(searchString);
            foreach (string prefabName in prefabNames)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabName);
                if (path.StartsWith("Assets"))
                {
                    T obj = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (obj != null)
                    {
                        upgrader(obj);
                    }
                }
            }
        }
        //=================================================================================================================================

        static void UpgradeShadowCaster(ShadowCaster2D shadowCaster)
        {
            if (shadowCaster.mesh.colors == null)
            {
                Mesh mesh = new Mesh();
                ShadowUtility.GenerateShadowMesh(mesh, shadowCaster.shapePath);
                shadowCaster.mesh = mesh;
            }
        }

        public static void UpgradeShadowCasters()
        {
            // Find all objects in our scene and convert them...
            ShadowCaster2D[] shadowCasters = Object.FindObjectsOfType<ShadowCaster2D>();
            foreach(ShadowCaster2D shadowCaster in shadowCasters)
            {
                UpgradeShadowCaster(shadowCaster);
            }

            // Find all objects in our project and convert them
            ProcessAssetDatabaseObjects<ShadowCaster2D>("t: Prefab", UpgradeShadowCaster);
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }
    }
}


