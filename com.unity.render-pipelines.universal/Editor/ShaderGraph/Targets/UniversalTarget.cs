using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Experimental.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    sealed class UniversalTarget : Target
    {
        const string kAssetGuid = "8c72f47fdde33b14a9340e325ce56f4d";
        List<SubTarget> m_SubTargets;
        SubTarget m_ActiveSubTarget;

        public UniversalTarget()
        {
            displayName = "Universal";
            m_SubTargets = TargetUtils.GetSubTargetsOfType<UniversalTarget>();
        }

        public const string kPipelineTag = "UniversalPipeline";

        public override void Setup(ref TargetSetupContext context)
        {
            // Currently we infer the active SubTarget based on the MasterNode type
            void SetActiveSubTargetIndex(IMasterNode masterNode)
            {
                Type activeSubTargetType;
                if(!s_SubTargetMap.TryGetValue(masterNode.GetType(), out activeSubTargetType))
                    return;

                m_ActiveSubTarget = m_SubTargets.FirstOrDefault(x => x.GetType() == activeSubTargetType);
            }
            
            // Setup the Target
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath(kAssetGuid));

            // Setup the active SubTarget
            SetActiveSubTargetIndex(context.masterNode);
            m_ActiveSubTarget.Setup(ref context);
        }

        public override bool IsValid(IMasterNode masterNode)
        {
            // Currently we infer the validity based on SubTarget mapping
            return s_SubTargetMap.TryGetValue(masterNode.GetType(), out _);
        }

        public override bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline is UniversalRenderPipelineAsset;
        }

        // Currently we need to map SubTarget type to IMasterNode type
        // We do this here to avoid bleeding this into the SubTarget API
        static Dictionary<Type, Type> s_SubTargetMap = new Dictionary<Type, Type>()
        {
            { typeof(PBRMasterNode), typeof(UniversalLitSubTarget) },
            { typeof(UnlitMasterNode), typeof(UniversalUnlitSubTarget) },
            { typeof(SpriteLitMasterNode), typeof(UniversalSpriteLitSubTarget) },
            { typeof(SpriteUnlitMasterNode), typeof(UniversalSpriteUnlitSubTarget) },
        };
    }

#region Passes
    static class CorePasses
    {
        public static PassDescriptor DepthOnly = new PassDescriptor()
        {
            // Definition
            displayName = "DepthOnly",
            referenceName = "SHADERPASS_DEPTHONLY",
            lightMode = "DepthOnly",
            useInPreview = true,

            // Template
            passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
            sharedTemplateDirectory = GenerationUtils.GetDefaultSharedTemplateDirectory(),

            // Port Mask
            vertexPorts = CorePortMasks.Vertex,
            pixelPorts = CorePortMasks.FragmentAlphaOnly,

            // Fields
            structs = CoreStructCollections.Default,
            fieldDependencies = CoreFieldDependencies.Default,

            // Conditional State
            renderStates = CoreRenderStates.DepthOnly,
            pragmas = CorePragmas.Instanced,
            includes = CoreIncludes.DepthOnly,
        };

        public static PassDescriptor ShadowCaster = new PassDescriptor()
        {
            // Definition
            displayName = "ShadowCaster",
            referenceName = "SHADERPASS_SHADOWCASTER",
            lightMode = "ShadowCaster",

            // Template
            passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
            sharedTemplateDirectory = GenerationUtils.GetDefaultSharedTemplateDirectory(),

            // Port Mask
            vertexPorts = CorePortMasks.Vertex,
            pixelPorts = CorePortMasks.FragmentAlphaOnly,

            // Fields
            structs = CoreStructCollections.Default,
            requiredFields = CoreRequiredFields.ShadowCaster,
            fieldDependencies = CoreFieldDependencies.Default,

            // Conditional State
            renderStates = CoreRenderStates.ShadowCasterMeta,
            pragmas = CorePragmas.Instanced,
            includes = CoreIncludes.ShadowCaster,
        };
    }
