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

        public string name => assetPath;

        public string info { get; }

        public bool isEnabled { get; set; }
        public string isDisabledMessage { get; set; }

        public RenderPipelineConverterMaterialUpgraderItem(string shaderPath, string materialPath, List<string> variantsPaths)
        {
            if (string.IsNullOrEmpty(materialPath))
                throw new ArgumentException(nameof(materialPath));

            assetPath = materialPath;
            this.variantsPaths = variantsPaths;
            info = shaderPath;
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

        public RenderPipelineConverterMaterialUpgrader()
        {
            m_UpgradersCache = upgraders;

            if (m_UpgradersCache.Count == 0)
            {
                Debug.Log($"No upgraders specified for this converter ({GetType()}). Skipping Initialization.");
                return;
            }
        }

        public void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish)
        {
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
