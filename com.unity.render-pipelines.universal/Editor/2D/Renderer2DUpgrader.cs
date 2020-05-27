using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    static class Renderer2DUpgrader
    {
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
            Renderer[] spriteRenderers = go.GetComponentsInChildren<Renderer>(true);
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if (data != null)
            {
                Material defaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);

                bool upgraded = false;
                foreach (Renderer renderer in spriteRenderers)
                {
                    int materialCount = renderer.sharedMaterials.Length;
                    Material[] newMaterials = new Material[materialCount];

                    for (int i = 0; i < materialCount; i++)
                    {
                        Material mat = renderer.sharedMaterials[i];

                        if (mat != null && mat.shader.name == "Sprites/Default")
                        {
                            newMaterials[i] = defaultMat;
                            upgraded = true;
                        }
                        else
                        {
                            newMaterials[i] = renderer.sharedMaterials[i];
                        }

                    }

                    if (upgraded)
                        renderer.sharedMaterials = newMaterials;
                }

                if (upgraded)
                {
                    Debug.Log(go.name + " was upgraded.", go);
                    EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }
        }

        static void UpgradeMaterial(Material mat)
        {
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if (data != null)
            {
                Material defaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);

                if (mat.shader.name == "Sprites/Default")
                {
                    mat.shader = defaultMat.shader;
                }
            }
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/2D Renderer/Upgrade Scene to 2D Renderer (Experimental)", false)]
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

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/2D Renderer/Upgrade Scene to 2D Renderer (Experimental)", true)]
        static bool UpgradeSceneTo2DRendererValidation()
        {
            return Light2DEditorUtility.IsUsing2DRenderer();
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/2D Renderer/Upgrade Project to 2D Renderer (Experimental)", false)]
        static void UpgradeProjectTo2DRenderer()
        {
            if (!EditorUtility.DisplayDialog("2D Renderer Upgrader", "The upgrade will search for all prefabs in your project that use Sprite Renderers and change the material references of those Sprite Renderers to a lit material. You can't undo this operation. It's highly recommended to backup your project before proceeding.", "Proceed", "Cancel"))
                return;

            ProcessAssetDatabaseObjects<GameObject>("t: Prefab", UpgradeGameObject);
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/2D Renderer/Upgrade Project to 2D Renderer (Experimental)", true)]
        static bool UpgradeProjectTo2DRendererValidation()
        {
            return Light2DEditorUtility.IsUsing2DRenderer();
        }
    }
}
