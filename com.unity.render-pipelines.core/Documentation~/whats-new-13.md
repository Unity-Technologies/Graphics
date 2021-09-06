# What's new in SRP Core version 13 / Unity 2022.1

This page contains an overview of new features, improvements, and issues resolved in version 13 of the Core Render Pipeline package, embedded in Unity 2022.1.

## Improvements

### Volume

The Volume framework have evolve a little to be more similar in the way it handle priorities to the remaining of the editor. Now it will be int based. The transition is done automatically in an internal upgrade. Though changing from float to int is not a trivial task and some project can have priority that collide.

If you encounter any colliding priority, you may have Volume that didn't apply change anymore. So you should resolve any conflict. The upgrade just take former priority times 1000 to keep first 3 digits of the float as relevant for the new int priority. It is fine most of the time.

But in some case you want to apply a different float to int adaptation. Here is a script that allow you to pass on every Volume of your project and apply your own re-range.

```
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

Don't forget to also rebuild your AssetBundles. They should be rebuild after each upgrade of HDRP to ensure the migration is build and will not occurs on loading (at runtime) for small performance gains. But here as you modify the way it have been migrated, you must do it or your custom adaptation will be lost.
