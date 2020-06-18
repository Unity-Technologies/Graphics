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
    abstract class SurfaceSubTarget : HDSubTarget, IRequiresData<BuiltinData>
    {
        BuiltinData m_BuiltinData;

        // Interface Properties
        BuiltinData IRequiresData<BuiltinData>.data
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }

        public BuiltinData builtinData
        {
            get => m_BuiltinData;
            set => m_BuiltinData = value;
        }

        protected override string renderQueue
        {
            get => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(systemData.renderingPass, systemData.sortPriority, systemData.alphaTest));
        }

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPass.template";

        protected virtual bool supportForward => false;
        protected virtual bool supportLighting => false;
        protected virtual bool supportDistortion => false;
        protected virtual bool supportPathtracing => false;
        protected virtual bool supportRaytracing => true;

        protected abstract string subShaderInclude { get; }
        protected virtual string postDecalsInclude => null;
        protected virtual string raytracingInclude => null;
        protected abstract FieldDescriptor subShaderField { get; }

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("f4df7e8f9b8c23648ae50cbca0221e47")); // SurfaceSubTarget.cs
            base.Setup(ref context);
        }

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return PostProcessSubShader(GetSubShaderDescriptor());
            if (supportRaytracing || supportPathtracing)
                yield return PostProcessSubShader(GetRaytracingSubShaderDescriptor());
        }

        protected virtual SubShaderDescriptor GetSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = true,
                passes = GetPasses()
            };

            PassCollection GetPasses()
            {
                var passes = new PassCollection
                {
                    // Common "surface" passes
                    HDShaderPasses.GenerateShadowCaster(supportLighting),
                    HDShaderPasses.GenerateMETA(supportLighting),
                    HDShaderPasses.GenerateSceneSelection(supportLighting),
                    HDShaderPasses.GenerateMotionVectors(supportLighting, supportForward),
                    { HDShaderPasses.GenerateBackThenFront(supportLighting), new FieldCondition(HDFields.TransparentBackFace, true)},
                    { HDShaderPasses.GenerateTransparentDepthPostpass(supportLighting) },
                };

                passes.Add(HDShaderPasses.GenerateTransparentDepthPrepass(supportLighting));

                if (supportForward)
                {
                    passes.Add(HDShaderPasses.GenerateDepthForwardOnlyPass(supportLighting));
                    passes.Add(HDShaderPasses.GenereateForwardOnlyPass(supportLighting));
                }
                if (supportDistortion)
                    passes.Add(HDShaderPasses.GenerateDistortionPass(supportLighting), new FieldCondition(HDFields.TransparentDistortion, true));

                return passes;
            }
        }

        protected virtual SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            return new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetPasses(),
            };

            PassCollection GetPasses()
            {
                var passes = new PassCollection();

                if (supportRaytracing)
                {
                    // Common "surface" raytracing passes
                    passes.Add(HDShaderPasses.GenerateRaytracingIndirect(supportLighting));
                    passes.Add(HDShaderPasses.GenerateRaytracingVisibility(supportLighting));
                    passes.Add(HDShaderPasses.GenerateRaytracingForward(supportLighting));
                    passes.Add(HDShaderPasses.GenerateRaytracingGBuffer(supportLighting));
                };

                if (supportPathtracing)
                    passes.Add(HDShaderPasses.GeneratePathTracing(supportLighting));
                
                return passes;
            }
        }

        SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
            if (String.IsNullOrEmpty(subShaderDescriptor.pipelineTag))
                subShaderDescriptor.pipelineTag = HDRenderPipeline.k_ShaderTagName;
            
            var passes = subShaderDescriptor.passes.ToArray();
            PassCollection finalPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;
                passDescriptor.passTemplatePath = templatePath;
                passDescriptor.sharedTemplateDirectories = templateMaterialDirectories;

                // Add the subShader to enable fields that depends on it
                var originalRequireFields = passDescriptor.requiredFields;
                // Duplicate require fields to avoid unwanted shared list modification
                passDescriptor.requiredFields = new FieldCollection();
                if (originalRequireFields != null)
                    foreach (var field in originalRequireFields)
                        passDescriptor.requiredFields.Add(field.field);
                passDescriptor.requiredFields.Add(subShaderField);

                IncludeCollection finalIncludes = new IncludeCollection();
                var includeList = passDescriptor.includes.Select(include => include.descriptor).ToList();

                // Replace include placeholders if necessary:
                foreach (var include in passDescriptor.includes)
                {
                    if (include.descriptor.value == CoreIncludes.kPassPlaceholder)
                        include.descriptor.value = subShaderInclude;
                    if (include.descriptor.value == CoreIncludes.kPostDecalsPlaceholder)
                        include.descriptor.value = postDecalsInclude;
                    if (include.descriptor.value == CoreIncludes.kRaytracingPlaceholder)
                        include.descriptor.value = raytracingInclude;

                    if (!String.IsNullOrEmpty(include.descriptor.value))
                        finalIncludes.Add(include.descriptor.value, include.descriptor.location, include.fieldConditions);
                }
                passDescriptor.includes = finalIncludes;

                // Replace valid pixel blocks by automatic thing so we don't have to write them
                var tmpCtx = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), passDescriptor);
                GetActiveBlocks(ref tmpCtx);
                if (passDescriptor.validPixelBlocks == null)
                    passDescriptor.validPixelBlocks = tmpCtx.activeBlocks.Where(b => b.shaderStage == ShaderStage.Fragment).ToArray();
                if (passDescriptor.validVertexBlocks == null)
                    passDescriptor.validVertexBlocks = CoreBlockMasks.Vertex;

                // Set default values for HDRP "surface" passes:
                if (passDescriptor.structs == null)
                    passDescriptor.structs = CoreStructCollections.Default;
                if (passDescriptor.fieldDependencies == null)
                    passDescriptor.fieldDependencies = CoreFieldDependencies.Default;

                finalPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = finalPasses;

            return subShaderDescriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            
            if (supportDistortion)
                AddDistortionFields(ref context);

            // Mark the shader as unlit so we can remove lighting in FieldConditions
            if (!supportLighting)
                context.AddField(HDFields.Unlit);

            // Common properties between all "surface" master nodes (everything except decal right now)
            context.AddField(HDStructFields.FragInputs.IsFrontFace,         systemData.doubleSidedMode != DoubleSidedMode.Disabled && context.pass.referenceName != "SHADERPASS_MOTION_VECTORS");

            // Blend Mode
            context.AddField(Fields.BlendAdd,                       systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Additive);
            context.AddField(Fields.BlendAlpha,                     systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Alpha);
            context.AddField(Fields.BlendPremultiply,               systemData.surfaceType != SurfaceType.Opaque && systemData.blendMode == BlendMode.Premultiply);

            // Double Sided
            context.AddField(HDFields.DoubleSided, systemData.doubleSidedMode != DoubleSidedMode.Disabled);

            // We always generate the keyword ALPHATEST_ON. All the variant of AlphaClip (shadow, pre/postpass) are only available if alpha test is on.
            context.AddField(Fields.AlphaTest, systemData.alphaTest
                                                && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold)
                                                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow)
                                                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass)
                                                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass)));

            // All the DoAlphaXXX field drive the generation of which code to use for alpha test in the template
            // Regular alpha test is only done if artist haven't ask to use the specific alpha test shadow one
            bool isShadowPass               = context.pass.lightMode == "ShadowCaster";
            bool isTransparentDepthPrepass  = context.pass.lightMode == "TransparentDepthPrepass";
            bool isTransparentDepthPostpass = context.pass.lightMode == "TransparentDepthPostpass";
            context.AddField(HDFields.DoAlphaTest, systemData.alphaTest && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) &&
                                                                                !(isShadowPass && builtinData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow))
                                                                                ));
                
            // Shadow use the specific alpha test only if user have ask to override it
            context.AddField(HDFields.DoAlphaTestShadow,    systemData.alphaTest && builtinData.alphaTestShadow && isShadowPass &&
                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));
            // Pre/post pass always use the specific alpha test provided for those pass
            context.AddField(HDFields.DoAlphaTestPrepass,   systemData.alphaTest && systemData.alphaTestDepthPrepass && isTransparentDepthPrepass &&
                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));           
            context.AddField(HDFields.DoAlphaTestPostpass,  systemData.alphaTest && systemData.alphaTestDepthPostpass && isTransparentDepthPostpass &&
                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass));

            // Features & Misc
            context.AddField(Fields.LodCrossFade,                   systemData.supportLodCrossFade);
            context.AddField(Fields.AlphaToMask,                    systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) && builtinData.alphaToMask);
            context.AddField(HDFields.AlphaFog,                     systemData.surfaceType != SurfaceType.Opaque && builtinData.transparencyFog);
            context.AddField(HDFields.TransparentBackFace,          systemData.surfaceType != SurfaceType.Opaque && builtinData.backThenFrontRendering);

            // Depth offset needs positionRWS and is now a multi_compile
            context.AddField(HDStructFields.FragInputs.positionRWS);
        }

        protected void AddDistortionFields(ref TargetFieldContext context)
        {
            // Distortion
            context.AddField(HDFields.DistortionDepthTest,                  builtinData.distortionDepthTest);
            context.AddField(HDFields.DistortionAdd,                        builtinData.distortionMode == DistortionMode.Add);
            context.AddField(HDFields.DistortionMultiply,                   builtinData.distortionMode == DistortionMode.Multiply);
            context.AddField(HDFields.DistortionReplace,                    builtinData.distortionMode == DistortionMode.Replace);
            context.AddField(HDFields.TransparentDistortion,                systemData.surfaceType != SurfaceType.Opaque && builtinData.distortion);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            if (supportDistortion)
                AddDistortionBlocks(ref context);

            // Common block between all "surface" master nodes
            // Vertex
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(BlockFields.VertexDescription.Normal);
            context.AddBlock(BlockFields.VertexDescription.Tangent);

            // Surface
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Emission);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, systemData.alphaTest);

            // Alpha Test
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPrepass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
                systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest && systemData.alphaTestDepthPostpass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow,
                systemData.alphaTest && builtinData.alphaTestShadow);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset,          builtinData.depthOffset);
        }

        protected void AddDistortionBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(HDBlockFields.SurfaceDescription.Distortion,       systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
            context.AddBlock(HDBlockFields.SurfaceDescription.DistortionBlur,   systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            var gui = new SubTargetPropertiesGUI(context, onChange, registerUndo, systemData, builtinData, null);
            AddInspectorPropertyBlocks(gui);
            context.Add(gui);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });
            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                value = (int)systemData.renderingPass,
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.addPrecomputedVelocity,
                hidden = true,
                overrideReferenceName = kAddPrecomputedVelocity,
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.depthOffset,
                hidden = true,
                overrideReferenceName = kDepthOffsetEnable
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.transparentWritesMotionVec,
                hidden = true,
                overrideReferenceName = kTransparentWritingMotionVec
            });

            // Common properties for all "surface" master nodes
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, systemData.alphaTest, builtinData.alphaTestShadow);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, systemData.doubleSidedMode);
            HDSubShaderUtilities.AddPrePostPassProperties(collector, systemData.alphaTestDepthPrepass, systemData.alphaTestDepthPostpass);

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                systemData.surfaceType,
                systemData.blendMode,
                systemData.sortPriority,
                builtinData.alphaToMask,
                systemData.transparentZWrite,
                systemData.transparentCullMode,
                systemData.opaqueCullMode,
                systemData.zTest,
                builtinData.backThenFrontRendering,
                builtinData.transparencyFog
            );
        }

        public override void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)systemData.surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)systemData.doubleSidedMode);
            material.SetFloat(kDoubleSidedEnable, systemData.doubleSidedMode != DoubleSidedMode.Disabled ? 1 : 0);
            material.SetFloat(kAlphaCutoffEnabled, systemData.alphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)systemData.blendMode);
            material.SetFloat(kEnableFogOnTransparent, builtinData.transparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)systemData.zTest);
            material.SetFloat(kTransparentCullMode, (int)systemData.transparentCullMode);
            material.SetFloat(kOpaqueCullMode, (int)systemData.opaqueCullMode);
            material.SetFloat(kTransparentZWrite, systemData.transparentZWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(systemData.renderingPass, offset: 0, alphaTest: systemData.alphaTest);

            LightingShaderGraphGUI.SetupMaterialKeywordsAndPass(material);
        }
    }
}
