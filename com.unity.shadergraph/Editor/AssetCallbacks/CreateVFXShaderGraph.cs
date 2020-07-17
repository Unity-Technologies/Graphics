#if VFX_GRAPH_10_0_0_OR_NEWER
using System;
using UnityEditor.ShaderGraph;
namespace UnityEditor.ShaderGraph
{
    static class CreateVFXShaderGraph
    {
        [MenuItem("Assets/Create/Shader/VFX Shader Graph", false, 208)]
        public static void CreateVFXGraph()
        {
            var target = (VFXTarget)Activator.CreateInstance(typeof(VFXTarget));

            var blockDescriptors = new [] 
            { 
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new [] {target}, blockDescriptors);
        }
    }
}
#endif
