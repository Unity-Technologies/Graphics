using UnityEditor.ShaderGraph;
using UnityEngine;
using static UnityEditor.Rendering.BuiltIn.ShaderUtils;
using UnityEditor.Rendering.BuiltIn;
using System;

namespace UnityEditor.Rendering.Fullscreen.ShaderGraph
{
    class FullscreenSubTarget : SubTarget<FullscreenTarget>, IHasMetadata
    {
        static readonly GUID kSourceCodeGuid = new GUID("1cfc804c75474e144be5d4158b9522ed");  // FullscreenSubTarget.cs // TODO

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            // TODO: custom editor field
            if (context.customEditorForRenderPipelines.Count == 0)
                context.SetDefaultShaderGUI(typeof(FullscreenShaderGUI).FullName);

            // Process SubShaders
            context.AddSubShader(SubShaders.FullscreenBlit(target));
        }

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject()
        {
            var bultInMetadata = ScriptableObject.CreateInstance<FullscreenMetaData>();
            bultInMetadata.fullscreenCompatibility = target.fullscreenCompatibility;
            return bultInMetadata;
        }

        // We don't need the save context / update materials for now
        public override object saveContext => null;

        public FullscreenSubTarget()
        {
            displayName = "Fullscreen";
        }

        public override bool IsActive() => true;

        public override void ProcessPreviewMaterial(Material material)
        {
            // if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                // TODO:
                // material.SetFloat(Property.Blend(), (float)target.alphaMode);
                // material.SetFloat(Property.ZWriteControl(), target.zWrite ? 1 : 0); // TODO
                // material.SetFloat(Property.ZTest(), (float)target.depthTestMode);
            }

            // We always need these properties regardless of whether the material is allowed to override
            // Queue control & offset enable correct automatic render queue behavior
            // Control == 0 is automatic, 1 is user-specified render queue
            // material.SetFloat(Property.QueueOffset(), 0.0f);
            // material.SetFloat(Property.QueueControl(), (float)BuiltInBaseShaderGUI.QueueControl.Auto);

            // call the full unlit material setup function
            // BuiltInUnlitGUI.UpdateMaterial(material);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (target.allowMaterialOverride)
            {
                base.CollectShaderProperties(collector, generationMode);

                target.CollectRenderStateShaderProperties(collector, generationMode);
            }
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // TODO: sub target specific options?
        }

        #region SubShader
        static class SubShaders
        {
            const string kFullscreenDrawProceduralInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenDrawProcedural.hlsl";
            const string kFullscreenBlitInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenBlit.hlsl";
            const string kCustomRenderTextureInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/CustomRenderTexture.hlsl";
            const string kFullscreenCommon = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenCommon.hlsl";

            static readonly KeywordDescriptor depthWriteKeywork = new KeywordDescriptor
            {
                displayName = "Depth Write",
                referenceName = "DEPTH_WRITE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.ShaderFeature,
                stages = KeywordShaderStage.Fragment,
            };

            public static SubShaderDescriptor FullscreenBlit(FullscreenTarget target)
            {
                var result = new SubShaderDescriptor()
                {
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                var fullscreenPass = new PassDescriptor
                {
                    // Definition
                    displayName = "Fullscreen",
                    referenceName = "SHADERPASS_FULLSCREEN",
                    useInPreview = true,

                    // Template
                    passTemplatePath = FullscreenTarget.kTemplatePath,
                    sharedTemplateDirectories = FullscreenTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = new BlockFieldDescriptor[]
                    {
                        BlockFields.VertexDescription.Position
                    },
                    validPixelBlocks = new BlockFieldDescriptor[]
                    {
                        FullscreenTarget.Blocks.color,
                        FullscreenTarget.Blocks.depth,
                    },

                    // Fields
                    structs = new StructCollection
                    {
                        { Structs.Attributes },
                        { Structs.SurfaceDescriptionInputs },
                        { FullscreenTarget.Varyings },
                        { Structs.VertexDescriptionInputs },
                    },
                    fieldDependencies = FieldDependencies.Default,
                    requiredFields = new FieldCollection
                    {
                        StructFields.Attributes.uv0, // Always need uv0 to calculate the other properties in fullscreen node code
                        StructFields.Varyings.texCoord0,
                        StructFields.Attributes.vertexID, // Need the vertex Id for the DrawProcedural case
                    },

                    // Conditional State
                    renderStates = target.GetRenderState(),
                    pragmas = new PragmaCollection
                    {
                        { Pragma.Target(ShaderModel.Target30) },
                        { Pragma.Vertex("vert") },
                        { Pragma.Fragment("frag") },
                    },
                    defines = new DefineCollection
                    {
                        {depthWriteKeywork, 1, new FieldCondition(FullscreenTarget.Fields.depth, true)}
                    },
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection
                    {
                        // Pre-graph
                        { CoreIncludes.preGraphIncludes },

                        // Post-graph
                        { kFullscreenCommon, IncludeLocation.Postgraph },
                    },
                };

                switch (target.fullscreenCompatibility)
                {
                    default:
                    case FullscreenTarget.FullscreenCompatibility.Blit:
                        fullscreenPass.includes.Add(kFullscreenBlitInclude, IncludeLocation.Postgraph);
                        break;
                    case FullscreenTarget.FullscreenCompatibility.DrawProcedural:
                        fullscreenPass.includes.Add(kFullscreenDrawProceduralInclude, IncludeLocation.Postgraph);
                        break;
                    case FullscreenTarget.FullscreenCompatibility.CustomRenderTexture:
                        fullscreenPass.includes.Add(kCustomRenderTextureInclude, IncludeLocation.Postgraph);
                        break;
                }

                result.passes.Add(fullscreenPass);

                return result;
            }
        }
        #endregion
    }
}
