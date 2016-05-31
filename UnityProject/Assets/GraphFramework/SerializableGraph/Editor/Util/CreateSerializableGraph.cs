using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing
{
    public class CreateSerializableGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Serializable Graph", false, 207)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateSerializableGraph>(),
                "New Shader Graph.ShaderGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = CreateInstance<SerializableGraphAsset>();
            graph.name = Path.GetFileName(pathName);
            AssetDatabase.CreateAsset(graph, pathName);
        }
    }
}
