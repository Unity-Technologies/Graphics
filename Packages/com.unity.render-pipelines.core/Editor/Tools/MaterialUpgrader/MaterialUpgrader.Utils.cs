using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Material Upgrader class.
    /// </summary>
    public partial class MaterialUpgrader
    {
        #region Internal API
        /// <summary>
        /// Represents an entry describing material properties
        /// </summary>
        internal class MaterialInfo
        {
            /// <summary>
            /// The material name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// The material being evaluated.
            /// </summary>
            [CanBeNull]
            public Material Material { get; set; }

            /// <summary>
            /// The current shader of the material.
            /// </summary>
            public string ShaderName { get; set; }

            /// <summary>
            /// If the material is a variant
            /// </summary>
            public bool IsVariant { get; set; }

            /// <summary>
            /// The base material name
            /// </summary>
            public string BaseMaterialName { get; set; }

            /// <summary>
            /// The base material is <see cref="IsVariant"/> is true
            /// </summary>
            [CanBeNull]
            public Material BaseMaterial { get; set; }

            /// <summary>
            /// Determines whether the specified object is equal to the current <see cref="MaterialInfo"/>.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj)
            {
                if (obj is not MaterialInfo other)
                    return false;

                return string.Equals(ShaderName, other.ShaderName, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Serves as the default hash function.
            /// </summary>
            /// <returns>A hash code for the current object.</returns>
            public override int GetHashCode()
            {
                return ShaderName?.GetHashCode() ?? 0;
            }
        }

        /// <summary>
        /// Represents an entry describing whether a material is available for upgrade,
        /// along with the reason if it's not.
        /// </summary>
        internal class MaterialUpgradeEntry
        {
            /// <summary>
            /// The material being evaluated.
            /// </summary>
            public MaterialInfo MaterialInfo { get; set; }

            /// <summary>
            /// Indicates whether the material is available for upgrade.
            /// </summary>
            public bool AvailableForUpgrade { get; set; }

            /// <summary>
            /// If the material is not available for upgrade, this provides the reason why.
            /// Empty if <see cref="AvailableForUpgrade"/> is true.
            /// </summary>
            public string NotAvailableForUpgradeReason { get; set; }

            /// <summary>
            /// Determines whether the specified object is equal to the current <see cref="MaterialUpgradeEntry"/>.
            /// </summary>
            /// <param name="obj">The object to compare with the current object.</param>
            /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
            public override bool Equals(object obj)
            {
                if (obj is not MaterialUpgradeEntry other)
                    return false;

                return Equals(MaterialInfo, other.MaterialInfo) &&
                       AvailableForUpgrade == other.AvailableForUpgrade &&
                       string.Equals(NotAvailableForUpgradeReason, other.NotAvailableForUpgradeReason, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Serves as the default hash function.
            /// </summary>
            /// <returns>A hash code for the current object.</returns>
            public override int GetHashCode()
            {
                return HashCode.Combine(MaterialInfo, AvailableForUpgrade, NotAvailableForUpgradeReason);
            }
        }

        static MaterialUpgrader GetUpgrader(List<MaterialUpgrader> upgraders, Material material)
        {
            if (material == null || material.shader == null)
                return null;

            string shaderName = material.shader.name;
            for (int i = 0; i != upgraders.Count; i++)
            {
                if (upgraders[i].OldShaderPath == shaderName)
                    return upgraders[i];
            }

            return null;
        }

        //@TODO: Only do this when it exceeds memory consumption...
        static void SaveAssetsAndFreeMemory()
        {
            AssetDatabase.SaveAssets();
            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Returns if the material has a mapping upgrader
        /// </summary>
        /// <param name="upgraders">The available upgrader list</param>
        /// <param name="material">The material to check</param>
        /// <returns></returns>
        static bool IsMaterialUpgradable(List<MaterialUpgrader> upgraders, Material material)
        {
            if (material == null || upgraders == null)
                throw new ArgumentException("Invalid input: upgraders or material is null.");

            return GetUpgrader(upgraders, material) != null;
        }
        /// Extracts shader paths from a list of upgraders into two sets:
        /// one for shaders to upgrade (old) and one for shaders already upgraded (new).
        internal static void GetUpgraderShaderPaths(List<MaterialUpgrader> upgraders, out HashSet<string> oldShaders, out HashSet<string> newShaders)
        {
            oldShaders = new HashSet<string>();
            newShaders = new HashSet<string>();

            if (upgraders == null)
                return;

            foreach (var upgrader in upgraders)
            {
                if (!string.IsNullOrEmpty(upgrader.OldShaderPath))
                    oldShaders.Add(upgrader.OldShaderPath);

                if (!string.IsNullOrEmpty(upgrader.NewShaderPath))
                    newShaders.Add(upgrader.NewShaderPath);
            }
        }

        internal static MaterialInfo ToMaterialInfo(Material material)
        {
            Material baseMaterial = material;

            if (material.isVariant)
            {
                // Traverse up to the root material
                while (baseMaterial.isVariant && baseMaterial.parent != null)
                {
                    baseMaterial = baseMaterial.parent;
                }
            }

            return new MaterialInfo
            {
                Name = material.name,
                Material = material,
                ShaderName = material.shader.name,
                IsVariant = material.isVariant,
                BaseMaterialName = baseMaterial.name,
                BaseMaterial = baseMaterial
            };
        }

        internal static List<MaterialInfo> GatherInfo(IEnumerable<Material> materials)
        {
            var materialsInfo = new List<MaterialInfo>();
            foreach (var material in materials)
            {
                if (material == null || material.shader == null)
                    continue;

                materialsInfo.Add(ToMaterialInfo(material));
            }

            return materialsInfo;
        }

        internal static IEnumerable<MaterialUpgradeEntry> FetchUpgradeOptions(HashSet<string> upgradersAvailable, HashSet<string> shaderNamesToIgnore, List<MaterialInfo> materialInfo)
        {
            foreach (var material in materialInfo)
            {
                var shaderName = material.ShaderName;
                bool isShaderIgnored = shaderNamesToIgnore.Contains(shaderName);
                if (isShaderIgnored)
                    continue;

                bool isUpgradable = !material.IsVariant && upgradersAvailable.Contains(shaderName);
                string reason = isUpgradable ? string.Empty : GenerateReason(material);

                yield return new MaterialUpgradeEntry
                {
                    MaterialInfo = material,
                    AvailableForUpgrade = isUpgradable,
                    NotAvailableForUpgradeReason = reason
                };
            }
        }

        internal static List<Material> FetchAllMaterialsInProject()
        {
            List<Material> materials = new(AssetDatabaseHelper.FindAssets<Material>(".mat"));
            // Add the build in material of terrain ( must be always available )
            if (Terrain.activeTerrains.Length > 0)
            {
                materials.Add(Terrain.activeTerrain.materialTemplate);
            }

            return materials;
        }

        /// <summary>
        /// Given a set of material assets in the project and determines whether they are eligible for upgrade.
        /// </summary>
        /// <param name="upgraders">A list of <see cref="MaterialUpgrader"/> instances to use for determining upgradability.</param>
        /// <param name="materials"> A list of materials to check for upgrade options, if null Unity asumes that you want all project materials.</param>
        /// <returns>
        /// An enumerable of <see cref="MaterialUpgradeEntry"/> representing each material and whether it can be upgraded.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="upgraders"/> is null.</exception>
        internal static IEnumerable<MaterialUpgradeEntry> FetchUpgradeOptions(List<MaterialUpgrader> upgraders, List<Material> materials = null)
        {
            if (upgraders == null)
                throw new ArgumentNullException(nameof(upgraders));

            // If no materials are provided, gather all materials in the project.
            if (materials == null)
            {
                materials = FetchAllMaterialsInProject();
            }

            var materialInfo = GatherInfo(materials);
            GetUpgraderShaderPaths(upgraders, out var upgradersAvailable, out var shaderNamesToIgnore);

            return FetchUpgradeOptions(upgradersAvailable, shaderNamesToIgnore, materialInfo);
        }

        internal static string GenerateReason(MaterialInfo material)
        {
            string reason = $"No upgrader available to convert material '{material.Name}'";
            if (material.IsVariant)
            {
                reason += $", a variant of '{material.BaseMaterialName}',";
            }
            reason += $" using shader '{material.BaseMaterialName}'.";
            return reason;
        }

        static StringBuilder s_UpgradeLog = new StringBuilder();

        internal static string PerformUpgradeInternal(
            List<MaterialUpgradeEntry> materialUpgrades,
            List<MaterialUpgrader> upgraders,
            HashSet<string> shaderNamesToIgnore,
            string progressBarName,
            bool showProgressBar = true,
            UpgradeFlags flags = UpgradeFlags.None)
        {
            s_UpgradeLog.Clear();

            if (materialUpgrades.Count > 0)
            {
                s_UpgradeLog.AppendLine($"{progressBarName}");

                for (int materialIndex = 0; materialIndex < materialUpgrades.Count; ++materialIndex)
                {
                    var entry = materialUpgrades[materialIndex];

                    if (showProgressBar)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(progressBarName, $"({materialIndex} of {materialUpgrades.Count}) {entry.MaterialInfo.Name}", (float)materialIndex / (float)materialUpgrades.Count))
                        {
                            s_UpgradeLog.AppendLine("Process cancelled by user.");
                            break;
                        }
                    }

                    if (!entry.AvailableForUpgrade || shaderNamesToIgnore.Contains(entry.MaterialInfo.ShaderName))
                    {
                        s_UpgradeLog.AppendLine($"Skipping material: {entry.MaterialInfo.Name} - {entry.NotAvailableForUpgradeReason}");
                    }
                    else
                    {
                        s_UpgradeLog.AppendLine($"Upgrading material: {entry.MaterialInfo.Name} using shader: {entry.MaterialInfo.ShaderName}");
                        Upgrade(entry.MaterialInfo.Material, upgraders, flags);
                    }
                }

                AssetDatabase.SaveAssets();

                if (showProgressBar)
                    EditorUtility.ClearProgressBar();
            }

            return s_UpgradeLog.ToString();
        }

        static void PerformUpgrade(List<MaterialUpgradeEntry> materialUpgrades, List<MaterialUpgrader> upgraders, HashSet<string> shaderNamesToIgnore, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            if (materialUpgrades == null || materialUpgrades.Count == 0)
            {
                Debug.LogWarning("No materials found for upgrade.");
                return;
            }

            bool CanPerformUpgrade()
            {
                const string title = "Material Upgrader";
                const string message = "This operation will overwrite existing materials in your project.\n\nPlease ensure you have a backup before proceeding.";
                const string proceed = "Proceed";
                const string cancel = "Cancel";

                if (Application.isBatchMode)
                    return true;

                return EditorUtility.DisplayDialog(title, message, proceed, cancel);
            }

            if (CanPerformUpgrade())
            {
                string upgradeLog = PerformUpgradeInternal(materialUpgrades, upgraders, shaderNamesToIgnore, progressBarName, true, flags);
                Debug.Log(upgradeLog);
            }
        }

        #endregion

        #region Public API
        /// <summary>
        /// Checking if project folder contains any materials that can not be automatic upgraded.
        /// </summary>
        /// <param name="upgraders">List if <see cref="MaterialUpgrader"/></param>
        /// <returns>Returns true if at least one material uses a non-built-in shader</returns>
        public static bool ProjectContainsNonAutomaticUpgradePath(List<MaterialUpgrader> upgraders)
        {
            foreach (var material in AssetDatabaseHelper.FindAssets<Material>(".mat"))
            {
                if (!IsMaterialUpgradable(upgraders, material))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Upgrade the project folder.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            HashSet<string> shaderNamesToIgnore = new HashSet<string>();
            UpgradeProjectFolder(upgraders, shaderNamesToIgnore, progressBarName, flags);
        }

        /// <summary>
        /// Fetches all upgraders that support the given render pipeline asset type
        /// </summary>
        /// <param name="renderPipelineAssetType">The RP asset type</param>
        /// <returns>A list with all the upgraders in for the given pipeline</returns>
        public static List<MaterialUpgrader> FetchAllUpgradersForPipeline(Type renderPipelineAssetType)
        {
            if (!typeof(RenderPipelineAsset).IsAssignableFrom(renderPipelineAssetType))
                throw new ArgumentException($"Type '{renderPipelineAssetType.FullName}' must inherit from RenderPipelineAsset.", nameof(renderPipelineAssetType));

            return MaterialUpgraderRegistry.instance.GetMaterialUpgradersForPipeline(renderPipelineAssetType);
        }

        /// <summary>
        /// Fetches all materials in the project that are upgradable to the given render pipeline asset type
        /// </summary>
        /// <param name="renderPipelineAssetType">The RP asset type</param>
        /// <returns>A list with all the materials in the project</returns>
        public static List<Material> FetchAllUpgradableMaterialsForPipeline(Type renderPipelineAssetType)
        {
            var upgraders = FetchAllUpgradersForPipeline(renderPipelineAssetType);
            
            if (upgraders == null || upgraders.Count == 0)
            {
                Debug.LogWarning($"No material upgraders found for pipeline: {renderPipelineAssetType.Name}");
                return new List<Material>();
            }

            var allMaterials = FetchAllMaterialsInProject();

            var materialsAvailableForUpgrade = new List<Material>();
            foreach (var option in FetchUpgradeOptions(upgraders, allMaterials))
            {
                if (!option.AvailableForUpgrade)
                    continue;

                materialsAvailableForUpgrade.Add(option.MaterialInfo.Material);
            }

            return materialsAvailableForUpgrade;
        }

        /// <summary>
        /// Upgrade a material.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="upgrader">Material upgrader.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void Upgrade(Material material, MaterialUpgrader upgrader, UpgradeFlags flags)
        {
            using (ListPool<MaterialUpgrader>.Get(out List<MaterialUpgrader> upgraders))
            {
                upgraders.Add(upgrader);
                Upgrade(material, upgraders, flags);
            }
        }

        /// <summary>
        /// Upgrade a material.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="upgraders">List of Material upgraders.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void Upgrade(Material material, List<MaterialUpgrader> upgraders, UpgradeFlags flags)
        {
            if (material == null || upgraders == null || upgraders.Count == 0)
                return;

            string message = string.Empty;
            if (Upgrade(material, upgraders, flags, ref message))
                return;

            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Upgrade a material.
        /// </summary>
        /// <param name="material">Material to upgrade.</param>
        /// <param name="upgraders">List of Material upgraders.</param>
        /// <param name="flags">Material upgrader flags.</param>
        /// <param name="message">Error message to be outputted when no material upgraders are suitable for given material if the flags <see cref="UpgradeFlags.LogMessageWhenNoUpgraderFound"/> is used.</param>
        /// <returns>Returns true if the upgrader was found for the passed in material.</returns>
        public static bool Upgrade(Material material, List<MaterialUpgrader> upgraders, UpgradeFlags flags, ref string message)
        {
            if (material == null)
                return false;

            var upgrader = GetUpgrader(upgraders, material);

            if (upgrader != null)
            {
                upgrader.Upgrade(material, flags);
                return true;
            }
            if ((flags & UpgradeFlags.LogMessageWhenNoUpgraderFound) == UpgradeFlags.LogMessageWhenNoUpgraderFound)
            {
                message =
                    $"{material.name} material was not upgraded. There's no upgrader to convert {material.shader.name} shader to selected pipeline";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Upgrade the project folder.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="shaderNamesToIgnore">Set of shader names to ignore.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeProjectFolder(List<MaterialUpgrader> upgraders, HashSet<string> shaderNamesToIgnore, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            using (ListPool<MaterialUpgradeEntry>.Get(out var tmp))
            {
                tmp.AddRange(FetchUpgradeOptions(upgraders));
                PerformUpgrade(tmp, upgraders, shaderNamesToIgnore, progressBarName, flags);
            }
        }

        /// <summary>
        /// Upgrade the selection.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            HashSet<string> shaderNamesToIgnore = new HashSet<string>();
            UpgradeSelection(upgraders, shaderNamesToIgnore, progressBarName, flags);
        }

        /// <summary>
        /// Upgrade the selection.
        /// </summary>
        /// <param name="upgraders">List of upgraders.</param>
        /// <param name="shaderNamesToIgnore">Set of shader names to ignore.</param>
        /// <param name="progressBarName">Name of the progress bar.</param>
        /// <param name="flags">Material Upgrader flags.</param>
        public static void UpgradeSelection(List<MaterialUpgrader> upgraders, HashSet<string> shaderNamesToIgnore, string progressBarName, UpgradeFlags flags = UpgradeFlags.None)
        {
            using (ListPool<MaterialUpgradeEntry>.Get(out var tmp))
            {
                using (ListPool<Material>.Get(out var selectedMaterials))
                {
                    var selection = Selection.objects;
                    if (selection != null)
                    {
                        for (int i = 0; i < selection.Length; ++i)
                        {
                            if (selection[i] is Material m)
                                selectedMaterials.Add(m);
                        }
                    }

                    tmp.AddRange(FetchUpgradeOptions(upgraders, selectedMaterials));
                }

                PerformUpgrade(tmp, upgraders, shaderNamesToIgnore, progressBarName, flags);
            }
        }
        #endregion
    }
}
