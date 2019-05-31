using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateFabricShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Fabric Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
<<<<<<< HEAD
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateFabricShaderGraph>(),
                "New Shader Graph.ShaderGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new FabricMasterNode());
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
=======
            GraphUtil.CreateNewGraph(new FabricMasterNode());
>>>>>>> master
        }
    }
}
