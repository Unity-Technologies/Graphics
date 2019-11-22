using System.IO;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class CreateCustomBlitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HDRP/CustomBlit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            GraphUtil.CreateNewGraph(new CustomBlitMasterNode());
        }
    }
}
