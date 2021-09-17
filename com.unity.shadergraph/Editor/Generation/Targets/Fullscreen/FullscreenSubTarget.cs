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
            // if (!context.HasCustomEditorForRenderPipeline(null))
            //     context.customEditorForRenderPipelines.Add((typeof(BuiltInUnlitGUI).FullName, ""));

            // Process SubShaders
            context.AddSubShader(SubShaders.FullscreenBlit(target));
        }

        protected FullscreenTarget.MaterialType materialType { get; }

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject()
        {
            var bultInMetadata = ScriptableObject.CreateInstance<FullscreenMetaData>();
            bultInMetadata.materialType = materialType;
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
            if (target.allowMaterialOverride)
            {
                // copy our target's default settings into the material
                // (technically not necessary since we are always recreating the material from the shader each time,
                // which will pull over the defaults from the shader definition)
                // but if that ever changes, this will ensure the defaults are set
                // TODO:
                // material.SetFloat(Property.Blend(), (float)target.alphaMode);
                // material.SetFloat(Property.ZWriteControl(), target.zWrite ? 1 : 0); // TODO
                material.SetFloat(Property.ZTest(), (float)target.depthTestMode);
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

                // setup properties using the defaults
                // TODO
                // collector.AddFloatProperty(Property.Blend(), (float)target.alphaMode);
                collector.AddFloatProperty(Property.SrcBlend(), 1.0f);    // always set by material inspector (TODO : get src/dst blend and set here?)
                collector.AddFloatProperty(Property.DstBlend(), 0.0f);    // always set by material inspector
                collector.AddFloatProperty(Property.ZWrite(), 0.0f); // TODO
                // collector.AddFloatProperty(Property.ZWriteControl(), (float)target.zWriteControl);
                collector.AddFloatProperty(Property.ZTest(), (float)target.depthTestMode);    // ztest mode is designed to directly pass as ztest
            }

            // We always need these properties regardless of whether the material is allowed to override other shader properties.
            // Queue control & offset enable correct automatic render queue behavior.  Control == 0 is automatic, 1 is user-specified.
            // We initialize queue control to -1 to indicate to UpdateMaterial that it needs to initialize it properly on the material.
            // collector.AddFloatProperty(Property.QueueOffset(), 0.0f);
            // collector.AddFloatProperty(Property.QueueControl(), -1.0f);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // TODO: sub target specific options?
        }

        #region SubShader
        static class SubShaders
        {
            const string kFullscreenInclude = "Packages/com.unity.shadergraph/Editor/Generation/Targets/Fullscreen/Includes/FullscreenBlit.hlsl";

            public static SubShaderDescriptor FullscreenBlit(FullscreenTarget target)
            {
                var result = new SubShaderDescriptor()
                {
                    generatesPreview = true,
                    passes = new PassCollection()
                };

                result.passes.Add(new PassDescriptor
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
                        FullscreenTarget.Blocks.Color,
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
                    defines = new DefineCollection(),
                    keywords = new KeywordCollection(),
                    includes = new IncludeCollection
                    {
                        // Pre-graph
                        { CoreIncludes.CorePregraph },
                        { CoreIncludes.ShaderGraphPregraph },

                        // Post-graph
                        // { CoreIncludes.CorePostgraph },
                        { kFullscreenInclude, IncludeLocation.Postgraph },
                    },
                });

                return result;
            }
        }
        #endregion
    }

    // internal static class SubShaderUtils
    // {
    //     // Overloads to do inline PassDescriptor modifications
    //     // NOTE: param order should match PassDescriptor field order for consistency
    //     #region PassVariant
    //     internal static PassDescriptor PassVariant(in PassDescriptor source, PragmaCollection pragmas)
    //     {
    //         var result = source;
    //         result.pragmas = pragmas;
    //         return result;
    //     }

    //     #endregion
    // }
}
