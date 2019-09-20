using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using ShaderPass = UnityEditor.ShaderGraph.Internal.ShaderPass;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPDecalTarget : ITargetVariant<MeshTarget>
    {
        public string displayName => "HDRP";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => string.Empty;

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return pipelineAsset is HDRenderPipelineAsset;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            switch(masterNode)
            {
                case DecalMasterNode decalMasterNode:
                    subShader = new DecalSubShader();
                    return true;
                default:
                    subShader = null;
                    return false;
            }
        }

        public static string[] ColorMasks = new string[8]
        {
            "ColorMask 0 2 ColorMask 0 3",      // nothing
            "ColorMask R 2 ColorMask R 3",      // metal
            "ColorMask G 2 ColorMask G 3",      // AO
            "ColorMask RG 2 ColorMask RG 3",    // metal + AO
            "ColorMask BA 2 ColorMask 0 3",     // smoothness
            "ColorMask RBA 2 ColorMask R 3",    // metal + smoothness
            "ColorMask GBA 2 ColorMask G 3",    // AO + smoothness
            "ColorMask RGBA 2 ColorMask RG 3",  // metal + AO + smoothness
        };

#region Passes
        public static class Passes
        {
            // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - s_MaterialDecalNames and s_MaterialDecalSGNames array
            // and DecalSet.InitializeMaterialValues()
            public static ShaderPass Projector3RT = new ShaderPass()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.AlbedoSlotId,
                    DecalMasterNode.BaseColorOpacitySlotId,
                    DecalMasterNode.NormalSlotId,
                    DecalMasterNode.NormaOpacitySlotId,
                    DecalMasterNode.MetallicSlotId,
                    DecalMasterNode.AmbientOcclusionSlotId,
                    DecalMasterNode.SmoothnessSlotId,
                    DecalMasterNode.MAOSOpacitySlotId,
                },

                // Render state overrides
                BlendOverride = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                CullOverride = "Cull Front",
                ZTestOverride = "ZTest Greater",
                ZWriteOverride = "ZWrite Off",
                ColorMaskOverride = ColorMasks[4], // Smoothness only
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "DECALS_3RT",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass Projector4RT = new ShaderPass()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.AlbedoSlotId,
                    DecalMasterNode.BaseColorOpacitySlotId,
                    DecalMasterNode.NormalSlotId,
                    DecalMasterNode.NormaOpacitySlotId,
                    DecalMasterNode.MetallicSlotId,
                    DecalMasterNode.AmbientOcclusionSlotId,
                    DecalMasterNode.SmoothnessSlotId,
                    DecalMasterNode.MAOSOpacitySlotId,
                },

                // Render state overrides
                BlendOverride = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor",
                CullOverride = "Cull Front",
                ZTestOverride = "ZTest Greater",
                ZWriteOverride = "ZWrite Off",
                StencilOverride = new List<string>()
                {
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "DECALS_4RT",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass ProjectorEmissive = new ShaderPass()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.EmissionSlotId
                },

                // Render state overrides
                CullOverride = "Cull Front",
                ZTestOverride = "ZTest Greater",
                ZWriteOverride = "ZWrite Off",
                BlendOverride = "Blend 0 SrcAlpha One",

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass Mesh3RT = new ShaderPass()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.AlbedoSlotId,
                    DecalMasterNode.BaseColorOpacitySlotId,
                    DecalMasterNode.NormalSlotId,
                    DecalMasterNode.NormaOpacitySlotId,
                    DecalMasterNode.MetallicSlotId,
                    DecalMasterNode.AmbientOcclusionSlotId,
                    DecalMasterNode.SmoothnessSlotId,
                    DecalMasterNode.MAOSOpacitySlotId,
                },

                // Render state overrides
                BlendOverride = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha",
                ZTestOverride = "ZTest LEqual",
                ZWriteOverride = "ZWrite Off",
                ColorMaskOverride = ColorMasks[4], // Smoothness only
                StencilOverride = new List<string>()
                {
                    "// Stencil setup",
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",
                    "AttributesMesh.uv0",
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "DECALS_3RT",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass Mesh4RT = new ShaderPass()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.AlbedoSlotId,
                    DecalMasterNode.BaseColorOpacitySlotId,
                    DecalMasterNode.NormalSlotId,
                    DecalMasterNode.NormaOpacitySlotId,
                    DecalMasterNode.MetallicSlotId,
                    DecalMasterNode.AmbientOcclusionSlotId,
                    DecalMasterNode.SmoothnessSlotId,
                    DecalMasterNode.MAOSOpacitySlotId,
                },

                // Render state overrides
                BlendOverride = "Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor",
                ZTestOverride = "ZTest LEqual",
                ZWriteOverride = "ZWrite Off",
                StencilOverride = new List<string>()
                {
                    "// Stencil setup",
                    "Stencil",
                    "{",
                    $"    WriteMask {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    $"    Ref  {(int)HDRenderPipeline.StencilBitMask.Decals}",
                    "    Comp Always",
                    "    Pass Replace",
                    "}"
                },

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",
                    "AttributesMesh.uv0",
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                defines = new List<string>()
                {
                    "DECALS_4RT",
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass MeshEmissive = new ShaderPass()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = false,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.AlbedoSlotId,
                    DecalMasterNode.BaseColorOpacitySlotId,
                    DecalMasterNode.NormalSlotId,
                    DecalMasterNode.NormaOpacitySlotId,
                    DecalMasterNode.MetallicSlotId,
                    DecalMasterNode.AmbientOcclusionSlotId,
                    DecalMasterNode.SmoothnessSlotId,
                    DecalMasterNode.MAOSOpacitySlotId,
                    DecalMasterNode.EmissionSlotId,
                },

                // Render state overrides
                BlendOverride = "Blend 0 SrcAlpha One",
                ZTestOverride = "ZTest LEqual",
                ZWriteOverride = "ZWrite Off",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",
                    "AttributesMesh.uv0",
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };

            public static ShaderPass Preview = new ShaderPass()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "ForwardOnly",
                passInclude = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl",
                useInPreview = true,

                // Port mask
                pixelPorts = new List<int>()
                {
                    DecalMasterNode.AlbedoSlotId,
                    DecalMasterNode.BaseColorOpacitySlotId,
                    DecalMasterNode.NormalSlotId,
                    DecalMasterNode.NormaOpacitySlotId,
                    DecalMasterNode.MetallicSlotId,
                    DecalMasterNode.AmbientOcclusionSlotId,
                    DecalMasterNode.SmoothnessSlotId,
                    DecalMasterNode.MAOSOpacitySlotId,
                    DecalMasterNode.EmissionSlotId,
                },

                // Render state overrides
                ZTestOverride = "ZTest LEqual",

                // Required fields
                requiredAttributes = new List<string>()
                {
                    "AttributesMesh.normalOS",
                    "AttributesMesh.tangentOS",
                    "AttributesMesh.uv0",
                },
                requiredVaryings = new List<string>()
                {
                    "FragInputs.tangentToWorld",
                    "FragInputs.positionRWS",
                    "FragInputs.texCoord0",
                },

                // Pass setup
                pragmas = new List<string>()
                {
                    "#pragma target 4.5",
                    "only_renderers d3d11 ps4 xboxone vulkan metal switch",
                    "multi_compile_instancing"
                },
                includes = new List<string>()
                {
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl",
                    "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl",
                    "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl",
                    "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl",
                },

                // Custom template
                passTemplatePath = $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template",
                sharedTemplateDirectory = $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph",
            };
        }
#endregion
    }
}
