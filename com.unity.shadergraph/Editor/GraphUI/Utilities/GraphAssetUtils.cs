
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class GraphAssetUtils
    {
        public class CreateAssetAction : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                ShaderGraphAsset.HandleCreate(pathName);
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
                Selection.activeObject = obj;
            }
        }

        [MenuItem("Assets/Create/Shader Graph 2/Blank Shader Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBlankGraphInProjectWindow()
        {
            var newGraphAction = ScriptableObject.CreateInstance<CreateAssetAction>();

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
