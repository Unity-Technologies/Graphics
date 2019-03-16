using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateHDLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Lit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new HDLitMasterNode());
        }
    }
}
