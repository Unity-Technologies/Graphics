using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Class to handle the Material Upgraders and fetching of them
    /// </summary>
    internal class MaterialUpgraderRegistry
    {
        private static Lazy<MaterialUpgraderRegistry> m_Instance = new(() => new MaterialUpgraderRegistry());

        /// <summary>
        /// Singleton instance of the registry.
        /// </summary>
        public static MaterialUpgraderRegistry instance => m_Instance.Value;

        Dictionary<Type, List<MaterialUpgrader>> m_UpgradersSupportedByPipeline = new();

        /// <summary>
        /// Returns a list of <see cref="MaterialUpgrader"/> with a given asset type
        /// </summary>
        /// <param name="renderPipelineAssetType">A valid RP asset type</param>
        /// <returns>Returns a list of <see cref="MaterialUpgrader"/></returns>
        public List<MaterialUpgrader> GetMaterialUpgradersForPipeline(Type renderPipelineAssetType)
        {
            List<MaterialUpgrader> materialUpgraders = GetOrCreateMaterialUpgradersForPipeline(renderPipelineAssetType);
            materialUpgraders.AddRange(GetOrCreateMaterialUpgradersForPipeline(typeof(RenderPipelineAsset)));
            materialUpgraders.Sort(CompareUpgradersByOldShaderAndPriority);
            return materialUpgraders;
        }

        private int CompareUpgradersByOldShaderAndPriority(MaterialUpgrader a, MaterialUpgrader b)
        {
            string nameA = a?.OldShaderPath ?? string.Empty;
            string nameB = b?.OldShaderPath ?? string.Empty;
            int nameComparison = string.Compare(nameA, nameB, StringComparison.Ordinal);
            if (nameComparison != 0)
                return nameComparison;

            // If names are the same, compare by priority
            return b.priority.CompareTo(a.priority);
        }

        MaterialUpgraderRegistry()
        {
            var providerTypes = TypeCache.GetTypesDerivedFrom<IMaterialUpgradersProvider>();

            foreach (var providerType in providerTypes)
            {
                if (providerType.IsAbstract)
                    continue;
                    
                var provider = Activator.CreateInstance(providerType) as IMaterialUpgradersProvider;

                var upgraders = provider.GetUpgraders();
                if (upgraders == null)
                    continue;

                var pipelineTypes = GetSupportedPipelines(providerType);

                if (pipelineTypes.Length == 0)
                    pipelineTypes = new Type[] { typeof(RenderPipelineAsset) }; // Default to all pipelines if none specified

                foreach (var pipelineType in pipelineTypes)
                {
                    var upgradersForPipeline = GetOrCreateMaterialUpgradersForPipeline(pipelineType);
                    foreach(var upgrader in upgraders) 
                    {
                        upgradersForPipeline.Add(upgrader);
                    }
                }
            }
        }

        private static Type[] GetSupportedPipelines(Type upgraderType)
        {
            var attr = upgraderType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
            if (attr == null)
                throw new InvalidOperationException($"Missing {nameof(SupportedOnRenderPipelineAttribute)} on {upgraderType}");

            return attr.renderPipelineTypes;
        }


        private List<MaterialUpgrader> GetOrCreateMaterialUpgradersForPipeline(Type renderPipelineAssetType)
        {
            if (!typeof(RenderPipelineAsset).IsAssignableFrom(renderPipelineAssetType))
                throw new ArgumentException($"Type '{renderPipelineAssetType.FullName}' must inherit from RenderPipelineAsset.", nameof(renderPipelineAssetType));

            if (!m_UpgradersSupportedByPipeline.TryGetValue(renderPipelineAssetType, out var upgradersForPipeline))
            {
                upgradersForPipeline = new List<MaterialUpgrader>();
                m_UpgradersSupportedByPipeline[renderPipelineAssetType] = upgradersForPipeline;
            }

            return upgradersForPipeline;
        }
    }
}
