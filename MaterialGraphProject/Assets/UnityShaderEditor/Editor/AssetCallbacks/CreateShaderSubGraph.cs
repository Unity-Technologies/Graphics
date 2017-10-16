using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    public class CreateShaderSubGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader Sub Graph", false, 208)]
        public static void CreateMaterialSubGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateShaderSubGraph>(),
                "New Shader Sub-Graph.ShaderSubGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new SubGraph();
            graph.AddNode(new SubGraphOutputNode());
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
