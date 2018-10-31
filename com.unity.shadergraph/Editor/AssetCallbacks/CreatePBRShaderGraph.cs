using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.ShaderGraph
{
    public class CreatePBRShaderGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/PBR Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePBRShaderGraph>(),
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new MaterialGraph();
            graph.AddNode(new PBRMasterNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
