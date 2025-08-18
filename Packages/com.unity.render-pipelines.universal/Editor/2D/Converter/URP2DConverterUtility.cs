using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using static UnityEditor.Rendering.AnimationClipUpgrader;

internal static class URP2DConverterUtility
{

    private static class Styles
    {
        public static readonly GUIContent failedToInstallPackageTitle = EditorGUIUtility.TrTextContent("Installation Failed");
        public static readonly GUIContent failedToInstallPackageContent = EditorGUIUtility.TrTextContent("Failed to install {0} package.\nErrorCode: {1}\nMessage: {2}");
        public static readonly GUIContent okText = EditorGUIUtility.TrTextContent("OK");
    }

    public static bool InstallPackage(string package)
    {
        var addRequest = UnityEditor.PackageManager.Client.Add(package);
        while (!addRequest.IsCompleted)
            System.Threading.Thread.Sleep(10);

        if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Failure)
        {
            var message = System.String.Format(Styles.failedToInstallPackageContent.text, package, addRequest.Error.errorCode, addRequest.Error.message);
            EditorUtility.DisplayDialog(Styles.failedToInstallPackageTitle.text, message, Styles.okText.text);
        }
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        return addRequest.Status == UnityEditor.PackageManager.StatusCode.Success;
    }

    public static bool IsPSB(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (path.StartsWith("Packages"))
            return false;

        return path.EndsWith(".psb") || path.EndsWith(".psd");
    }


    public static bool IsMaterialPath(string path, string[] ids)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (path.StartsWith("Packages"))
            return false;

        if (path.EndsWith(".mat"))
        {
            return URP2DConverterUtility.DoesFileContainString(path, ids);
        }
            

        return false;
    }

    public static bool IsPrefabOrScenePath(string path, string[] ids)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (path.StartsWith("Packages"))
            return false;

        if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            return URP2DConverterUtility.DoesFileContainString(path, ids);

        return false;
    }

    public static bool IsPrefabOrScenePath(string path, string id)
    {
        return IsPrefabOrScenePath(path, new string[] { id });
    }

    public static bool DoesFileContainString(string path, string[] strs)
    {
        if (strs != null && strs.Length > 0)
        {
            using (StreamReader file = File.OpenText(path))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    for (int i = 0; i < strs.Length; i++)
                    {
                        if (line.Contains(strs[i]))
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public static string UpgradePSB(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        return string.Empty;
    }

    public static string UpgradePrefab(string path, Action<GameObject> objectUpgrader)
    {
        UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);

        int firstIndex = 0;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] as GameObject)
            {
                firstIndex = i;
                break;
            }
        }

        // There should be no need to check this as we have already determined that there is something that needs upgrading
        if (!PrefabUtility.IsPartOfImmutablePrefab(objects[firstIndex]))
        {
            for (int objIndex = 0; objIndex < objects.Length; objIndex++)
            {
                GameObject go = objects[objIndex] as GameObject;
                if (go != null)
                {
                    objectUpgrader(go);
                }
            }

            GameObject asset = objects[firstIndex] as GameObject;
            PrefabUtility.SavePrefabAsset(asset.transform.root.gameObject);

            return string.Empty;
        }

        return "Unable to modify an immutable prefab";
    }

    public static void UpgradeScene(string path, Action<GameObject> objectUpgrader)
    {
        Scene scene = new Scene();
        bool openedByUser = false;
        for (int i = 0; i < SceneManager.sceneCount && !openedByUser; i++)
        {
            scene = SceneManager.GetSceneAt(i);
            if (path == scene.path)
                openedByUser = true;
        }

        if (!openedByUser)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        GameObject[] gameObjects = scene.GetRootGameObjects();
        foreach (GameObject go in gameObjects)
            objectUpgrader(go);

        EditorSceneManager.SaveScene(scene);
        if (!openedByUser)
            EditorSceneManager.CloseScene(scene, true);
    }

    public static void UpgradeMaterial(string path, Action<Material> materialUpgrader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        materialUpgrader(material);

        GUID guid = AssetDatabase.GUIDFromAssetPath(path);
        AssetDatabase.SaveAssetIfDirty(guid);
    }

    public static string GetObjectIDString(UnityEngine.Object obj)
    {
        string guid;
        long localId;
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj.GetEntityId(), out guid, out localId))
            return "fileID: " + localId + ", guid: " + guid;

        return null;
    }
}
