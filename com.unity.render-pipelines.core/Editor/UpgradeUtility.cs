using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Rendering
{
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
