# Upgrading to SRP Core package version 13

This page describes how to upgrade from an older version of the SRP Core package to version 13.

## Upgrading from SRP Core version 12

Consider the following changes when upgrading from SRP Core version 12.

### Changes to the type of the Priority property on Volume components

In SRP Core version 13, the type of the Priority property in Volume components is changed from `float` to `int`.

After installing SRP Core version 13 in your project, the first time Unity loads a Volume component, it converts the the Priority value from `float` to `int`. Unity multiplies the `float` value by 1000 and runs the [Mathf.RoundToInt](https://docs.unity3d.com/ScriptReference/Mathf.RoundToInt.html) method on it.

Unity performs this process once for each Volume component.

In rare cases, the rounding might lead to situations when two Volume components have the same values in the Priority field. In that case you have to change the Priority values on those components manually.

If your project is using [AssetBundles](https://docs.unity3d.com/2021.2/Documentation/Manual/AssetBundlesIntro.html), rebuild them after the package upgrade. Rebuilding Asset Bundles ensures that the property conversion does not happen at runtime.

The following script lets you go through every Volume component in your project and apply your own value conversion.

```C#
using System;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Rendering
{
    class VolumePriorityUpgrader : Editor
    {
        [MenuItem("Project/Upgrade Project")]
        static void UpgradeProject()
        {
            // Load all prefabs
            string[] guids = AssetDatabase.FindAssets("glob:\"*.prefab\"");
            for (int i = 0; i < guids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (RecursiveApplyOnVolumeInHierarchy(gameObject.transform, Upgrade))
                {
                    EditorUtility.SetDirty(gameObject);
                    AssetDatabase.SaveAssetIfDirty(gameObject);
                }
            }

            // Load all scenes
            guids = AssetDatabase.FindAssets("glob:\"*.unity\"");
            for (int i = 0; i < guids.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                //prevent scene modification in readonly packages
                UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
                if (!(packageInfo == null
                    || packageInfo.source == UnityEditor.PackageManager.PackageSource.Local
                    || packageInfo.source == UnityEditor.PackageManager.PackageSource.Embedded))
                    return;

                SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(assetPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

                bool applied = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    applied |= RecursiveApplyOnVolumeInHierarchy(root.transform, Upgrade);
                if (applied)
                    EditorUtility.SetDirty(scene.GetRootGameObjects()[0]);

                AssetDatabase.SaveAssetIfDirty(new GUID(guids[i]));
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        static bool RecursiveApplyOnVolumeInHierarchy(Transform root, Action<Volume, Object> action)
        {
            bool applied = false;
            void RecursiveApply(Transform current)
            {
                foreach (Transform child in current.transform)
                    RecursiveApply(child);

                foreach (Volume volume in current.GetComponents<Volume>())
                {
                    action(volume, root);
                    applied = true;
                }
            }

            RecursiveApply(root);

            return applied;
        }

        static void Upgrade(Volume volume, Object container)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(volume))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(volume))
                {
                    GameObject instance = PrefabUtility.GetNearestPrefabInstanceRoot(volume);
                    UpgradePrefabInstance(volume, container, instance);
                }
                else if (PrefabUtility.IsPartOfPrefabAsset(volume))
                    UpgradePrefabAsset(volume, container);
                return;
            }
            else
                UpgradeNonPrefab(volume);
        }

        static void UpgradePrefabAsset(Volume volume, Object container)
        {
            float formerValue = new SerializedObject(volume).FindProperty("m_ObsoletePriority").floatValue;
            volume.priority = CustomFormula(formerValue);

            //logging change
            string path = AssetDatabase.GetAssetPath(container);
            string hierarchy = volume.transform == volume.transform.root ? volume.name : $"{volume.transform.root.name}/{AnimationUtility.CalculateTransformPath(volume.transform, volume.transform.root)}";
            Debug.Log($"Upgrading priority from {formerValue}f to {volume.priority} in prefab:{path}:{hierarchy}");
        }

        static void UpgradePrefabInstance(Volume volume, Object container, GameObject instance)
        {
            // in case of instance we prefer not upgrading them if they don't already have an override as it can add uneeded override onto the instance.
            PropertyModification[] overrides = PrefabUtility.GetPropertyModifications(instance).Where(o => o.target is Volume && (o.propertyPath == "m_Priority" || o.propertyPath == "priority")).ToArray();
            if (overrides.Length == 0)
                return;

            float formerValue = new SerializedObject(volume).FindProperty("m_ObsoletePriority").floatValue;
            volume.priority = CustomFormula(formerValue);

            //logging change
            bool isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(volume);
            string path = isPrefabAsset ? AssetDatabase.GetAssetPath(container) : volume.gameObject.scene.path;
            string hierarchy = volume.transform == volume.transform.root ? volume.name : $"{volume.transform.root.name}/{AnimationUtility.CalculateTransformPath(volume.transform, volume.transform.root)}";
            Debug.Log($"Upgrading priority from {formerValue}f to {volume.priority} in {(isPrefabAsset ? "prefab" : "scene")}:{path}:{hierarchy}");
        }

        static void UpgradeNonPrefab(Volume volume)
        {
            float formerValue = new SerializedObject(volume).FindProperty("m_ObsoletePriority").floatValue;
            volume.priority = CustomFormula(formerValue);

            //logging change
            string path = volume.gameObject.scene.path;
            string hierarchy = volume.transform == volume.transform.root ? volume.name : $"{volume.transform.root.name}/{AnimationUtility.CalculateTransformPath(volume.transform, volume.transform.root)}";
            Debug.Log($"Upgrading priority from {formerValue}f to {volume.priority} in scene:{path}:{hierarchy}");
        }

        static int CustomFormula(float formerValue)
        {
            // Edit this method with your own formula. Here is the formula currently used by the upgrade mechanisme
            return Mathf.RoundToInt(formerValue * 1000);
        }
    }
}
```