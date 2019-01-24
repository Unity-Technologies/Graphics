using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.ShaderGraph
{
    class CreateUnlitShaderGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/Unlit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUnlitShaderGraph>(),
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData();
            graph.AddNode(new UnlitMasterNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
