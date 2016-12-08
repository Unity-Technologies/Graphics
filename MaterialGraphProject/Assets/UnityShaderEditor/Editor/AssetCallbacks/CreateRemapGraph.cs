using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    public class CreateRemapGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Remap Graph", false, 209)]
        public static void CreateMaterialRemapGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateRemapGraph>(),
                "New Remap-Graph.remapGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = CreateInstance<MaterialRemapAsset>();
            graph.name = Path.GetFileName(pathName);
            AssetDatabase.CreateAsset(graph, pathName);
        }
    }
}
