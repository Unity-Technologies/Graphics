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
        public string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template";
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph";

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
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Projector3RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals3RT,
                includes = Includes.Default,
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
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Projector4RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals4RT,
                includes = Includes.Default,
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
                pixelPorts = PixelPorts.Emissive,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.ProjectorEmissive,
                pragmas = Pragmas.Instanced,
                includes = Includes.Default,
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
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Mesh3RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals3RT,
                includes = Includes.Default,
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
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Mesh4RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals4RT,
                includes = Includes.Default,
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
                pixelPorts = PixelPorts.MeshEmissive,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.MeshEmissive, 
                pragmas = Pragmas.Instanced,
                includes = Includes.Default,
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
                pixelPorts = PixelPorts.MeshEmissive,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = FieldDependencies.Default,

                // Render state overrides
                renderStates = RenderStates.Preview,
                pragmas = Pragmas.Instanced,
                includes = Includes.Default,
            };
        }
#endregion

#region PortMasks
        static class PixelPorts
        {
            public static int[] Default = new int[]
            {
                DecalMasterNode.AlbedoSlotId,
                DecalMasterNode.BaseColorOpacitySlotId,
                DecalMasterNode.NormalSlotId,
                DecalMasterNode.NormaOpacitySlotId,
                DecalMasterNode.MetallicSlotId,
                DecalMasterNode.AmbientOcclusionSlotId,
                DecalMasterNode.SmoothnessSlotId,
                DecalMasterNode.MAOSOpacitySlotId,
            };

            public static int[] Emissive = new int[]
            {
                DecalMasterNode.EmissionSlotId
            };

            public static int[] MeshEmissive = new int[]
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
            };
        }
#endregion

#region RequiredFields
        static class RequiredFields
        {
            public static IField[] Mesh = new IField[]
            {   
                HDRPMeshTarget.ShaderStructs.AttributesMesh.normalOS,
                HDRPMeshTarget.ShaderStructs.AttributesMesh.tangentOS,
                HDRPMeshTarget.ShaderStructs.AttributesMesh.uv0,
                HDRPMeshTarget.ShaderStructs.FragInputs.tangentToWorld,
                HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS,
                HDRPMeshTarget.ShaderStructs.FragInputs.texCoord0,
            };
        }
#endregion

#region StructDescriptors
        static class StructDescriptors
        {
            public static StructDescriptor[] Default = new StructDescriptor[]
            {
                HDRPMeshTarget.AttributesMesh,
                HDRPMeshTarget.VaryingsMeshToPS,
                HDRPMeshTarget.SurfaceDescriptionInputs,
                HDRPMeshTarget.VertexDescriptionInputs,
            };
        }
#endregion

#region RenderStates
        static class RenderStates
        {
            readonly static string[] s_ColorMasks = new string[8]
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

            public static ConditionalRenderState[] Projector3RT = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha")),
                new ConditionalRenderState(RenderState.Cull(Cull.Front)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Greater)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[4])),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Ref = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] Projector4RT = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor")),
                new ConditionalRenderState(RenderState.Cull(Cull.Front)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Greater)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Ref = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                })),

                // ColorMask per Affects Channel
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[0]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[1]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[2]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[3]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[4]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[5]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[6]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[7]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
            };

            public static ConditionalRenderState[] ProjectorEmissive = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend("Blend 0 SrcAlpha One")),
                new ConditionalRenderState(RenderState.Cull(Cull.Front)),
                new ConditionalRenderState(RenderState.ZTest(ZTest.Greater)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
            };

            public static ConditionalRenderState[] Mesh3RT = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha")),
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[4])),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Ref = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                })),
            };

            public static ConditionalRenderState[] Mesh4RT = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor")),
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
                new ConditionalRenderState(RenderState.Stencil(new Stencil()
                {
                    WriteMask = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Ref = ((int)HDRenderPipeline.StencilBitMask.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                })),
                
                // ColorMask per Affects Channel
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[0]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[1]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[2]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[3]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, false) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[4]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[5]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[6]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, false), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
                new ConditionalRenderState(RenderState.ColorMask(s_ColorMasks[7]), new FieldCondition[] { 
                    new FieldCondition(HDRPShaderGraphFields.AffectsMetal, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsAO, true), 
                    new FieldCondition(HDRPShaderGraphFields.AffectsSmoothness, true) }),
            };

            public static ConditionalRenderState[] MeshEmissive = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.Blend("Blend 0 SrcAlpha One")),
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
                new ConditionalRenderState(RenderState.ZWrite(ZWrite.Off)),
            };

            public static ConditionalRenderState[] Preview = new ConditionalRenderState[]
            {
                new ConditionalRenderState(RenderState.ZTest(ZTest.LEqual)),
            };
        }
#endregion

#region Pragmas
        static class Pragmas
        {
            public static ConditionalPragma[] Instanced = new ConditionalPragma[]
            {
                new ConditionalPragma(Pragma.Target(4.5)),
                new ConditionalPragma(Pragma.OnlyRenderers(new Platform[] {Platform.D3D11, Platform.PS4, Platform.XboxOne, Platform.Vulkan, Platform.Metal, Platform.Switch})),
                new ConditionalPragma(Pragma.MultiCompileInstancing),
            };
        }
#endregion

