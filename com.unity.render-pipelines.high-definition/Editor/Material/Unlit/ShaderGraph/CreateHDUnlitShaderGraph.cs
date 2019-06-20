using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateHDUnlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Unlit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new HDUnlitMasterNode());
        }
    }
}
