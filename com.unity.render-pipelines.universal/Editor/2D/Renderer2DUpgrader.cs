using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.Universal
{
    static class Renderer2DUpgrader
    {
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

        static void UpgradeGameObject(GameObject go)
        {
            SpriteRenderer[] spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>(true);

            bool upgraded = false;
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "Sprites/Default")
                {
                    renderer.sharedMaterial = s_SpriteLitDefault;
                    upgraded = true;
                }
            }

            if (upgraded)
            {
                Debug.Log(go.name + " was upgraded.", go);
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }

        static void UpgradeMaterial(Material mat)
        {
            if (mat.shader.name == "Sprites/Default")
            {
                mat.shader = s_SpriteLitDefault.shader;
            }
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/2D Renderer/Upgrade Scene to 2D Renderer (Experimental)")]
        static void UpgradeSceneTo2DRenderer()
        {
            if (!EditorUtility.DisplayDialog("2D Renderer Upgrader", "The upgrade will change the material references of Sprite Renderers in currently open scene(s) to a lit material. You can't undo this operation. Make sure you save the scene(s) before proceeding.", "Proceed", "Cancel"))
                return;

            GameObject[] gameObjects = Object.FindObjectsOfType<GameObject>();
            if (gameObjects != null && gameObjects.Length > 0)
            {
                foreach (GameObject go in gameObjects)
                {
                    UpgradeGameObject(go);
                }
            }
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/2D Renderer/Upgrade Project to 2D Renderer (Experimental)")]
        static void UpgradeProjectTo2DRenderer()
        {
            if (!EditorUtility.DisplayDialog("2D Renderer Upgrader", "The upgrade will search for all prefabs in your project that use Sprite Renderers and change the material references of those Sprite Renderers to a lit material. You can't undo this operation. It's highly recommended to backup your project before proceeding.", "Proceed", "Cancel"))
                return;

            ProcessAssetDatabaseObjects<GameObject>("t: Prefab", UpgradeGameObject);
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }
    }
}
