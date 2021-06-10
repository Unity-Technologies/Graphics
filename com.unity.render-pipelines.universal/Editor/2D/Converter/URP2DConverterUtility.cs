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
            using (StreamReader file = File.OpenText(path))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(str))
                        return true;
                }
            }
        }

        return false;
    }

    public static string UpgradePrefab(string path, Action<GameObject> objectUpgrader)
    {
        UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);

        // There should be no need to check this as we have already determined that there is something that needs upgrading
        if (!PrefabUtility.IsPartOfImmutablePrefab(objects[0]))
        {
            for (int objIndex = 0; objIndex < objects.Length; objIndex++)
            {
                GameObject go = objects[objIndex] as GameObject;
                if (go != null)
                {
                    objectUpgrader(go);
                }
            }

            GameObject asset = objects[0] as GameObject;
            PrefabUtility.SavePrefabAsset(asset.transform.root.gameObject);

            return null;
        }

        return "Unable to modify an immutable prefab";
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

        GUID guid = AssetDatabase.GUIDFromAssetPath(path);
        AssetDatabase.SaveAssetIfDirty(guid);
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
