using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
#if UNITY_2018_2_OR_NEWER
    class HDRPVariantStripper : IPreprocessShaders
    {
        // returns true if the variant should be stripped.
        delegate bool VariantStrippingFunc(Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData);

        Dictionary<string, VariantStrippingFunc> m_StripperFuncs;

        HDRenderPipelineAsset m_CurrentHDRPAsset;

        ShaderKeyword m_ShadowMask;
        ShaderKeyword m_Transparent;
        ShaderKeyword m_DebugDisplay;
        ShaderKeyword m_TileLighting;
        ShaderKeyword m_ClusterLighting;

        //ShaderKeyword m_FeatureSSS;

        public HDRPVariantStripper()
        {
            // TODO: Grab correct configuration/quality asset.
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
                m_CurrentHDRPAsset = hdPipeline.asset;

            m_StripperFuncs = new Dictionary<string, VariantStrippingFunc>();
            m_StripperFuncs.Add("HDRenderPipeline/Lit", LitShaderStripper);
            m_StripperFuncs.Add("HDRenderPipeline/LitTessellation", LitShaderStripper);
            m_StripperFuncs.Add("HDRenderPipeline/LayeredLit", LitShaderStripper);
            m_StripperFuncs.Add("HDRenderPipeline/LayeredLitTessellation", LitShaderStripper);

            m_Transparent = new ShaderKeyword("_SURFACE_TYPE_TRANSPARENT");
            m_DebugDisplay = new ShaderKeyword("DEBUG_DISPLAY");
            m_TileLighting = new ShaderKeyword("USE_FPTL_LIGHTLIST");
            m_ClusterLighting = new ShaderKeyword("USE_CLUSTERED_LIGHTLIST");

            //m_FeatureSSS = new ShaderKeyword("_MATERIAL_FEATURE_SUBSURFACE_SCATTERING");
        }

        bool LitShaderStripper(Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            bool isGBufferPass = snippet.passName == "GBuffer";
            bool isForwardPass = snippet.passName == "Forward";
            bool isTransparentForwardPass = snippet.passName == "TransparentDepthPostpass" || snippet.passName == "TransparentBackface" || snippet.passName == "TransparentDepthPrepass";
            bool isMotionPass = snippet.passName == "Motion Vectors";

            // NOTE: All these keyword should be automatically stripped so there's no need to handle them ourselves.
            // LIGHTMAP_ON, DIRLIGHTMAP_COMBINED, DYNAMICLIGHTMAP_ON, LIGHTMAP_SHADOW_MIXING, SHADOWS_SHADOWMASK
            // FOG_LINEAR, FOG_EXP, FOG_EXP2
            // STEREO_INSTANCING_ON, STEREO_MULTIVIEW_ON, STEREO_CUBEMAP_RENDER_ON, UNITY_SINGLE_PASS_STEREO
            // INSTANCING_ON

            if (!m_CurrentHDRPAsset.renderPipelineSettings.supportMotionVectors && isMotionPass)
                return true;

            // When using forward only, we never need GBuffer pass (only Forward)
            if (m_CurrentHDRPAsset.renderPipelineSettings.supportForwardOnly && isGBufferPass)
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
            {
                // If transparent, we never need GBuffer pass.
                if (isGBufferPass)
                    return true;

                // We will also use cluster instead of tile
                if (inputData.shaderKeywordSet.IsEnabled(m_TileLighting))
                    return true;
            }
            else // Opaque
            {
                // If opaque, we never need transparent specific passes (even in forward only mode)
                if (isTransparentForwardPass)
                    return true;

                if (!m_CurrentHDRPAsset.renderPipelineSettings.supportForwardOnly && inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting))
                    return true;

                if (!m_CurrentHDRPAsset.renderPipelineSettings.supportForwardOnly)
                {
                    // If opaque and not forward only, then we only need the forward debug pass.
                    if (isForwardPass && !inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                        return true;
                }
            }

            // TODO: Expose development build flag.
            //if (developmentBuild && inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
            //    return true;

            // TODO: Tests for later
            // We need to find a way to strip useless shader features for passes/shader stages that don't need them (example, vertex shaders won't ever need SSS Feature flag)
            // This causes several problems:
            // - Runtime code that "finds" shader variants based on feature flags might not find them anymore... thus fall backing to the "let's give a score to variant" code path that may find the wrong variant.
            // - Another issue is that if a feature is declared without a "_" fall-back, if we strip the other variants, none may be left to use! This needs to be changed on our side.
            //if (snippet.shaderType == ShaderType.Vertex && inputData.shaderKeywordSet.IsEnabled(m_FeatureSSS))
            //    return true;

            return false;
        }


        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData)
        {
            // This test will also return if we are not using HDRenderPipelineAsset
            if (m_CurrentHDRPAsset == null || !m_CurrentHDRPAsset.allowShaderVariantStripping)
                return;

            // Do we have a shader variant stripper function for this shader?
            VariantStrippingFunc stripperFunc = null;
            m_StripperFuncs.TryGetValue(shader.name, out stripperFunc);
            if (stripperFunc == null)
                return;

            int inputShaderVariantCount = inputData.Count;

            ShaderCompilerData workaround = inputData[0];

            for (int i = 0; i < inputData.Count; ++i)
            {
                ShaderCompilerData input = inputData[i];
                if (stripperFunc(shader, snippet, input))
                {
                    inputData.RemoveAt(i);
                    i--;
                }
            }

            // Currently if a certain snippet is completely stripped (for example if you remove a whole pass) other passes might get broken
            // To work around that, we make sure that we always have at least one variant.
            if (inputData.Count == 0)
                inputData.Add(workaround);
        }
    }
#endif
}
