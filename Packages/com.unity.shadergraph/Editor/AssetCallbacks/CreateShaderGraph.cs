using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    static class CreateShaderGraph
    {
        [MenuItem("Assets/Create/Shader Graph/Blank Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankShaderGraph()
        {
            GraphUtil.CreateNewGraph();
        }

        [MenuItem("Assets/Create/Shader Graph/From Template...")]
        public static void CreateFromTemplate()
        {
            GraphViewTemplateWindow.ShowCreateFromTemplate(new ShaderGraphTemplateHelper(), GraphUtil.CreateAndRenameGraphFromTemplate, showSaveDialog: false);
        }
    }
}
