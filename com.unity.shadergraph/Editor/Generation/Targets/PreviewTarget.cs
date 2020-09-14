using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    sealed class PreviewTarget : Target
    {
        static readonly GUID kSourceCodeGuid = new GUID("7464b9fcde08e5645a16b9b8ae1e573c"); // PreviewTarget.cs

        public PreviewTarget()
        {
            displayName = "Preview";
            isHidden = true;
        }

        public override bool IsActive() => false;

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(SubShaders.Preview);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline) => true;

        static class SubShaders
        {
            public static SubShaderDescriptor Preview = new SubShaderDescriptor()
            {
                renderQueue = "Geometry",
                renderType = "Opaque",
                generatesPreview = true,
                passes = new PassCollection { Passes.Preview },
            };
        }
        
        static class Passes
        {
            public static PassDescriptor Preview = new PassDescriptor()
            {
                // Definition
                referenceName = "SHADERPASS_PREVIEW",
                useInPreview = true,

                // Templates
                passTemplatePath = GenerationUtils.GetDefaultTemplatePath("PassMesh.template"),
                sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                // Collections
                structs = new StructCollection
                {
                    { Structs.Attributes },
                    { StructDescriptors.PreviewVaryings },
                    { Structs.SurfaceDescriptionInputs },
                    { Structs.VertexDescriptionInputs },
                },
                fieldDependencies = FieldDependencies.Default,
                pragmas = new PragmaCollection
                {
                    { Pragma.Vertex("vert") },
                    { Pragma.Fragment("frag") },
                },
                defines = new DefineCollection
                {
                    { KeywordDescriptors.PreviewKeyword, 1 },
                },
                includes = new IncludeCollection
                {
                    // Pre-graph
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl", IncludeLocation.Pregraph },       // TODO: put this on a conditional
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariables.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl", IncludeLocation.Pregraph },
                    { "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl", IncludeLocation.Pregraph },

                    // Post-graph
                    { "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewVaryings.hlsl", IncludeLocation.Postgraph },
                    { "Packages/com.unity.shadergraph/ShaderGraphLibrary/PreviewPass.hlsl", IncludeLocation.Postgraph },
                }
            };
        }

        static class StructDescriptors
        {
            public static StructDescriptor PreviewVaryings = new StructDescriptor()
            {
                name = "Varyings",
                packFields = true,
                fields = new[]
                {
                    StructFields.Varyings.positionCS,
                    StructFields.Varyings.positionWS,
                    StructFields.Varyings.normalWS,
                    StructFields.Varyings.tangentWS,
                    StructFields.Varyings.texCoord0,
                    StructFields.Varyings.texCoord1,
                    StructFields.Varyings.texCoord2,
                    StructFields.Varyings.texCoord3,
                    StructFields.Varyings.color,
                    StructFields.Varyings.viewDirectionWS,
                    StructFields.Varyings.screenPosition,
                    StructFields.Varyings.instanceID,
                    StructFields.Varyings.cullFace,
                }
            };
        }

        static class KeywordDescriptors
        {
            public static KeywordDescriptor PreviewKeyword = new KeywordDescriptor()
            {
                displayName = "Preview",
                referenceName = "SHADERGRAPH_PREVIEW",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Global,
            };
        }
    }
}
