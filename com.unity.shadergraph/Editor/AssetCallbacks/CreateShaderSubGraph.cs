using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.ShaderGraph
{
    class CreateShaderSubGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/Sub Graph", false, 208)]
        public static void CreateMaterialSubGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateShaderSubGraph>(),
                string.Format("New Shader Sub Graph.{0}", ShaderSubGraphImporter.Extension), null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new GraphData { isSubGraph = true };
            var outputNode = new SubGraphOutputNode();
            graph.AddNode(outputNode);
            graph.outputNode = outputNode;
            outputNode.AddSlot(ConcreteSlotValueType.Vector4);
            graph.path = "Sub Graphs";
            FileUtilities.WriteShaderGraphToDisk(pathName, graph);
            AssetDatabase.Refresh();
        }
    }
}
