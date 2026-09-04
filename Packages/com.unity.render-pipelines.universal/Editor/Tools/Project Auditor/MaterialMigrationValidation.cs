using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Other")]
    class UnmigratedMaterialsAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0402 = nameof(URP0402);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0402,
            "URP: Materials: Materials using Built-in shaders detected",
            Areas.Quality | Areas.GPU,
            "One or more materials in the project are still using Built-in render pipeline shaders. These materials should be converted to use URP-optimized shaders for better performance and compatibility. Use the Render Pipeline Converter (Window > Rendering > Render Pipeline Converter) to upgrade materials.",
            "Convert materials to URP using Window > Rendering > Render Pipeline Converter"
        );

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            if (!CheckForUnmigratedMaterials())
                yield return new RenderingSettingsIssue(URP0402);
        }

        static bool CheckForUnmigratedMaterials()
        {
            // Get all built-in to URP upgraders
            var upgraders = BuiltInToURP3DMaterialUpgrader.FetchMaterialUpgraders();
            if (upgraders == null || upgraders.Count == 0)
                return true; // No upgraders defined, consider valid

            // Build a set of old shader paths that should be upgraded
            var builtInShaderPaths = new HashSet<string>();
            foreach (var upgrader in upgraders)
            {
                if (!string.IsNullOrEmpty(upgrader.OldShaderPath))
                {
                    builtInShaderPaths.Add(upgrader.OldShaderPath);
                }
            }

            if (builtInShaderPaths.Count == 0)
                return true; // No old shaders to check

            // Find all materials in the project
            var materials = AssetDatabaseHelper.FindAssets<Material>();
            foreach (var material in materials)
            {
                if (material != null && material.shader != null)
                {
                    var shaderName = material.shader.name;

                    // Check if this material is using a built-in shader that should be upgraded
                    if (builtInShaderPaths.Contains(shaderName))
                    {
                        return false; // Found an unmigrated material
                    }
                }
            }

            return true; // All materials are using URP shaders
        }
    }
}
