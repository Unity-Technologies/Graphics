using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Flags describing usage of an asset by its dependents, when that asset might have serialized shader property names.
    /// </summary>
    [Flags]
    public enum SerializedShaderPropertyUsage : byte
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
        UsedByAmbiguouslyUpgraded = 4 | 2
    }
    
    /// <summary>
    /// Class containing utility methods for upgrading assets affected by render pipeline migration.
    /// </summary>
    static class UpgradeUtility
    {
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
                if (upgrader.NewShader == null)
                    continue;

                if (!upgradePathBuilder.TryGetValue(upgrader.NewShader, out var allPaths))
                    upgradePathBuilder[upgrader.NewShader] = allPaths = new List<MaterialUpgrader>();
                allPaths.Add(upgrader);
            }
            return upgradePathBuilder.ToDictionary(kv => kv.Key, kv => kv.Value as IReadOnlyList<MaterialUpgrader>);
        }
    }
}
