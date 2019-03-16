using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class CreateHairShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/Hair Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new HairMasterNode());
        }
    }
}
