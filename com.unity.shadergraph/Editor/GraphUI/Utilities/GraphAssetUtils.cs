using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI.Utilities
{
    public class GraphAssetUtils
    {
        [MenuItem("Assets/Create/Shader Graph/Blank Shader Graph", priority = CoreUtils.Sections.section1 +  CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankShaderGraph()
        {
            var graphAssetModel = ShaderGraphOnboardingProvider.CreateBlankShaderGraph();
        }
    }
}
