using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    internal class MaterialFinder
    {
        public static Dictionary<string, List<(Material parent, List<Material> variants)>> GroupAllMaterialsInProject()
        {
            var allMaterials = AssetDatabaseHelper.FindAssets<Material>();

            Dictionary<string, List<(Material parent, List<Material> variants)>> result = new();

            foreach (var material in allMaterials)
            {
                var shader = material.shader;

                if (shader == null)
                    continue;

                // Try get the shader entry, if not present create it
                if (!result.TryGetValue(shader.name, out var list))
                {
                    list = new List<(Material parent, List<Material> variants)>();
                    result[shader.name] = list;
                }

                bool isMaterialVariant = material.parent != null;

                if (isMaterialVariant)
                {
                    var entry = list.Find(e => e.parent == material.parent);
                    if (entry == default)
                    {
                        entry = (material.parent, new List<Material> { material });
                        list.Add(entry);
                    }
                    else
                    {
                        entry.variants.Add(material);
                    }
                }
                else
                {
                    var entry = list.Find(e => e.parent == material);
                    if (entry == default)
                    {
                        entry = (material, new List<Material>());
                        list.Add(entry);
                    }
                    else
                    {
                        // Already present as parent, do nothing
                    }
                }
            }

            return result;
        }
    }
}
