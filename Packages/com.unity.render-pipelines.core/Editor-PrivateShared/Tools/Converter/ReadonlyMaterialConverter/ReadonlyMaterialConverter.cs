using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering.Converter
{
    internal class ReadonlyMaterialMap
    {
        const string k_BuiltInExtraResourcesPath = "Resources/unity_builtin_extra";

        public bool TryGetMappingMaterial(Material material, out Material mappingMaterial)
        {
            mappingMaterial = material;

            if (material != null &&
                sourceMaterials.TryGetValue(material.name, out var sourceMaterial) &&
                sourceMaterial == material &&
                m_BuiltInMaterialsMappings.TryGetValue(material.name, out var mapping))
            {
                mappingMaterial = mapping();
            }

            return mappingMaterial != null;
        }

        public int count => m_BuiltInMaterialsMappings.Count;

        public IEnumerable<string> Keys => m_BuiltInMaterialsMappings.Keys;

        Dictionary<string, Func<Material>> m_BuiltInMaterialsMappings = new();
        Func<IEnumerable<Material>> m_SourceMaterialsProvider;
        Dictionary<string, Material> m_SourceMaterials;

        Dictionary<string, Material> sourceMaterials => m_SourceMaterials ??= CollectSourceMaterials();

        public List<(string materialName, string searchQuery)> GetMaterialSearchList()
        {
            List<(string materialName, string searchQuery)> list = new();
            foreach (var mat in GetBuiltInMaterials())
            {
                string formattedId = $"<$object:{GlobalObjectId.GetGlobalObjectIdSlow(mat)},UnityEngine.Object$>";
                list.Add(($"p: ref={formattedId}", $"{mat.name} is being referenced"));
            }
            return list;
        }

        public ReadonlyMaterialMap(Dictionary<string, Func<Material>> mappings, Func<IEnumerable<Material>> sourceMaterialsProvider = null)
        {
            m_BuiltInMaterialsMappings = mappings;
            m_SourceMaterialsProvider = sourceMaterialsProvider ?? LoadBuiltInExtraMaterials;
        }

        public static IEnumerable<Material> LoadBuiltInExtraMaterials()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(k_BuiltInExtraResourcesPath))
            {
                if (asset is Material material)
                    yield return material;
            }
        }

        Dictionary<string, Material> CollectSourceMaterials()
        {
            var materials = new Dictionary<string, Material>();
            foreach (var material in m_SourceMaterialsProvider())
            {
                if (material != null && m_BuiltInMaterialsMappings.ContainsKey(material.name))
                    materials.TryAdd(material.name, material);
            }
            return materials;
        }

        public Material[] GetBuiltInMaterials()
        {
            using (UnityEngine.Pool.ListPool<Material>.Get(out var tmp))
            {
                foreach (var materialName in Keys)
                {
                    if (!sourceMaterials.TryGetValue(materialName, out var mat) || mat == null)
                    {
                        Debug.LogError($"Material '{materialName}' not found in built-in resources.");
                        continue;
                    }

                    tmp.Add(mat);
                }
                return tmp.ToArray();
            }
        }
    }

    [Serializable]
    internal abstract class ReadonlyMaterialConverter : AssetsConverter
    {
        protected virtual Dictionary<string, Func<Material>> materialMappings { get; }

        protected virtual IEnumerable<Material> GetSourceMaterials() => ReadonlyMaterialMap.LoadBuiltInExtraMaterials();

        protected override List<(string query, string description)> contextSearchQueriesAndIds
        {
            get => mappings.GetMaterialSearchList();
        }

        /// <summary>
        /// Categorize results by material name instead of asset path
        /// </summary>
        protected override List<IRenderPipelineConverterItem> CategorizeResults(
            List<RenderPipelineConverterAssetItem> assets,
            Dictionary<string, List<RenderPipelineConverterAssetItem>> assetsByDescription)
        {
            if (assetsByDescription == null || assetsByDescription.Count == 0)
                return new List<IRenderPipelineConverterItem>();

            var result = new List<IRenderPipelineConverterItem>();

            foreach (var kvp in assetsByDescription)
            {
                var materialName = kvp.Key;
                var materialAssets = kvp.Value;

                if (materialAssets.Count == 0)
                    continue;

                // Create asset group for this material (shows material icon, not folder icon)
                var materialGroup = new RenderPipelineConverterUtility.AssetGroupItem
                {
                    name = materialName,
                    info = $"{materialAssets.Count} object(s) referencing {materialName}",
                    assetType = typeof(Material)
                };

                // Add all assets to this group
                foreach (var asset in materialAssets)
                {
                    materialGroup.children.Add(asset);
                }

                result.Add(materialGroup);
            }

            return result;
        }

        internal MaterialReferenceChanger m_MaterialReferenceChanger;
        private ReadonlyMaterialMap m_Mappings;

        internal ReadonlyMaterialMap mappings
        {
            get
            {
                m_Mappings ??= new ReadonlyMaterialMap(materialMappings, GetSourceMaterials);
                return m_Mappings;
            }
        }

        public override void BeforeConvert()
        {
            m_MaterialReferenceChanger = new MaterialReferenceChanger(mappings);
        }

        public override void AfterConvert()
        {
            m_MaterialReferenceChanger?.Dispose();
            m_MaterialReferenceChanger = null;
        }

        protected override Status ConvertObject(UnityEngine.Object obj, StringBuilder message)
        {
            if (!m_MaterialReferenceChanger.ReassignUnityObjectMaterials(obj, message))
            {
                return Status.Error;
            }

            return Status.Success;
        }
    }
}
