using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipelineTest.TestGenerator
{
    // Prototype
    [AddComponentMenu("TestGenerator/Updaters/Set Material On GameObjects")]
    public class SetMaterialOnGameObjects : MonoBehaviour, IUpdateGameObjects
    {
        enum GetMaterialMode
        {
            ReferenceMaterial,
            InstantiateMaterial,
            GetOrCreateMaterialAsset,
            LoadMaterialAsset
        }

        [Serializable]
        struct DirectorySpec
        {
#pragma warning disable 649
            [Tooltip("If defined, will use the same directory of this asset. Otherwise, will used the path")]
            [SerializeField] Object m_SameDirectoryOf;
            [SerializeField] string m_CreateAssetDirectory;
#pragma warning restore 649

            public string Resolve()
            {
                var directory = m_CreateAssetDirectory;
                if (m_SameDirectoryOf != null && EditorUtility.IsPersistent(m_SameDirectoryOf))
                {
                    var dir = AssetDatabase.GetAssetPath(m_SameDirectoryOf);
                    if (!string.IsNullOrEmpty(dir))
                        directory = Path.GetDirectoryName(dir);
                }

                return directory;
            }
        }

#pragma warning disable 649
        [SerializeField] ExecuteMode m_ExecuteMode = ExecuteMode.All;
        [SerializeField] GetMaterialMode m_GetMaterialMode;
        [SerializeField] Material m_Material;
        [Header("Get or create asset")]
        [SerializeField]
        DirectorySpec m_GeneratedDirectory;
        [Tooltip("0: index of instance, 1: name of instance")]
        [SerializeField] string m_CreateAssetFormat = "Material_{0}_{1}.mat";
        [Header("Load asset")]
        [SerializeField]
        DirectorySpec m_LoadDirectory;
        [Tooltip("0: index of instance, 1: name of instance")]
        [SerializeField] string m_LoadAssetFormat = "Material_{0}_{1}.mat";
#pragma warning restore 649

        public ExecuteMode executeMode => m_ExecuteMode;

        public void UpdateInPlayMode(Transform parent, List<GameObject> instances)
        {
            UpdateInstances(instances, true);
        }

        public void UpdateInEditMode(Transform parent, List<GameObject> instances)
        {
            UpdateInstances(instances, false);
        }

        void UpdateInstances(List<GameObject> instances, bool isPlayMode)
        {
            if (m_Material == null || m_Material.Equals(null))
                return;

            for (var i = 0 ; i < instances.Count; ++i)
            {
                var instance = instances[i];
                var renderer = instance.GetComponent<Renderer>();
                if (renderer == null || renderer.Equals(null)) continue;

                switch (m_GetMaterialMode)
                {
                    case GetMaterialMode.ReferenceMaterial:
                        renderer.sharedMaterial = m_Material;
                        break;
                    case GetMaterialMode.InstantiateMaterial:
                        renderer.material = Instantiate(m_Material);
                        break;
                    case GetMaterialMode.GetOrCreateMaterialAsset:
                    {
#if UNITY_EDITOR
                        if (!isPlayMode)
                        {
                            // Find asset on disk

                            // Resolve the directory
                            var directory = ResolveGeneratedAssetDirectory();

                            // Compute the full path
                            var fileName = string.Format(m_CreateAssetFormat, i, instance.name);
                            var fullPath = Path.Combine(directory, fileName);

                            // Load the asset
                            var material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                            if (material == null)
                            {
                                // Create the asset
                                material = CreateMaterial(m_Material, i, instance.name);
                                AssetDatabase.CreateAsset(material, fullPath);
                            }

                            renderer.sharedMaterial = material;
                        }
                        else
#endif
                        {
                            Debug.LogError(
                                $"{typeof(GetMaterialMode).Name} {GetMaterialMode.GetOrCreateMaterialAsset} " +
                                "is not available in playmode test.");
                        }
                        break;
                    }
                    case GetMaterialMode.LoadMaterialAsset:
                    {
#if UNITY_EDITOR
                        var directory = ResolveLoadAssetDirectory();

                        // Compute the full path
                        var fileName = string.Format(m_LoadAssetFormat, i, instance.name);
                        var fullPath = Path.Combine(directory, fileName);

                        var material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                        if (material != null)
                            renderer.sharedMaterial = material;
#endif
                        {
                            Debug.LogError(
                                $"{typeof(GetMaterialMode).Name} {GetMaterialMode.LoadMaterialAsset} " +
                                "is not available in playmode test.");
                        }
                        break;
                    }
                }
            }
        }

        protected string ResolveGeneratedAssetDirectory() => m_GeneratedDirectory.Resolve();
        protected string ResolveLoadAssetDirectory() => m_LoadDirectory.Resolve();

        protected virtual Material CreateMaterial(Material prefab, int index, string instanceName)
        {
            Material material;
            material = Instantiate(prefab);
            return material;
        }
    }
}
