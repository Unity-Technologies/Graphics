using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    class PreviewTarget : ITarget
    {
        public string displayName => "PREVIEW";

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return false;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            subShader = null;
            return false;
        }

#region Passes
        public static class Passes
        {
            public static ShaderPass Preview = new ShaderPass()
            {
                // Definition
                referenceName = "SHADERPASS_PREVIEW",
                passInclude = "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewPass.hlsl",
                varyingsInclude = "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewVaryings.hlsl",

                // Pass setup
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                },
                defines = new List<string>()
                {
                    "SHADERGRAPH_PREVIEW 1",
                }
            };
        }
#endregion
    }
}