#endregion

#region PortMasks
    class CorePortMasks
    {
        public static int[] Vertex = new int[]
        {
            PBRMasterNode.PositionSlotId,
            PBRMasterNode.VertNormalSlotId,
            PBRMasterNode.VertTangentSlotId,
        };

        public static int[] FragmentAlphaOnly = new int[]
        {
            PBRMasterNode.AlphaSlotId,
            PBRMasterNode.AlphaThresholdSlotId,
        };
    }
#endregion

#region StructCollections
    static class CoreStructCollections
    {
        public static StructCollection Default = new StructCollection
        {
            { Structs.Attributes },
            { UniversalStructs.Varyings },
            { Structs.SurfaceDescriptionInputs },
            { Structs.VertexDescriptionInputs },
        };
    }
#endregion

#region RequiredFields
    static class CoreRequiredFields
    {
        public static FieldCollection ShadowCaster = new FieldCollection()
        {
            StructFields.Attributes.normalOS,
        };
    }
#endregion

#region FieldDependencies
    static class CoreFieldDependencies
    {
        public static DependencyCollection Default = new DependencyCollection()
        {
            { FieldDependencies.Default },
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,    StructFields.Attributes.instanceID ),
            new FieldDependency(UniversalStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,     StructFields.Attributes.instanceID ),
        };
    }
#endregion

#region RenderStates
    static class CoreRenderStates
    {
        public static readonly RenderStateCollection Default = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.ZWrite(ZWrite.Off), new FieldCondition(Fields.SurfaceTransparent, true) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
        };

        public static readonly RenderStateCollection ShadowCasterMeta = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
        };

        public static readonly RenderStateCollection DepthOnly = new RenderStateCollection
        {
            { RenderState.ZTest(ZTest.LEqual) },
            { RenderState.ZWrite(ZWrite.On) },
            { RenderState.Cull(Cull.Back), new FieldCondition(Fields.DoubleSided, false) },
            { RenderState.Cull(Cull.Off), new FieldCondition(Fields.DoubleSided, true) },
            { RenderState.ColorMask("ColorMask 0") },
            { RenderState.Blend(Blend.One, Blend.Zero), new FieldCondition(Fields.SurfaceOpaque, true) },
            { RenderState.Blend(Blend.SrcAlpha, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendAlpha, true) },
            { RenderState.Blend(Blend.One, Blend.OneMinusSrcAlpha, Blend.One, Blend.OneMinusSrcAlpha), new FieldCondition(Fields.BlendPremultiply, true) },
            { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(Fields.BlendAdd, true) },
            { RenderState.Blend(Blend.DstColor, Blend.Zero), new FieldCondition(Fields.BlendMultiply, true) },
        };
    }
#endregion

#region Pragmas
    static class CorePragmas
    {
        public static readonly PragmaCollection Default = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[]{ Platform.GLES }) },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Instanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[]{ Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection Forward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.OnlyRenderers(new[]{ Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection _2DDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target20) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9 }) },
            { Pragma.PreferHlslCC(new[]{ Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSDefault = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9, Platform.GLES }) },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSInstanced = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9, Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.DOTSInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };

        public static readonly PragmaCollection DOTSForward = new PragmaCollection
        {
            { Pragma.Target(ShaderModel.Target45) },
            { Pragma.ExcludeRenderers(new[]{ Platform.D3D9, Platform.GLES }) },
            { Pragma.MultiCompileInstancing },
            { Pragma.MultiCompileFog },
            { Pragma.DOTSInstancing },
            { Pragma.Vertex("vert") },
            { Pragma.Fragment("frag") },
        };
    }
#endregion

