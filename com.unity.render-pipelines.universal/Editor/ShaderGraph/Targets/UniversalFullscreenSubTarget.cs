using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    class UniversalFullscreenSubTarget : FullscreenSubTarget<UniversalTarget>, IRequiresData<FullscreenData>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("48080a5025a54a84087e882e2f988642");  // UniversalFullscreenSubTarget.cs // TODO

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        protected override string pipelineTag => UniversalTarget.kPipelineTag;

        protected override IncludeCollection pregraphIncludes => new IncludeCollection
        {
            { CoreIncludes.CorePregraph },
            { CoreIncludes.ShaderGraphPregraph },
        };

        public UniversalFullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }

        // public virtual bool IsNodeAllowedByTarget(Type nodeType)
        // {
        //     // TODO
        //     // SRPFilterAttribute srpFilter = NodeClassCache.GetAttributeOnNodeType<SRPFilterAttribute>(nodeType);
        //     // bool worksWithThisSrp = srpFilter == null || srpFilter.srpTypes.Contains(typeof(HDRenderPipeline));

        //     return true;
        // }

        // TODO: replace shader generation by this: and HDRRP specific includes + functions

        // #region SubShader
        // static class SubShaders
        // {
        //     const string kFullscreenDrawProceduralInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenDrawProcedural.hlsl";
        //     const string kFullscreenBlitInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenBlit.hlsl";
        //     const string kCustomRenderTextureInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/CustomRenderTexture.hlsl";
        //     const string kFullscreenCommon = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenCommon.hlsl";

        //     static readonly KeywordDescriptor depthWriteKeywork = new KeywordDescriptor
        //     {
        //         displayName = "Depth Write",
        //         referenceName = "DEPTH_WRITE",
        //         type = KeywordType.Boolean,
        //         definition = KeywordDefinition.ShaderFeature,
        //         stages = KeywordShaderStage.Fragment,
        //     };

        //     public static SubShaderDescriptor FullscreenBlit(FullscreenTarget target)
        //     {
        //         var result = new SubShaderDescriptor()
        //         {
        //             generatesPreview = true,
        //             passes = new PassCollection()
        //         };

        //         result.passes.Add(GetFullscreenPass(target, FullscreenTarget.FullscreenCompatibility.Blit));
        //         result.passes.Add(GetFullscreenPass(target, FullscreenTarget.FullscreenCompatibility.DrawProcedural));

        //         return result;
        //     }

        //     static PassDescriptor GetFullscreenPass(FullscreenTarget target, FullscreenTarget.FullscreenCompatibility compatibility)
        //     {
        //         var fullscreenPass = new PassDescriptor
        //         {
        //             // Definition
        //             displayName = compatibility.ToString(),
        //             referenceName = "SHADERPASS_" + compatibility.ToString().ToUpper(),
        //             useInPreview = true,

        //             // Template
        //             passTemplatePath = FullscreenTarget.kTemplatePath,
        //             sharedTemplateDirectories = FullscreenTarget.kSharedTemplateDirectories,

        //             // Port Mask
        //             validVertexBlocks = new BlockFieldDescriptor[]
        //             {
        //                 BlockFields.VertexDescription.Position
        //             },
        //             validPixelBlocks = new BlockFieldDescriptor[]
        //             {
        //                 FullscreenTarget.Blocks.color,
        //                 FullscreenTarget.Blocks.depth,
        //             },

        //             // Fields
        //             structs = new StructCollection
        //             {
        //                 { Structs.Attributes },
        //                 { Structs.SurfaceDescriptionInputs },
        //                 { FullscreenTarget.Varyings },
        //                 { Structs.VertexDescriptionInputs },
        //             },
        //             fieldDependencies = FieldDependencies.Default,
        //             requiredFields = new FieldCollection
        //             {
        //                 StructFields.Attributes.uv0, // Always need uv0 to calculate the other properties in fullscreen node code
        //                 StructFields.Varyings.texCoord0,
        //                 StructFields.Attributes.vertexID, // Need the vertex Id for the DrawProcedural case
        //             },

        //             // Conditional State
        //             renderStates = target.GetRenderState(),
        //             pragmas = new PragmaCollection
        //             {
        //                 { Pragma.Target(ShaderModel.Target30) },
        //                 { Pragma.Vertex("vert") },
        //                 { Pragma.Fragment("frag") },
        //             },
        //             defines = new DefineCollection
        //             {
        //                 {depthWriteKeywork, 1, new FieldCondition(FullscreenTarget.Fields.depth, true)}
        //             },
        //             keywords = new KeywordCollection(),
        //             includes = new IncludeCollection
        //             {
        //                 // Pre-graph
        //                 { CoreIncludes.preGraphIncludes },

        //                 // Post-graph
        //                 { kFullscreenCommon, IncludeLocation.Postgraph },
        //             },
        //         };

        //         switch (compatibility)
        //         {
        //             default:
        //             case FullscreenTarget.FullscreenCompatibility.Blit:
        //                 fullscreenPass.includes.Add(kFullscreenBlitInclude, IncludeLocation.Postgraph);
        //                 break;
        //             case FullscreenTarget.FullscreenCompatibility.DrawProcedural:
        //                 fullscreenPass.includes.Add(kFullscreenDrawProceduralInclude, IncludeLocation.Postgraph);
        //                 break;
        //             // case FullscreenTarget.FullscreenCompatibility.CustomRenderTexture:
        //             //     fullscreenPass.includes.Add(kCustomRenderTextureInclude, IncludeLocation.Postgraph);
        //                 break;
        //         }

        //         return fullscreenPass;
        //     }
        // }
        // #endregion
    }
}
