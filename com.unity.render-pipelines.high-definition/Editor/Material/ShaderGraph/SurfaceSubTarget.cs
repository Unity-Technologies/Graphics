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
            get => HDRenderQueue.GetShaderTagValue(HDRenderQueue.ChangeType(systemData.renderQueueType, systemData.sortPriority, systemData.alphaTest, false));
        }

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPass.template";

        protected virtual bool supportForward => false;
        protected virtual bool supportLighting => false;
        protected virtual bool supportDistortion => false;
        protected override bool supportRaytracing => true;

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            // Alpha test is currently the only property in buitin data to trigger the material upgrade script.
            int hash = systemData.alphaTest.GetHashCode();
            return hash;
        }

        static readonly GUID kSourceCodeGuid = new GUID("f4df7e8f9b8c23648ae50cbca0221e47"); // SurfaceSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
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
                    HDShaderPasses.GenerateScenePicking(),
                    HDShaderPasses.GenerateSceneSelection(supportLighting),
                    HDShaderPasses.GenerateMotionVectors(supportLighting, supportForward),
                    { HDShaderPasses.GenerateBackThenFront(supportLighting), new FieldCondition(HDFields.TransparentBackFace, true)},
                    { HDShaderPasses.GenerateTransparentDepthPostpass(supportLighting), new FieldCondition(HDFields.TransparentDepthPostPass, true)}
                };

                if (supportLighting)
                {
                    // We always generate the TransparentDepthPrepass as it can be use with SSR transparent
                    passes.Add(HDShaderPasses.GenerateTransparentDepthPrepass(true));
                }                
                else
                {
                    // We only generate the pass if requested
                    passes.Add(HDShaderPasses.GenerateTransparentDepthPrepass(false), new FieldCondition(HDFields.TransparentDepthPrePass, true));
                }

                if (supportForward)
                {
                    passes.Add(HDShaderPasses.GenerateDepthForwardOnlyPass(supportLighting));
                    passes.Add(HDShaderPasses.GenerateForwardOnlyPass(supportLighting));
                }

                if (supportDistortion)
                    passes.Add(HDShaderPasses.GenerateDistortionPass(supportLighting), new FieldCondition(HDFields.TransparentDistortion, true));

                passes.Add(HDShaderPasses.GenerateFullScreenDebug());

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

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            pass.keywords.Add(CoreKeywordDescriptors.AlphaTest, new FieldCondition(Fields.AlphaTest, true));

            if (pass.IsDepthOrMV())
            {
                pass.keywords.Add(CoreKeywordDescriptors.AlphaToMask, new FieldCondition(Fields.AlphaToMask, true));
                pass.keywords.Add(CoreKeywordDescriptors.WriteMsaaDepth);
            }

            pass.keywords.Add(CoreKeywordDescriptors.SurfaceTypeTransparent);
            pass.keywords.Add(CoreKeywordDescriptors.BlendMode);
            pass.keywords.Add(CoreKeywordDescriptors.DoubleSided, new FieldCondition(HDFields.Unlit, false));
            pass.keywords.Add(CoreKeywordDescriptors.DepthOffset, new FieldCondition(HDFields.DepthOffset, true));
            pass.keywords.Add(CoreKeywordDescriptors.AddPrecomputedVelocity);
            pass.keywords.Add(CoreKeywordDescriptors.TransparentWritesMotionVector);
            pass.keywords.Add(CoreKeywordDescriptors.FogOnTransparent);

            if (pass.IsLightingOrMaterial())
                pass.keywords.Add(CoreKeywordDescriptors.DebugDisplay);
            
            if (!pass.IsDXR())
                pass.keywords.Add(CoreKeywordDescriptors.LodFadeCrossfade, new FieldCondition(Fields.LodCrossFade, true));

            if (pass.lightMode == HDShaderPassNames.s_MotionVectorsStr)
            {
                if (supportForward)
                    pass.defines.Add(CoreKeywordDescriptors.WriteNormalBuffer, 1, new FieldCondition(HDFields.Unlit, false));
                else
                    pass.keywords.Add(CoreKeywordDescriptors.WriteNormalBuffer, new FieldCondition(HDFields.Unlit, false));
            }
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
            context.AddField(HDStructFields.FragInputs.IsFrontFace, systemData.doubleSidedMode != DoubleSidedMode.Disabled && context.pass.referenceName != "SHADERPASS_MOTION_VECTORS");

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
            bool isShadowPass               = (context.pass.lightMode == "ShadowCaster") || (context.pass.lightMode == "VisibilityDXR");
            bool isTransparentDepthPrepass  = context.pass.lightMode == "TransparentDepthPrepass";

            // Shadow use the specific alpha test only if user have ask to override it
            context.AddField(HDFields.DoAlphaTestShadow,    systemData.alphaTest && builtinData.alphaTestShadow && isShadowPass &&
                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow));
            // Pre/post pass always use the specific alpha test provided for those pass
            context.AddField(HDFields.DoAlphaTestPrepass,   systemData.alphaTest && builtinData.transparentDepthPrepass && isTransparentDepthPrepass &&
                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass));           

            // Features & Misc
            context.AddField(Fields.LodCrossFade,           builtinData.supportLodCrossFade);
            context.AddField(Fields.AlphaToMask,            systemData.alphaTest);
            context.AddField(HDFields.TransparentBackFace,  builtinData.backThenFrontRendering);
            context.AddField(HDFields.TransparentDepthPrePass, builtinData.transparentDepthPrepass);
            context.AddField(HDFields.TransparentDepthPostPass, builtinData.transparentDepthPostpass);

            context.AddField(HDFields.DepthOffset, builtinData.depthOffset && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.DepthOffset));

            // Depth offset needs positionRWS and is now a multi_compile
            if (builtinData.depthOffset)
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
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass, systemData.alphaTest && builtinData.transparentDepthPrepass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass, systemData.alphaTest && builtinData.transparentDepthPostpass);
            context.AddBlock(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow, systemData.alphaTest && builtinData.alphaTestShadow);

            // Misc
            context.AddBlock(HDBlockFields.SurfaceDescription.DepthOffset, builtinData.depthOffset);
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
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });
            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                value = (int)systemData.renderQueueType,
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.addPrecomputedVelocity,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kAddPrecomputedVelocity,
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.depthOffset,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kDepthOffsetEnable
            });

            collector.AddShaderProperty(new BooleanShaderProperty
            {
                value = builtinData.transparentWritesMotionVec,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
                overrideReferenceName = kTransparentWritingMotionVec
            });

            // Common properties for all "surface" master nodes
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, systemData.alphaTest, builtinData.alphaTestShadow);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, systemData.doubleSidedMode);
            HDSubShaderUtilities.AddPrePostPassProperties(collector, builtinData.transparentDepthPrepass, builtinData.transparentDepthPostpass);

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
            material.renderQueue = (int)HDRenderQueue.ChangeType(systemData.renderQueueType, offset: 0, alphaTest: systemData.alphaTest, false);

            LightingShaderGraphGUI.SetupMaterialKeywordsAndPass(material);
        }

        internal override void MigrateTo(ShaderGraphVersion version)
        {
            base.MigrateTo(version);

            if (version == ShaderGraphVersion.FirstTimeMigration)
            {
#pragma warning disable 618
                // If we come from old master node, nothing to do.
                // Only perform an action if we are a shader stack
                if (!m_MigrateFromOldSG)
                {
                    builtinData.transparentDepthPrepass = systemData.m_TransparentDepthPrepass;
                    builtinData.transparentDepthPostpass = systemData.m_TransparentDepthPostpass;
                    builtinData.supportLodCrossFade = systemData.m_SupportLodCrossFade;
                }
#pragma warning restore 618
            }
        }
    }
}
