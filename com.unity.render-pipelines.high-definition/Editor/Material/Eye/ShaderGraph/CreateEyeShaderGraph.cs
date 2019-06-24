using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateEyeShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Eye Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new EyeMasterNode());
        }
    }
}
