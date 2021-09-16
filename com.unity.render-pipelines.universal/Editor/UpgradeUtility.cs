using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Flags describing usage of an asset by its dependents, when that asset might have serialized shader property names.
    /// </summary>
    [Flags]
    enum SerializedShaderPropertyUsage : byte
    {
        /// <summary>
        /// Asset's usage is unknown.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Asset contains no serialized shader properties.
        /// </summary>
        NoShaderProperties = 1,
        /// <summary>
        /// Asset is used by objects that have materials which have been upgraded.
        /// </summary>
        UsedByUpgraded = 2,
        /// <summary>
        /// Asset is used by objects that have materials which were not upgraded.
        /// </summary>
        UsedByNonUpgraded = 4,
        /// <summary>
        /// Asset is used by objects that have materials which may have been upgraded, but there is no unambiguous upgrade path.
        /// </summary>
        UsedByAmbiguouslyUpgraded = 8,
    }

    /// <summary>
    /// Class containing utility methods for upgrading assets affected by render pipeline migration.
    /// </summary>
    static class UpgradeUtility
    {
        /// <summary>
        /// Stable, unique identifier for some asset.
        /// </summary>
        internal struct UID
        {
            public string Value;
            public static implicit operator string(UID uid) => uid.Value;
            public static implicit operator UID(string id) => new UID { Value = id };
        }

        internal interface IMaterial
        {
            UID ID { get; }
            string ShaderName { get; }
        }

        internal struct MaterialProxy : IMaterial
        {
            public MaterialProxy(Material material, UnityObject[] allAssetsAtPath)
            {
                m_ID = $"{allAssetsAtPath}{Array.IndexOf(allAssetsAtPath, material)}";
                m_Material = material;
            }

            UID m_ID;
            Material m_Material;
            public UID ID => m_ID;
            public string ShaderName => m_Material.shader.name;
            public static implicit operator Material(MaterialProxy proxy) => proxy.m_Material;
            public static implicit operator MaterialProxy(Material material) =>
                new MaterialProxy(material, AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(material)));
            public override string ToString() => m_Material.ToString();
        }

        /// <summary>
        /// Create A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// </summary>
        /// <param name="upgraders">The set of <see cref="MaterialUpgrader"/> from which to build the table.</param>
        /// <returns>A table of new shader names and all known upgrade paths to them in the target pipeline.</returns>
        public static Dictionary<string, IReadOnlyList<MaterialUpgrader>> GetAllUpgradePathsToShaders(
            IEnumerable<MaterialUpgrader> upgraders
        )
        {
            var upgradePathBuilder = new Dictionary<string, List<MaterialUpgrader>>();
            foreach (var upgrader in upgraders)
            {
                // skip over upgraders that do not rename shaders or have not been initialized
                if (upgrader.NewShaderPath == null)
                    continue;

                if (!upgradePathBuilder.TryGetValue(upgrader.NewShaderPath, out var allPaths))
                    upgradePathBuilder[upgrader.NewShaderPath] = allPaths = new List<MaterialUpgrader>();
                allPaths.Add(upgrader);
            }
            return upgradePathBuilder.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MaterialUpgrader>);
        }

        /// <summary>
        /// Gets the new name for a serialized shader property, which needs to be applied to a material that has been upgraded.
        /// </summary>
        /// <remarks>
        /// Some assets serialize shader property names, in order to apply modifications to a material at run-time.
        /// Use this method's return value to determine whether the serialized property name can be safely substituted,
        /// based on a material the host object intends to apply it to.
        /// </remarks>
        /// <param name="shaderPropertyName">A shader property name serialized on a host object.</param>
        /// <param name="material">
        /// The target material to which some shader property modification will be applied.
        /// It is presumed to have already been upgraded.
        /// </param>
        /// <param name="materialPropertyType">What type of property <paramref name="shaderPropertyName"/> is.</param>
        /// <param name="allUpgradePathsToNewShaders">
        /// A table of new shader names and all known upgrade paths to them in the target pipeline.
        /// (See also <seealso cref="UpgradeUtility.GetAllUpgradePathsToShaders"/>.)
        /// </param>
        /// <param name="upgradePathsUsedByMaterials">
        /// Optional table of materials known to have gone through a specific upgrade path.
        /// </param>
        /// <param name="newPropertyName">
        /// The new name for <paramref name="shaderPropertyName"/>.
        /// Its value is only guaranteed to be unambiguous if the method returns
        /// <see cref="SerializedShaderPropertyUsage.UsedByUpgraded"/>.
        /// </param>
        /// <returns>
        /// Usage flags indicating how <paramref name="shaderPropertyName"/> relates to <paramref name="material"/>.
        /// </returns>
        public static SerializedShaderPropertyUsage GetNewPropertyName(
            string shaderPropertyName,
            IMaterial material,
            MaterialUpgrader.MaterialPropertyType materialPropertyType,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialUpgrader>> allUpgradePathsToNewShaders,
            IReadOnlyDictionary<UID, MaterialUpgrader> upgradePathsUsedByMaterials,
            out string newPropertyName
        )
        {
            var result = SerializedShaderPropertyUsage.Unknown;

            // we want to find out if we should rename the property
            newPropertyName = shaderPropertyName;

            // first check if we already know how this material was upgraded
            if (upgradePathsUsedByMaterials != null && upgradePathsUsedByMaterials.TryGetValue(material.ID, out var upgrader))
            {
                result |= SerializedShaderPropertyUsage.UsedByUpgraded;

                var propertyRenameTable = upgrader.GetPropertyRenameMap(materialPropertyType);
                propertyRenameTable.TryGetValue(shaderPropertyName, out newPropertyName);
            }

            // otherwise, try to guess whether it might have been upgraded
            if (newPropertyName == shaderPropertyName)
            {
                // get possible known upgrade paths material might have taken
                allUpgradePathsToNewShaders.TryGetValue(material.ShaderName, out var possibleUpgraders);

                // if there are none, then assume this material was not upgraded
                if ((possibleUpgraders?.Count ?? 0) == 0)
                {
                    result |= SerializedShaderPropertyUsage.UsedByNonUpgraded;
                }
                // otherwise, see if there are any possible upgrade paths
                else
                {
                    // narrow possible upgraders to those which specify a rename for the bound property
                    var matchingUpgraders = possibleUpgraders.Where(
                        u => u.GetPropertyRenameMap(materialPropertyType).ContainsKey(shaderPropertyName)
                        ).ToList();

                    // if there are any, assume the material has been upgraded
                    if (matchingUpgraders.Any())
                    {
                        result |= SerializedShaderPropertyUsage.UsedByUpgraded;

                        // if there are many possible upgrade paths to take, mark the upgrade as ambiguous
                        newPropertyName = matchingUpgraders[0].GetPropertyRenameMap(materialPropertyType)[shaderPropertyName];
                        var name = newPropertyName; // cannot use out param inside lambda
                        if (matchingUpgraders.Any(u => u.GetPropertyRenameMap(materialPropertyType)[shaderPropertyName] != name))
                            result |= SerializedShaderPropertyUsage.UsedByAmbiguouslyUpgraded;
                    }
                    else
                    {
                        var alreadyUpgraded = possibleUpgraders.Any(u => u.GetPropertyRenameMap(materialPropertyType).Values.Contains(shaderPropertyName));

                        if (alreadyUpgraded)
                        {
                            result |= SerializedShaderPropertyUsage.UsedByUpgraded;
                        }
                    }
                }
            }

            return result;
        }
    }
}
