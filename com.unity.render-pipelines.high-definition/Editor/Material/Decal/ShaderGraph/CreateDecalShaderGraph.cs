using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateDecalShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Decal Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
<<<<<<< HEAD
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateDecalShaderGraph>(),
                "New Shader Graph.ShaderGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new DecalMasterNode());
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
=======
            GraphUtil.CreateNewGraph(new DecalMasterNode());
>>>>>>> master
        }
    }
}
