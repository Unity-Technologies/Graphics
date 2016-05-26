using System.IO;
using UnityEditor.Graphing;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace UnityEditor.MaterialGraph
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

    public class SerializableGraphAsset : ScriptableObject
    {
        [SerializeField]
        private SerializableGraph m_Graph;

        public SerializableGraph graph
        {
            get { return m_Graph; }
        }
    }
}
