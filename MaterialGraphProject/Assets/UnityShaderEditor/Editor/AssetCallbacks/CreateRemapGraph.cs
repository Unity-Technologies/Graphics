using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph
{
    public class CreateRemapGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader Remap Graph", false, 209)]
        public static void CreateMaterialRemapGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateRemapGraph>(),
                "New Remap-Graph.ShaderRemapGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new MasterRemapGraph();
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
