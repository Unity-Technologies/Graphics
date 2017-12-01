using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    public class CreateLayeredShaderGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Layered Shader Graph", false, 208)]
        public static void CreateLayeredGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateLayeredShaderGraph>(),
                "New Layerd Shader Graph.LayeredShaderGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new LayeredShaderGraph();
            graph.AddNode(new LayerWeightsOutputNode());
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
