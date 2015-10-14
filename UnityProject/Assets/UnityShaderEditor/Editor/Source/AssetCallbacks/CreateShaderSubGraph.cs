using System.IO;

namespace UnityEditor.MaterialGraph
{
    /*public class CreateShaderSubGraph : EndNameEditActionCallback
    {
        [MenuItem("Assets/Create/Shader Sub-Graph", false, 209)]
        public static void CreateMaterialSubGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateShaderSubGraph>(),
                                                                    "New Shader SubGraph.ShaderSubGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = CreateInstance<MaterialSubGraph>();
            graph.name = Path.GetFileName(pathName);
            AssetDatabase.CreateAsset(graph, pathName);
            graph.CreateSubAssets();
        }
    }*/
}
