
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class GraphAssetUtils
    {
        [MenuItem("Assets/Create/Shader Graph 2/Blank Shader Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<ShaderGraphAsset.CreateAssetAction>();

            var path = "";
            var obj = Selection.activeObject;
            path = obj == null ? "Assets" : AssetDatabase.GetAssetPath(obj.GetInstanceID());

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                newGraphAction,
                $"{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}",
                null,
                null);
        }
    }
}
