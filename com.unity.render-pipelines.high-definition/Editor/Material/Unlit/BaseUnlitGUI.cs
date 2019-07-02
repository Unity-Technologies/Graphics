using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph;

// Include material common properties names
using static UnityEngine.Experimental.Rendering.HDPipeline.HDMaterialProperties;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // Extension class to setup material keywords on unlit materials
    public static class BaseUnlitGUI
    {
        public static void SetupBaseUnlitKeywords(this Material material)
        {
            bool alphaTestEnable = material.HasProperty(kAlphaCutoffEnabled) && material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaTestEnable);

            SurfaceType surfaceType = material.GetSurfaceType();
            CoreUtils.SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", surfaceType == SurfaceType.Transparent);

            bool enableBlendModePreserveSpecularLighting = (surfaceType == SurfaceType.Transparent) && material.HasProperty(kEnableBlendModePreserveSpecularLighting) && material.GetFloat(kEnableBlendModePreserveSpecularLighting) > 0.0f;
            CoreUtils.SetKeyword(material, "_BLENDMODE_PRESERVE_SPECULAR_LIGHTING", enableBlendModePreserveSpecularLighting);

            bool transparentWritesMotionVec = (surfaceType == SurfaceType.Transparent) && material.HasProperty(kTransparentWritingMotionVec) && material.GetInt(kTransparentWritingMotionVec) > 0;
            CoreUtils.SetKeyword(material, "_TRANSPARENT_WRITES_MOTION_VEC", transparentWritesMotionVec);

            // These need to always been set either with opaque or transparent! So a users can switch to opaque and remove the keyword correctly
            CoreUtils.SetKeyword(material, "_BLENDMODE_ALPHA", false);
            CoreUtils.SetKeyword(material, "_BLENDMODE_ADD", false);
            CoreUtils.SetKeyword(material, "_BLENDMODE_PRE_MULTIPLY", false);

            HDRenderQueue.RenderQueueType renderQueueType = HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue);
            bool needOffScreenBlendFactor = renderQueueType == HDRenderQueue.RenderQueueType.AfterPostprocessTransparent || renderQueueType == HDRenderQueue.RenderQueueType.LowTransparent;

            // Alpha tested materials always have a prepass where we perform the clip.
            // Then during Gbuffer pass we don't perform the clip test, so we need to use depth equal in this case.
            if (alphaTestEnable)
            {
                material.SetInt(kZTestGBuffer, (int)UnityEngine.Rendering.CompareFunction.Equal);
            }
            else
            {
                material.SetInt(kZTestGBuffer, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }

            // If the material use the kZTestDepthEqualForOpaque it mean it require depth equal test for opaque but transparent are not affected
            if (material.HasProperty(kZTestDepthEqualForOpaque))
            {
                if (surfaceType == SurfaceType.Opaque)
                {
                    // When the material is after post process, we need to use LEssEqual because there is no depth prepass for unlit opaque
                    if (HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque.Contains(material.renderQueue))
                        material.SetInt(kZTestDepthEqualForOpaque, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                    else
                        material.SetInt(kZTestDepthEqualForOpaque, (int)UnityEngine.Rendering.CompareFunction.Equal);
                }
                else
                    material.SetInt(kZTestDepthEqualForOpaque, (int)material.GetTransparentZTest());
            }

            if (surfaceType == SurfaceType.Opaque)
            {
                material.SetOverrideTag("RenderType", alphaTestEnable ? "TransparentCutout" : "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                // Caution:  we need to setup One for src and Zero for Dst for all element as users could switch from transparent to Opaque and keep remaining value.
                // Unity will disable Blending based on these default value.
                // Note that for after postprocess we setup 0 in opacity inside the shaders, so we correctly end with 0 in opacity for the compositing pass
                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt(kZWrite, 1);
            }
            else
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt(kZWrite, material.GetZWrite() ? 1 : 0);

                if (material.HasProperty(kBlendMode))
                {
                    BlendMode blendMode = material.GetBlendMode();

                    CoreUtils.SetKeyword(material, "_BLENDMODE_ALPHA", BlendMode.Alpha == blendMode);
                    CoreUtils.SetKeyword(material, "_BLENDMODE_ADD", BlendMode.Additive == blendMode);
                    CoreUtils.SetKeyword(material, "_BLENDMODE_PRE_MULTIPLY", BlendMode.Premultiply == blendMode);

                    // When doing off-screen transparency accumulation, we change blend factors as described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
                    switch (blendMode)
                    {
                        // Alpha
                        // color: src * src_a + dst * (1 - src_a)
                        // src * src_a is done in the shader as it allow to reduce precision issue when using _BLENDMODE_PRESERVE_SPECULAR_LIGHTING (See Material.hlsl)
                        case BlendMode.Alpha:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            if (needOffScreenBlendFactor)
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            else
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            break;

                        // Additive
                        // color: src * src_a + dst
                        // src * src_a is done in the shader
                        case BlendMode.Additive:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            if (needOffScreenBlendFactor)
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            }
                            else
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            }
                            break;

                        // PremultipliedAlpha
                        // color: src * src_a + dst * (1 - src_a)
                        // src is supposed to have been multiplied by alpha in the texture on artists side.
                        case BlendMode.Premultiply:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            if (needOffScreenBlendFactor)
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            else
                            {
                                material.SetInt("_AlphaSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                material.SetInt("_AlphaDstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            }
                            break;
                    }
                }
            }

            bool fogEnabled = material.HasProperty(kEnableFogOnTransparent) && material.GetFloat(kEnableFogOnTransparent) > 0.0f && surfaceType == SurfaceType.Transparent;
            CoreUtils.SetKeyword(material, "_ENABLE_FOG_ON_TRANSPARENT", fogEnabled);

            if (material.HasProperty(kDistortionEnable) && material.HasProperty(kDistortionBlendMode))
            {
                bool distortionDepthTest = material.GetFloat(kDistortionDepthTest) > 0.0f;
                if (material.HasProperty(kZTestModeDistortion))
                {
                    if (distortionDepthTest)
                    {
                        material.SetInt(kZTestModeDistortion, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                    }
                    else
                    {
                        material.SetInt(kZTestModeDistortion, (int)UnityEngine.Rendering.CompareFunction.Always);
                    }
                }

                var distortionBlendMode = material.GetInt(kDistortionBlendMode);
                switch (distortionBlendMode)
                {
                    default:
                    case 0: // Add
                        material.SetInt("_DistortionSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionDstBlend", (int)UnityEngine.Rendering.BlendMode.One);

                        material.SetInt("_DistortionBlurSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionBlurDstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionBlurBlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                        break;

                    case 1: // Multiply
                        material.SetInt("_DistortionSrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DistortionDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

                        material.SetInt("_DistortionBlurSrcBlend", (int)UnityEngine.Rendering.BlendMode.DstAlpha);
                        material.SetInt("_DistortionBlurDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_DistortionBlurBlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                        break;

                    case 2: // Replace
                        material.SetInt("_DistortionSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

                        material.SetInt("_DistortionBlurSrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DistortionBlurDstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_DistortionBlurBlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                        break;
                }
            }

            CullMode doubleSidedOffMode = (surfaceType == SurfaceType.Transparent) ? material.GetTransparentCullMode() : CullMode.Back;

            bool isBackFaceEnable = material.HasProperty(kTransparentBackfaceEnable) && material.GetFloat(kTransparentBackfaceEnable) > 0.0f && surfaceType == SurfaceType.Transparent;
            bool doubleSidedEnable = material.HasProperty(kDoubleSidedEnable) && material.GetFloat(kDoubleSidedEnable) > 0.0f;

            // Disable culling if double sided
            material.SetInt("_CullMode", doubleSidedEnable ? (int)UnityEngine.Rendering.CullMode.Off : (int)doubleSidedOffMode);

            // We have a separate cullmode (_CullModeForward) for Forward in case we use backface then frontface rendering, need to configure it
            if (isBackFaceEnable)
            {
                material.SetInt("_CullModeForward", (int)UnityEngine.Rendering.CullMode.Back);
            }
            else
            {
                material.SetInt("_CullModeForward", (int)(doubleSidedEnable ? UnityEngine.Rendering.CullMode.Off : doubleSidedOffMode));
            }

            CoreUtils.SetKeyword(material, "_DOUBLESIDED_ON", doubleSidedEnable);

            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            if (material.HasProperty(kEmissionColor))
            {
                material.SetColor(kEmissionColor, Color.white); // kEmissionColor must always be white to allow our own material to control the GI (this allow to fallback from builtin unity to our system).
                                                                // as it happen with old material that it isn't the case, we force it.
                MaterialEditor.FixupEmissiveFlag(material);
            }

            // Commented out for now because unfortunately we used the hard coded property names used by the GI system for our own parameters
            // So we need a way to work around that before we activate this.
            material.SetupMainTexForAlphaTestGI("_EmissiveColorMap", "_EmissiveColor");

            // DoubleSidedGI has to be synced with our double sided toggle
            var serializedObject = new SerializedObject(material);
            var doubleSidedGIppt = serializedObject.FindProperty("m_DoubleSidedGI");
            doubleSidedGIppt.boolValue = doubleSidedEnable;
            serializedObject.ApplyModifiedProperties();
        }

        // This is a hack for GI. PVR looks in the shader for a texture named "_MainTex" to extract the opacity of the material for baking. In the same manner, "_Cutoff" and "_Color" are also necessary.
        // Since we don't have those parameters in our shaders we need to provide a "fake" useless version of them with the right values for the GI to work.
        public static void SetupMainTexForAlphaTestGI(this Material material, string colorMapPropertyName, string colorPropertyName)
        {
            if (material.HasProperty(colorMapPropertyName))
            {
                var mainTex = material.GetTexture(colorMapPropertyName);
                material.SetTexture("_MainTex", mainTex);
            }

            if (material.HasProperty(colorPropertyName))
            {
                var color = material.GetColor(colorPropertyName);
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_AlphaCutoff")) // Same for all our materials
            {
                var cutoff = material.GetFloat("_AlphaCutoff");
                material.SetFloat("_Cutoff", cutoff);
            }
        }

        static public void SetupBaseUnlitPass(this Material material)
        {
            if (material.HasProperty(kDistortionEnable))
            {
                bool distortionEnable = material.GetFloat(kDistortionEnable) > 0.0f && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);

                bool distortionOnly = false;
                if (material.HasProperty(kDistortionOnly))
                {
                    distortionOnly = material.GetFloat(kDistortionOnly) > 0.0f;
                }

                // If distortion only is enabled, disable all passes (except distortion and debug)
                bool enablePass = !(distortionEnable && distortionOnly);

                // Disable all passes except distortion
                // Distortion is setup in code above
                material.SetShaderPassEnabled(HDShaderPassNames.s_ForwardStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_DepthOnlyStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_DepthForwardOnlyStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_ForwardOnlyStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_GBufferStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_GBufferWithPrepassStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_DistortionVectorsStr, distortionEnable); // note: use distortionEnable
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentBackfaceStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPostpassStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_MetaStr, enablePass);
                material.SetShaderPassEnabled(HDShaderPassNames.s_ShadowCasterStr, enablePass);
            }

            if (material.HasProperty(kTransparentDepthPrepassEnable))
            {
                bool depthWriteEnable = (material.GetFloat(kTransparentDepthPrepassEnable) > 0.0f) && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPrepassStr, depthWriteEnable);
            }

            if (material.HasProperty(kTransparentDepthPostpassEnable))
            {
                bool depthWriteEnable = (material.GetFloat(kTransparentDepthPostpassEnable) > 0.0f) && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentDepthPostpassStr, depthWriteEnable);
            }

            if (material.HasProperty(kTransparentBackfaceEnable))
            {
                bool backFaceEnable = (material.GetFloat(kTransparentBackfaceEnable) > 0.0f) && ((SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent);
                material.SetShaderPassEnabled(HDShaderPassNames.s_TransparentBackfaceStr, backFaceEnable);
            }

            // Shader graphs materials have their own management of motion vector pass in the material inspector
            if (!material.shader.IsShaderGraph())
            {
                // We don't have any vertex animation for lit/unlit vector, so we
                // setup motion vector pass to false. Remind that in HDRP this
                // doesn't disable motion vector, it just mean that the material
                // don't do any vertex deformation but we can still have
                // skinning / morph target
                material.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, false);
            }
        }
    }
}
