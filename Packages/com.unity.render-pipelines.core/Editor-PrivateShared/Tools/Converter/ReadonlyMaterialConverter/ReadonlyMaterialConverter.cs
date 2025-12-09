using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering.Converter
{
    internal class ReadonlyMaterialMap
    {
        public bool TryGetMappingMaterial(Material material, out Material mappingMaterial)
        {
            mappingMaterial = material;

            if (m_BuiltInMaterialsMappings.TryGetValue(material.name, out var mapping))
                mappingMaterial = mapping();

            return mappingMaterial != null;
        }

        public int count => m_BuiltInMaterialsMappings.Count;

        public IEnumerable<string> Keys => m_BuiltInMaterialsMappings.Keys;

        Dictionary<string, Func<Material>> m_BuiltInMaterialsMappings = new();

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

        public ReadonlyMaterialMap(Dictionary<string, Func<Material>> mappings)
        {
            m_BuiltInMaterialsMappings = mappings;
        }

        public Material[] GetBuiltInMaterials()
        {
            using (UnityEngine.Pool.ListPool<Material>.Get(out var tmp))
            {
                foreach (var materialName in Keys)
                {
                    var name = materialName + ".mat";

                    Material mat = null;
                    foreach (var material in AssetDatabaseHelper.FindAssets<Material>())
                    {
                        if (material.name == materialName)
                        {
                            mat = material;
                            break;
                        }
                    }

                    if (mat == null)
                    {
                        mat = AssetDatabase.GetBuiltinExtraResource<Material>(name);
                        if (mat == null)
                        {
                            mat = Resources.GetBuiltinResource<Material>(name);
                            if (mat == null)
                            {
                                mat = Resources.Load<Material>(name);
                            }
                        }
                    }

                    if (mat == null)
                    {
                        Debug.LogError($"Material '{materialName}' not found in built-in resources or project assets.");
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

        protected override List<(string query, string description)> contextSearchQueriesAndIds
        {
            get => mappings.GetMaterialSearchList();
        }

        internal MaterialReferenceChanger m_MaterialReferenceChanger;
        private ReadonlyMaterialMap m_Mappings;

        internal ReadonlyMaterialMap mappings
        {
            get
            {
                m_Mappings ??= new ReadonlyMaterialMap(materialMappings);
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
