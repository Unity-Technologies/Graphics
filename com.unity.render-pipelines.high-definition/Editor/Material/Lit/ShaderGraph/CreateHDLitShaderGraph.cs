using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateHDLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Lit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
<<<<<<< HEAD
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateHDLitShaderGraph>(),
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new HDLitMasterNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
=======
            GraphUtil.CreateNewGraph(new HDLitMasterNode());
>>>>>>> master
        }
    }
}
