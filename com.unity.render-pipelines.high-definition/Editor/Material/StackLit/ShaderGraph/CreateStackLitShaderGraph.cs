using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateStackLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/StackLit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new StackLitMasterNode());
        }
    }
}
