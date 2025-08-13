using System.Collections.Generic;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Provider for a set of Materials Upgraders
    /// </summary>
    public interface IMaterialUpgradersProvider
    {
        /// <summary>
        /// Returns a list of custom MaterialUpgrader instances.
        /// </summary>
        /// <returns>A list of upgraders</returns>
        IEnumerable<MaterialUpgrader> GetUpgraders();
    }
}
