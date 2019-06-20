using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateFabricShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Fabric Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new FabricMasterNode());
        }
    }
}
