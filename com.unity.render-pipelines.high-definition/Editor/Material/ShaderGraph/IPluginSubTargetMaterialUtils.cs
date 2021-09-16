using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class PluginMaterial
    {
        public enum GenericVersions
        {
            NeverMigrated = 0,
            Initial = 1,
        }
        public const int k_NeverMigratedVersion = 0;
    }
    interface IPluginSubTargetMaterialUtils
    {
        int latestMaterialVersion { get; }
        int latestSubTargetVersion { get; }
        // The caller assumes the following exits with the material now migrated to latestVersion
        // currentVersion must be a value that latestMaterialVersion has previously returned, or 0
        // for a material never migrated.
        bool MigrateMaterial(Material material, int currentVersion);
    }
}
