using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class DecalSubTarget : HDSubTarget, ILegacyTarget, IRequiresData<DecalData>
    {
        public DecalSubTarget() => displayName = "Decal";

        public static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Decal/ShaderGraph/DecalPass.template";
        public static string passTemplateMaterialDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates";

        protected override string templatePath => passTemplatePath;
        protected override string templateMaterialDirectory => passTemplateMaterialDirectory;
        protected override string subTargetAssetGuid => "3ec927dfcb5d60e4883b2c224857b6c2";
        protected override string customInspector => "Rendering.HighDefinition.DecalGUI";
        protected override string renderType => HDRenderTypeTags.Opaque.ToString();
        protected override string renderQueue => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(HDRenderQueue.RenderQueueType.Opaque, decalData.drawOrder, false));
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Lit;

        // Material Data
        DecalData m_DecalData;

        // Interface Properties
        DecalData IRequiresData<DecalData>.data
        {
            get => m_DecalData;
            set => m_DecalData = value;
        }

        // Public properties
        public DecalData decalData
        {
            get => m_DecalData;
            set => m_DecalData = value;
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return SubShaders.Decal;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            // Decal properties
            context.AddField(HDFields.AffectsAlbedo,        decalData.affectsAlbedo);
            context.AddField(HDFields.AffectsNormal,        decalData.affectsNormal);
            context.AddField(HDFields.AffectsEmission,      decalData.affectsEmission);
            context.AddField(HDFields.AffectsMetal,         decalData.affectsMetal);
            context.AddField(HDFields.AffectsAO,            decalData.affectsAO);
            context.AddField(HDFields.AffectsSmoothness,    decalData.affectsSmoothness);
            context.AddField(HDFields.AffectsMaskMap,       decalData.affectsSmoothness || decalData.affectsMetal || decalData.affectsAO);
            context.AddField(HDFields.DecalDefault,         decalData.affectsAlbedo || decalData.affectsNormal || decalData.affectsMetal ||
                                                                    decalData.affectsAO || decalData.affectsSmoothness );
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);
            
            // Decal
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS);
            context.AddBlock(HDBlockFields.SurfaceDescription.NormalAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.MAOSAlpha);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new DecalPropertyBlock(decalData));
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            Vector1ShaderProperty drawOrder = new Vector1ShaderProperty();
            drawOrder.overrideReferenceName = "_DrawOrder";
            drawOrder.displayName = "Draw Order";
            drawOrder.floatType = FloatType.Integer;
            drawOrder.hidden = true;
            drawOrder.value = 0;
            collector.AddShaderProperty(drawOrder);

            Vector1ShaderProperty decalMeshDepthBias = new Vector1ShaderProperty();
            decalMeshDepthBias.overrideReferenceName = "_DecalMeshDepthBias";
            decalMeshDepthBias.displayName = "DecalMesh DepthBias";
            decalMeshDepthBias.hidden = true;
            decalMeshDepthBias.floatType = FloatType.Default;
            decalMeshDepthBias.value = 0;
            collector.AddShaderProperty(decalMeshDepthBias);
        }

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor Decal = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { DecalPasses.Projector3RT, new FieldCondition(HDFields.DecalDefault, true) },
                    { DecalPasses.Projector4RT, new FieldCondition(HDFields.DecalDefault, true) },
                    { DecalPasses.ProjectorEmissive, new FieldCondition(HDFields.AffectsEmission, true) },
                    { DecalPasses.Mesh3RT, new FieldCondition(HDFields.DecalDefault, true) },
                    { DecalPasses.Mesh4RT, new FieldCondition(HDFields.DecalDefault, true) },
                    { DecalPasses.MeshEmissive, new FieldCondition(HDFields.AffectsEmission, true) },
                    { DecalPasses.Preview, new FieldCondition(Fields.IsPreview, true) },
                },
            };
        }
#endregion

#region Passes
        public static class DecalPasses
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

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = DecalRenderStates.Projector3RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._3RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Projector4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                referenceName = "SHADERPASS_DBUFFER_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferProjector4RT],
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.Projector4RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._4RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor ProjectorEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_PROJECTOR",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_ProjectorEmissive],
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.ProjectorEmissive,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Mesh3RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT],
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.Mesh3RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._3RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Mesh4RT = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                referenceName = "SHADERPASS_DBUFFER_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT],
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentDefault,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.Mesh4RT,
                pragmas = DecalPragmas.Instanced,
                defines = DecalDefines._4RT,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor MeshEmissive = new PassDescriptor()
            {
                // Definition
                displayName = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                referenceName = "SHADERPASS_FORWARD_EMISSIVE_MESH",
                lightMode = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive],
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Conditional State
                renderStates = DecalRenderStates.MeshEmissive,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };

            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD_PREVIEW",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = passTemplateMaterialDirectory,

                // Port mask
                validPixelBlocks = DecalBlockMasks.FragmentMeshEmissive,

                //Fields
                structs = CoreStructCollections.Default,
                requiredFields = DecalRequiredFields.Mesh,
                fieldDependencies = CoreFieldDependencies.Default,

                // Render state overrides
                renderStates = DecalRenderStates.Preview,
                pragmas = DecalPragmas.Instanced,
                includes = DecalIncludes.Default,
            };
        }
