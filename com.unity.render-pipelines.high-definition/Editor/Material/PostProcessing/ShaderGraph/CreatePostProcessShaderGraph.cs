using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreatePostProcessShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/PostProcess Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new PostProcessMasterNode());
        }
    }
}
