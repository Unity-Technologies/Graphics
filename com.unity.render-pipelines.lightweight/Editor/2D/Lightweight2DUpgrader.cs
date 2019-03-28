using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

internal class Lightweight2DUpgrader : MonoBehaviour
{
    static Material s_SpriteLitDefault = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.lightweight/Runtime/Materials/Sprite-Lit-Default.mat");

    public delegate void Upgrader<T>(T toUpgrade) where T : Object;

    static void ProcessAssetDatabaseObjects<T>(string searchString, Upgrader<T> upgrader) where T : Object
    {
        string[] prefabNames = AssetDatabase.FindAssets(searchString);
        foreach (string prefabName in prefabNames)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabName);
            if (path.StartsWith("Assets"))
            {
                T obj = AssetDatabase.LoadAssetAtPath<T>(path);
                if(obj != null)
                {
                    upgrader(obj);
                }
            }
        }
    }

    static void UpgradeGameObject(GameObject go)
    {
        SpriteRenderer[] spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            renderer.sharedMaterial = s_SpriteLitDefault;
        }
    }

    static void UpgradeMaterial(Material mat)
    {
        if (mat.shader.name == "Sprites/Default")
        {
            mat.shader = s_SpriteLitDefault.shader;
        }
    }

    [MenuItem("Edit/Render Pipeline/2D Renderer/Upgrade Scene to 2D Renderer")]
    static void UpgradeSceneTo2DRenderer()
    {
        GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();
        if (gameObjects != null && gameObjects.Length > 0)
        {
            foreach (GameObject go in gameObjects)
            {
                UpgradeGameObject(go);
            }
        }
    }

    [MenuItem("Edit/Render Pipeline/2D Renderer/Upgrade Project to 2D Renderer")]
    static void UpgradeProjectTo2DRenderer()
    {
        ProcessAssetDatabaseObjects<GameObject>("t: Prefab", UpgradeGameObject);
        ProcessAssetDatabaseObjects<Material>("t: Material", UpgradeMaterial);
        AssetDatabase.SaveAssets();
    }
}
