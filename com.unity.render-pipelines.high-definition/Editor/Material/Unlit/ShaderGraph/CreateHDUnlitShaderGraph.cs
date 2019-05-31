using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateHDUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Unlit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
<<<<<<< HEAD
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateHDUnlitShaderGraph>(),
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new HDUnlitMasterNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
=======
            GraphUtil.CreateNewGraph(new HDUnlitMasterNode());
>>>>>>> master
        }
    }
}