#endregion

#region BlockMasks
        static class DecalBlockMasks
        {
            public static BlockFieldDescriptor[] FragmentDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.MAOSAlpha,
            };

            public static BlockFieldDescriptor[] FragmentEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Emission,
            };

            public static BlockFieldDescriptor[] FragmentMeshEmissive = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.NormalTS,
                HDBlockFields.SurfaceDescription.NormalAlpha,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.MAOSAlpha,
                BlockFields.SurfaceDescription.Emission,
            };
        }
#endregion

#region RequiredFields
        static class DecalRequiredFields
        {
            public static FieldCollection Mesh = new FieldCollection()
            {
                HDStructFields.AttributesMesh.normalOS,
                HDStructFields.AttributesMesh.tangentOS,
                HDStructFields.AttributesMesh.uv0,
                HDStructFields.FragInputs.tangentToWorld,
                HDStructFields.FragInputs.positionRWS,
                HDStructFields.FragInputs.texCoord0,
            };
        }
#endregion

#region RenderStates
        static class DecalRenderStates
        {
            readonly static string[] s_DecalColorMasks = new string[8]
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

            public static RenderStateCollection Projector3RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ColorMask(s_DecalColorMasks[4]) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = ((int)StencilUsage.Decals).ToString(),
                    Ref = ((int)StencilUsage.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Projector4RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = ((int)StencilUsage.Decals).ToString(),
                    Ref = ((int)StencilUsage.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                }) },

                // ColorMask per Affects Channel
                { RenderState.ColorMask(s_DecalColorMasks[0]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[1]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[2]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[3]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[4]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
                { RenderState.ColorMask(s_DecalColorMasks[5]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
                { RenderState.ColorMask(s_DecalColorMasks[6]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
                { RenderState.ColorMask(s_DecalColorMasks[7]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
            };

            public static RenderStateCollection ProjectorEmissive = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.Cull(Cull.Front) },
                { RenderState.ZTest(ZTest.Greater) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection Mesh3RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ColorMask(s_DecalColorMasks[4]) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = ((int)StencilUsage.Decals).ToString(),
                    Ref = ((int)StencilUsage.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };

            public static RenderStateCollection Mesh4RT = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha Blend 3 Zero OneMinusSrcColor") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = ((int)StencilUsage.Decals).ToString(),
                    Ref = ((int)StencilUsage.Decals).ToString(),
                    Comp = "Always",
                    Pass = "Replace",
                }) },

                // ColorMask per Affects Channel
                { RenderState.ColorMask(s_DecalColorMasks[0]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[1]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[2]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[3]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, false) } },
                { RenderState.ColorMask(s_DecalColorMasks[4]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
                { RenderState.ColorMask(s_DecalColorMasks[5]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, false),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
                { RenderState.ColorMask(s_DecalColorMasks[6]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, false),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
                { RenderState.ColorMask(s_DecalColorMasks[7]), new FieldCondition[] {
                    new FieldCondition(HDFields.AffectsMetal, true),
                    new FieldCondition(HDFields.AffectsAO, true),
                    new FieldCondition(HDFields.AffectsSmoothness, true) } },
            };

            public static RenderStateCollection MeshEmissive = new RenderStateCollection
            {
                { RenderState.Blend("Blend 0 SrcAlpha One") },
                { RenderState.ZTest(ZTest.LEqual) },
                { RenderState.ZWrite(ZWrite.Off) },
            };

            public static RenderStateCollection Preview = new RenderStateCollection
            {
                { RenderState.ZTest(ZTest.LEqual) },
            };
        }
#endregion

#region Pragmas
        static class DecalPragmas
        {
            public static PragmaCollection Instanced = new PragmaCollection
            {
                { CorePragmas.Basic },
                { Pragma.MultiCompileInstancing },
            };
        }
#endregion

#region Defines
        static class DecalDefines
        {
            static class Descriptors
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

            public static DefineCollection _3RT = new DefineCollection
            {
                { Descriptors.Decals3RT, 1 },
            };

            public static DefineCollection _4RT = new DefineCollection
            {
                { Descriptors.Decals4RT, 1 },
            };
        }
#endregion
        // protected override IncludeCollection subShaderIncludes => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl";

#region Includes
        static class DecalIncludes
        {
            const string kPacking = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl";
            const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
            const string kFunctions = "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl";
            const string kDecal = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl";
            const string kPassDecal = "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl";

            public static IncludeCollection Default = new IncludeCollection
            {
                { CoreIncludes.CorePregraph },
                { kPacking, IncludeLocation.Pregraph },
                { kColor, IncludeLocation.Pregraph },
                { kFunctions, IncludeLocation.Pregraph },
                { kDecal, IncludeLocation.Pregraph },
                { kPassDecal, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