#region Defines
        static class Defines
        {
            public static ConditionalDefine[] Decals3RT = new ConditionalDefine[]
            {
                new ConditionalDefine(KeywordDescriptors.Decals3RT, 1),
            };

            public static ConditionalDefine[] Decals4RT = new ConditionalDefine[]
            {
                new ConditionalDefine(KeywordDescriptors.Decals4RT, 1),
            };
        }
#endregion

#region Includes
        static class Includes
        {
            public static ConditionalInclude[] Default = new ConditionalInclude[]
            {
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl")),
                new ConditionalInclude(Include.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl")),
            };
        }
#endregion

#region KeywordDescriptors
        static class KeywordDescriptors
        {
            public static KeywordDescriptor Decals3RT = new KeywordDescriptor()
            {
                displayName = "Decals 3RT",
                referenceName = "DECALS_3RT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };

            public static KeywordDescriptor Decals4RT = new KeywordDescriptor()
            {
                displayName = "Decals 4RT",
                referenceName = "DECALS_4RT",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                scope = KeywordScope.Global,
            };
        }
#endregion

#region Dependencies
        static class FieldDependencies
        {
            public static FieldDependency[] Default = new FieldDependency[]
            {
                //Standard Varying Dependencies
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.positionRWS,   HDRPMeshTarget.ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.normalWS,      HDRPMeshTarget.ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.tangentWS,     HDRPMeshTarget.ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord0,     HDRPMeshTarget.ShaderStructs.AttributesMesh.uv0),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord1,     HDRPMeshTarget.ShaderStructs.AttributesMesh.uv1),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord2,     HDRPMeshTarget.ShaderStructs.AttributesMesh.uv2),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord3,     HDRPMeshTarget.ShaderStructs.AttributesMesh.uv3),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.color,         HDRPMeshTarget.ShaderStructs.AttributesMesh.color),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.instanceID,    HDRPMeshTarget.ShaderStructs.AttributesMesh.instanceID),

                //Tessellation Varying Dependencies
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.positionRWS,   HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.positionRWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.normalWS,      HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.normalWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.tangentWS,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.tangentWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord0,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord0),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord1,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord1),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord2,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord2),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord3,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord3),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.color,         HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.color),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.instanceID,    HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.instanceID),
                
                //Tessellation Varying Dependencies, TODO: Why is this loop created?
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.tangentWS,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.tangentWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord0,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord0),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord1,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord1),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord2,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord2),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.texCoord3,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord3),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.color,         HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.color),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.VaryingsMeshToDS.instanceID,    HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.instanceID),
                
                //FragInput dependencies
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS,        HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.positionRWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.tangentToWorld,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.tangentWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.tangentToWorld,     HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.normalWS),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.texCoord0,          HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord0),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.texCoord1,          HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord1),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.texCoord2,          HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord2),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.texCoord3,          HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.texCoord3),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.color,              HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.color),
                new FieldDependency(HDRPMeshTarget.ShaderStructs.FragInputs.IsFrontFace,        HDRPMeshTarget.ShaderStructs.VaryingsMeshToPS.cullFace),
                
                //Vertex Description Dependencies
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,            HDRPMeshTarget.ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,             HDRPMeshTarget.ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,           HDRPMeshTarget.ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,            HDRPMeshTarget.ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         HDRPMeshTarget.ShaderStructs.AttributesMesh.normalOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,         HDRPMeshTarget.ShaderStructs.AttributesMesh.tangentOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,          MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,          HDRPMeshTarget.ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,           HDRPMeshTarget.ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,   HDRPMeshTarget.ShaderStructs.AttributesMesh.positionOS),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,            MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),

                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,      MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,                          HDRPMeshTarget.ShaderStructs.AttributesMesh.uv0),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,                          HDRPMeshTarget.ShaderStructs.AttributesMesh.uv1),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,                          HDRPMeshTarget.ShaderStructs.AttributesMesh.uv2),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,                          HDRPMeshTarget.ShaderStructs.AttributesMesh.uv3),
                new FieldDependency(MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,                  HDRPMeshTarget.ShaderStructs.AttributesMesh.color),

                //Surface Description Dependencies
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,             HDRPMeshTarget.ShaderStructs.FragInputs.tangentToWorld),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,            MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,              MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,            HDRPMeshTarget.ShaderStructs.FragInputs.tangentToWorld),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,             MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,          HDRPMeshTarget.ShaderStructs.FragInputs.tangentToWorld),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,         MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,           MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
    
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,           HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,   HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,          HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,            HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection,      HDRPMeshTarget.ShaderStructs.FragInputs.positionRWS),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,     MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,       MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,    MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal),

                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,               MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,                          HDRPMeshTarget.ShaderStructs.FragInputs.texCoord0),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,                          HDRPMeshTarget.ShaderStructs.FragInputs.texCoord1),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,                          HDRPMeshTarget.ShaderStructs.FragInputs.texCoord2),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,                          HDRPMeshTarget.ShaderStructs.FragInputs.texCoord3),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,                  HDRPMeshTarget.ShaderStructs.FragInputs.color),
                new FieldDependency(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,                     HDRPMeshTarget.ShaderStructs.FragInputs.IsFrontFace),
            };
        }
#endregion
    }
}