#region Includes
    static class CoreIncludes
    {
        const string kColor = "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl";
        const string kCore = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
        const string kLighting = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
        const string kGraphFunctions = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl";
        const string kGraphVariables = "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl";
        const string kVaryings = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl";
        const string kShaderPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl";
        const string kDepthOnlyPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl";
        const string kShadowCasterPass = "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl";

        public static IncludeCollection CorePregraph = new IncludeCollection
        {
            { kColor, IncludeLocation.Pregraph },
            { kCore, IncludeLocation.Pregraph },
            { kLighting, IncludeLocation.Pregraph },
        };

        public static IncludeCollection ShaderGraphPregraph = new IncludeCollection
        {
            { kGraphFunctions, IncludeLocation.Pregraph },
            { kGraphVariables, IncludeLocation.Pregraph },
        };

        public static IncludeCollection CorePostgraph = new IncludeCollection
        {
            { kShaderPass, IncludeLocation.Postgraph },
            { kVaryings, IncludeLocation.Postgraph },
        };

        public static IncludeCollection DepthOnly = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kDepthOnlyPass, IncludeLocation.Postgraph },
        };

        public static IncludeCollection ShadowCaster = new IncludeCollection
        {
            // Pre-graph
            { CorePregraph },
            { ShaderGraphPregraph },

            // Post-graph
            { CorePostgraph },
            { kShadowCasterPass, IncludeLocation.Postgraph },
        };
    } 
#endregion
    
#region KeywordDescriptors
    static class CoreKeywordDescriptors
    {
        public static KeywordDescriptor Lightmap = new KeywordDescriptor()
        {
            displayName = "Lightmap",
            referenceName = "LIGHTMAP_ON",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor DirectionalLightmapCombined = new KeywordDescriptor()
        {
            displayName = "Directional Lightmap Combined",
            referenceName = "DIRLIGHTMAP_COMBINED",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor SampleGI = new KeywordDescriptor()
        {
            displayName = "Sample GI",
            referenceName = "_SAMPLE_GI",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor MainLightShadows = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows",
            referenceName = "_MAIN_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor MainLightShadowsCascade = new KeywordDescriptor()
        {
            displayName = "Main Light Shadows Cascade",
            referenceName = "_MAIN_LIGHT_SHADOWS_CASCADE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor AdditionalLights = new KeywordDescriptor()
        {
            displayName = "Additional Lights",
            referenceName = "_ADDITIONAL",
            type = KeywordType.Enum,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Vertex", referenceName = "LIGHTS_VERTEX" },
                new KeywordEntry() { displayName = "Fragment", referenceName = "LIGHTS" },
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
            }
        };

        public static KeywordDescriptor AdditionalLightShadows = new KeywordDescriptor()
        {
            displayName = "Additional Light Shadows",
            referenceName = "_ADDITIONAL_LIGHT_SHADOWS",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShadowsSoft = new KeywordDescriptor()
        {
            displayName = "Shadows Soft",
            referenceName = "_SHADOWS_SOFT",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor MixedLightingSubtractive = new KeywordDescriptor()
        {
            displayName = "Mixed Lighting Subtractive",
            referenceName = "_MIXED_LIGHTING_SUBTRACTIVE",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor SmoothnessChannel = new KeywordDescriptor()
        {
            displayName = "Smoothness Channel",
            referenceName = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ETCExternalAlpha = new KeywordDescriptor()
        {
            displayName = "ETC External Alpha",
            referenceName = "ETC1_EXTERNAL_ALPHA",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShapeLightType0 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 0",
            referenceName = "USE_SHAPE_LIGHT_TYPE_0",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShapeLightType1 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 1",
            referenceName = "USE_SHAPE_LIGHT_TYPE_1",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShapeLightType2 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 2",
            referenceName = "USE_SHAPE_LIGHT_TYPE_2",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };

        public static KeywordDescriptor ShapeLightType3 = new KeywordDescriptor()
        {
            displayName = "Shape Light Type 3",
            referenceName = "USE_SHAPE_LIGHT_TYPE_3",
            type = KeywordType.Boolean,
            definition = KeywordDefinition.MultiCompile,
            scope = KeywordScope.Global,
        };
    }
#endregion
}
