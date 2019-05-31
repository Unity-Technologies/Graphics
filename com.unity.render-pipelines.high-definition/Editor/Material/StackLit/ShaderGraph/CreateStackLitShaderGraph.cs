using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateStackLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/StackLit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
<<<<<<< HEAD
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateStackLitShaderGraph>(),
                "New Shader Graph.ShaderGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new StackLitMasterNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
=======
            GraphUtil.CreateNewGraph(new StackLitMasterNode());
>>>>>>> master
        }
    }
}
