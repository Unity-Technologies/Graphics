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

            for (var i = 0; i < instances.Count; ++i)
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
                        if (!isPlayMode)
                        {
                            var directory = ResolveLoadAssetDirectory();

                            // Compute the full path
                            var fileName = string.Format(m_LoadAssetFormat, i, instance.name);
                            var fullPath = Path.Combine(directory, fileName);

                            var material = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                            if (material != null)
                                renderer.sharedMaterial = material;
                        }
                        else
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

        /// <summary>
        ///     Use this method to get the generated directory.
        /// </summary>
        /// <returns></returns>
        protected string ResolveGeneratedAssetDirectory()
        {
            return m_GeneratedDirectory.Resolve();
        }

        /// <summary>
        ///     Use this method to get the load directory.
        /// </summary>
        /// <returns></returns>
        protected string ResolveLoadAssetDirectory()
        {
            return m_LoadDirectory.Resolve();
        }

        /// <summary>
        ///     Override this method to generate a new material.
        /// </summary>
        /// <param name="prefab">The reference material.</param>
        /// <param name="index">The index of the game object.</param>
        /// <param name="instanceName">The name of the game object.</param>
        /// <returns></returns>
        protected virtual Material CreateMaterial(Material prefab, int index, string instanceName)
        {
            Material material;
            material = Instantiate(prefab);
            return material;
        }

        /// <summary>
        ///     How the material is obtained and applied.
        /// </summary>
        enum GetMaterialMode
        {
            /// <summary>
            ///     Use the 'Material' field as shared material for the game objects
            /// </summary>
            ReferenceMaterial,

            /// <summary>
            ///     Use the 'Material' field to instantiate a material for the game objects.
            /// </summary>
            InstantiateMaterial,

            /// <summary>
            ///     Get a material on disk, and if not found, create it from the 'Material' field.
            /// </summary>
            GetOrCreateMaterialAsset,

            /// <summary>
            ///     Get a material on disk, fail if not found.
            /// </summary>
            LoadMaterialAsset
        }

        /// <summary>
        ///     Specification of a directory on disk.
        /// </summary>
        [Serializable]
        struct DirectorySpec
        {
#pragma warning disable 649
            [Tooltip("If defined, will use the same directory of this asset. Otherwise, will used the path")]
            [SerializeField]
            Object m_SameDirectoryOf;

            [Tooltip("When 'SameDirectoryOf' is not defined, this path will be used. (Relative to the project root).")]
            [SerializeField]
            string m_CreateAssetDirectory;
#pragma warning restore 649

            /// <summary>
            ///     Compute the folder path
            /// </summary>
            /// <returns></returns>
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
        [Tooltip("When to execute this updater.")] [SerializeField]
        ExecuteMode m_ExecuteMode = ExecuteMode.All;

        [Tooltip("How the material is obtained and applied.")] [SerializeField]
        GetMaterialMode m_GetMaterialMode;

        [Tooltip("The reference material to use.")] [SerializeField]
        Material m_Material;

        [Header("Get or create asset")]
        [SerializeField]
        [Tooltip("Generated materials will be written in this directory.")]
        DirectorySpec m_GeneratedDirectory;

        [Tooltip("Generated materials will be named with this format. (0: index of instance, 1: name of instance)")]
        [SerializeField]
        string m_CreateAssetFormat = "Material_{0}_{1}.mat";

        [Header("Load asset")] [SerializeField] [Tooltip("Materials will be loaded from this directory.")]
        DirectorySpec m_LoadDirectory;

        [SerializeField]
        [Tooltip(
            "To find the material to load, this format will be used to find its name. (0: index of instance, 1: name of instance)")]
        string m_LoadAssetFormat = "Material_{0}_{1}.mat";
#pragma warning restore 649
    }
}
