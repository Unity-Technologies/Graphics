using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


internal static class URP2DConverterUtility
{
    public static bool IsMaterialPath(string path, string id)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (path.EndsWith(".mat"))
            return URP2DConverterUtility.DoesFileContainString(path, id);

        return false;
    }

    public static bool IsPrefabOrScenePath(string path, string id)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            return URP2DConverterUtility.DoesFileContainString(path, id);

        return false;
    }

    public static bool DoesFileContainString(string path, string str)
    {
        if (str != null)
        {
            string file = File.ReadAllText(path);
            return file.Contains(str);
        }

        return false;
    }

    public static void UpgradePrefab(string path, Action<GameObject> objectUpgrader )
    {
        UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int objIndex = 0; objIndex < objects.Length; objIndex++)
        {
            GameObject go = objects[objIndex] as GameObject;
            if (go != null)
            {
                objectUpgrader(go);
                PrefabUtility.SavePrefabAsset(go);
            }
        }
    }

    public static void UpgradeScene(string path, Action<GameObject> objectUpgrader)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        GameObject[] gameObjects = scene.GetRootGameObjects();
        foreach (GameObject go in gameObjects)
            objectUpgrader(go);

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
    }

    public static void UpgradeMaterial(string path, Shader oldShader, Shader newShader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material.shader == oldShader)
            material.shader = newShader;
    }

    public static string GetObjectIDString(UnityEngine.Object obj)
    {
        string guid;
        long localId;
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj.GetInstanceID(), out guid, out localId))
            return "fileID: " + localId + ", guid: " + guid;

        return null;
    }
}
