using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDShaderKernels
    {
        static public KernelDescriptor GenerateVertexSetup()
        {
            return new KernelDescriptor
            {
                name = "VertexSetup",
                templatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/Kernels/VertexSetup.template",
                passDescriptorReference = HDShaderPasses.GenerateShadowCaster(false, false, false)
            };
        }
    }
}
