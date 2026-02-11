using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.Rendering.MaterialUpgrader;

namespace UnityEditor.Rendering.Converter
{
    [Serializable]
    internal class RenderPipelineConverterMaterialUpgraderItem : IRenderPipelineConverterItem
    {
        public string assetPath { get; }
        public List<string> variantsPaths { get; }

        public string name { get; }

        public string info => assetPath;

        public bool isEnabled { get; set; } = true;
        public string isDisabledMessage { get; set; } = string.Empty;

        public Texture2D icon
        {
            get
            {
                var obj = material;
                if (obj == null)
                    return null;

                // Try the object's thumbnail/icon
                var icon = AssetPreview.GetMiniThumbnail(obj);
                if (icon != null) return icon;

                // Fallback to type icon
                var type = obj.GetType();
                icon = EditorGUIUtility.ObjectContent(null, type).image as Texture2D;
                return icon;
            }
        }

        public RenderPipelineConverterMaterialUpgraderItem(string shaderPath, string materialPath, List<string> variantsPaths)
        {
            if (string.IsNullOrEmpty(materialPath))
                throw new ArgumentException(nameof(materialPath));

            assetPath = materialPath;
            this.variantsPaths = variantsPaths;

            if (material == null)
                throw new ArgumentException($"Unable to load material at path {materialPath}");

            name = material.name + " - " + shaderPath;
        }

        public Material material => AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        public IEnumerable<Material> variantMaterials
        {
            get
            {
                foreach (var path in variantsPaths)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat != null)
                        yield return mat;
                }
            }
        }

        public void OnClicked()
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(assetPath));
        }
    }

    [Serializable]
    internal abstract class RenderPipelineConverterMaterialUpgrader : IRenderPipelineConverter
    {
        /// <summary>
        /// List of material upgraders to use for this converter.
        /// </summary>
        protected abstract List<MaterialUpgrader> upgraders { get; }

        private List<MaterialUpgrader> m_UpgradersCache;

        internal List<IRenderPipelineConverterItem> assets = new();

        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
            m_UpgradersCache = upgraders;

            if (m_UpgradersCache.Count == 0)
            {
                Debug.Log($"No upgraders specified for this converter ({GetType()}). Skipping Initialization.");
                return;
            }

            var materialsGroupByShader = MaterialFinder.GroupAllMaterialsInProject();
            using (HashSetPool<string>.Get(out var destinationShaders))
            {
                foreach (var upgrader in m_UpgradersCache)
                {
                    destinationShaders.Add(upgrader.NewShaderPath);
                }

                assets.Clear();
                foreach (var kvp in materialsGroupByShader)
                {
                    // This material shader is already on the target pipeline, skip it.
                    if (destinationShaders.Contains(kvp.Key))
                        continue;

                    foreach (var (parent, variants) in kvp.Value)
                    {
                        List<string> variantsPaths = new();
                        foreach (var variant in variants)
                        {
                            variantsPaths.Add(AssetDatabase.GetAssetPath(variant));
                        }

                        assets.Add(new RenderPipelineConverterMaterialUpgraderItem(kvp.Key,
                            AssetDatabase.GetAssetPath(parent),
                            variantsPaths));
                    }
                }
                onScanFinish?.Invoke(assets);
            } 
        }

        public Status Convert(IRenderPipelineConverterItem item, out string message)
        {
            if (item is not RenderPipelineConverterMaterialUpgraderItem materialUpgraderItem)
            {
                message = $"Item is not a {nameof(RenderPipelineConverterMaterialUpgraderItem)}.";
                return Status.Error;
            }

            if (materialUpgraderItem.material == null)
            {
                message = $"Failed to load material at path {materialUpgraderItem.assetPath}.";
                return Status.Error;
            }

            var upgrader = MaterialUpgrader.GetUpgrader(m_UpgradersCache, materialUpgraderItem.material);
            if (upgrader == null)
            {
                message = $"No upgrader found for shader {materialUpgraderItem.material.shader.name}.";
                return Status.Warning;
            }

            upgrader.Upgrade(materialUpgraderItem.material, UpgradeFlags.None);
            foreach (var variant in materialUpgraderItem.variantMaterials)
            {
                if (variant == null)
                    continue;

                upgrader.Upgrade(variant, UpgradeFlags.None);
            }

            message = string.Empty;
            return Status.Success;
        }
    }
}
