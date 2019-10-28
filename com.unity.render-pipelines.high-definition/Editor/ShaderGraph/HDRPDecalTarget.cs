using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using PassDescriptor = UnityEditor.ShaderGraph.Internal.PassDescriptor;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPDecalTarget : ITargetImplementation
    {
        public Type targetType => typeof(DecalTarget);
        public string displayName => "HDRP";
        public string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template";
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph";

        public bool IsValid(IMasterNode masterNode)
        {
            return (masterNode is DecalMasterNode);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("61d739b0177943f4d858e09ae4b69ea2")); // DecalTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("21bb2072667892445b27f3e9aad497af")); // HDRPDecalTarget

            switch(context.masterNode)
            {
                case DecalMasterNode decalMasterNode:
                    context.SetupSubShader(SubShaders.Decal);
                    break;
            }
        }

#region SubShaders
        public static class SubShaders
        {
            const string kPipelineTag = HDRenderPipeline.k_ShaderTagName;
            public static SubShaderDescriptor Decal = new SubShaderDescriptor()
            {
                pipelineTag = kPipelineTag,
                passes = new ConditionalPass[]
                {
                    new ConditionalPass(Passes.Projector3RT, new FieldCondition(HDRPShaderGraphFields.DecalDefault, true)),
                    new ConditionalPass(Passes.Projector4RT, new FieldCondition(HDRPShaderGraphFields.DecalDefault, true)),
                    new ConditionalPass(Passes.ProjectorEmissive, new FieldCondition(HDRPShaderGraphFields.AffectsEmission, true)),
                    new ConditionalPass(Passes.Mesh3RT, new FieldCondition(HDRPShaderGraphFields.DecalDefault, true)),
                    new ConditionalPass(Passes.Mesh4RT, new FieldCondition(HDRPShaderGraphFields.DecalDefault, true)),
                    new ConditionalPass(Passes.MeshEmissive, new FieldCondition(HDRPShaderGraphFields.AffectsEmission, true)),
                    new ConditionalPass(Passes.Preview, new FieldCondition(Fields.IsPreview, true)),
                },
            };
        }
#endregion

#region Passes
        public static class Passes
        {
            // CAUTION: c# code relies on the order in which the passes are declared, any change will need to be reflected in Decalsystem.cs - s_MaterialDecalNames and s_MaterialDecalSGNames array
            // and DecalSet.InitializeMaterialValues()
            public static PassDescriptor Projector3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector3RT],
                useInPreview = false,

                // Port mask
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Projector3RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals3RT,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
            };

            public static PassDescriptor Projector4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                useInPreview = false,

                // Port mask
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Projector4RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals4RT,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
            };

            public static PassDescriptor ProjectorEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                useInPreview = false,

                // Port mask
                pixelPorts = PixelPorts.Emissive,

                //Fields
                structs = StructDescriptors.Default,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.ProjectorEmissive,
                pragmas = Pragmas.Instanced,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
            };

            public static PassDescriptor Mesh3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                useInPreview = false,

                // Port mask
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Mesh3RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals3RT,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
            };

            public static PassDescriptor Mesh4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                useInPreview = false,

                // Port mask
                pixelPorts = PixelPorts.Default,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.Mesh4RT,
                pragmas = Pragmas.Instanced,
                defines = Defines.Decals4RT,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
            };

            public static PassDescriptor MeshEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                useInPreview = false,

                // Port mask
                pixelPorts = PixelPorts.MeshEmissive,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Conditional State
                renderStates = RenderStates.MeshEmissive, 
                pragmas = Pragmas.Instanced,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
            };

            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Port mask
                pixelPorts = PixelPorts.MeshEmissive,

                //Fields
                structs = StructDescriptors.Default,
                requiredFields = RequiredFields.Mesh,
                fieldDependencies = HDRPMeshTarget.FieldDependencies.Default,

                // Render state overrides
                renderStates = RenderStates.Preview,
                pragmas = Pragmas.Instanced,
                preGraphIncludes = PreGraphIncludes.Default,
                postGraphIncludes = PostGraphIncludes.Default,
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
                new ConditionalRenderState(RenderState.Stencil(new StencilDescriptor()
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
                new ConditionalRenderState(RenderState.Stencil(new StencilDescriptor()
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
                new ConditionalRenderState(RenderState.Stencil(new StencilDescriptor()
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
                new ConditionalRenderState(RenderState.Stencil(new StencilDescriptor()
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
                new ConditionalPragma(Pragma.Vertex("Vert")),
                new ConditionalPragma(Pragma.Fragment("Frag")),
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
        static class PreGraphIncludes
        {
            public static ConditionalInclude[] Default = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl")),
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl")),
            };
        }
        public class PostGraphIncludes
        {
            public static ConditionalInclude[] Default = new ConditionalInclude[]
            {
                new ConditionalInclude(IncludeDescriptor.File("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl")),
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
    }
}
