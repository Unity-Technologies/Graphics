using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // The common shader stripper function 
    public class CommonShaderPreprocessor : BaseShaderPreprocessor
    {
        public CommonShaderPreprocessor() { }

        public override bool ShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // Strip every useless shadow configs
            var shadowInitParams = hdrpAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams;

            foreach (var shadowVariant in m_ShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.shadowQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(shadowVariant.Value))
                        return true;
            }

            // CAUTION: Pass Name and Lightmode name must match in master node and .shader.
            // HDRP use LightMode to do drawRenderer and pass name is use here for stripping!

            // Remove editor only pass
            bool isSceneSelectionPass = snippet.passName == "SceneSelectionPass";
            if (isSceneSelectionPass)
                return true;

            // CAUTION: We can't identify transparent material in the stripped in a general way.
            // Shader Graph don't produce any keyword - However it will only generate the pass that are required, so it already handle transparent (Note that shader Graph still define _SURFACE_TYPE_TRANSPARENT but as a #define)
            // For inspector version of shader, we identify transparent with a shader feature _SURFACE_TYPE_TRANSPARENT.
            // Only our Lit (and inherited) shader use _SURFACE_TYPE_TRANSPARENT, so the specific stripping based on this keyword is in LitShadePreprocessor.
            // Here we can't strip based on opaque or transparent but we will strip based on HDRP Asset configuration.

            bool isMotionPass = snippet.passName == "MotionVectors";
            bool isTransparentPrepass = snippet.passName == "TransparentDepthPrepass";
            bool isTransparentPostpass = snippet.passName == "TransparentDepthPostpass";
            bool isTransparentBackface = snippet.passName == "TransparentBackface";
            bool isDistortionPass = snippet.passName == "DistortionVectors";

            if (isMotionPass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                return true;

            if (isDistortionPass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportDistortion)
                return true;

            if (isTransparentBackface && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTransparentBackface)
                return true;

            if (isTransparentPrepass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTransparentDepthPrepass)
                return true;

            if (isTransparentPostpass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTransparentDepthPostpass)
                return true;

            // If we are in a release build, don't compile debug display variant
            // Also don't compile it if not requested by the render pipeline settings
            if ((/*!Debug.isDebugBuild || */ !hdrpAsset.currentPlatformRenderPipelineSettings.supportRuntimeDebugDisplay) && inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_LodFadeCrossFade) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportDitheringCrossFade)
                return true;
           
            if (inputData.shaderKeywordSet.IsEnabled(m_WriteMSAADepth) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMSAA)
                return true;

            // Note that this is only going to affect the deferred shader and for a debug case, so it won't save much.
            if (inputData.shaderKeywordSet.IsEnabled(m_SubsurfaceScattering) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportSubsurfaceScattering)
                return true;

            // DECAL

            // Identify when we compile a decal shader
            bool isDecal3RTPass = false;
            bool isDecal4RTPass = false;
            bool isDecalPass = false;

            if (snippet.passName.Contains("DBufferMesh") || snippet.passName.Contains("DBufferProjector"))
            {
                isDecalPass = true;

                // All decal pass name:
                // "ShaderGraph_DBufferMesh3RT" "ShaderGraph_DBufferProjector3RT" "DBufferMesh_3RT"
                // "DBufferProjector_M" "DBufferProjector_AO" "DBufferProjector_MAO" "DBufferProjector_S" "DBufferProjector_MS" "DBufferProjector_AOS" "DBufferProjector_MAOS"
                // "DBufferMesh_M" "DBufferMesh_AO" "DBufferMesh_MAO" "DBufferMesh_S" "DBufferMesh_MS" "DBufferMesh_AOS""DBufferMesh_MAOS"

                // Caution: As mention in Decal.shader DBufferProjector_S is also DBufferProjector_3RT so this pass is both 4RT and 3RT
                // there is a multi-compile to handle this pass, so it will be correctly removed by testing m_Decals3RT or m_Decals4RT
                if (snippet.passName != "DBufferProjector_S")
                {
                    isDecal3RTPass = snippet.passName.Contains("3RT");
                    isDecal4RTPass = !isDecal3RTPass;
                }
            }

            // If decal support, remove unused variant
            if (hdrpAsset.currentPlatformRenderPipelineSettings.supportDecals)
            {
                // Remove the no decal case
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalsOFF))
                    return true;

                // If decal but with 4RT remove 3RT variant and vice versa
                if ((inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) || isDecal3RTPass) && hdrpAsset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask)
                    return true;

                if ((inputData.shaderKeywordSet.IsEnabled(m_Decals4RT) || isDecal4RTPass) && !hdrpAsset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask)
                    return true;
            }
            else
            {
                if (isDecalPass)
                    return true;

                // If no decal support, remove decal variant
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) || inputData.shaderKeywordSet.IsEnabled(m_Decals4RT))
                    return true;
            }

            return false;
        }
    }

    class HDRPreprocessShaders : IPreprocessShaders
    {
        // Track list of materials asking for specific preprocessor step
        List<BaseShaderPreprocessor> materialList;


        uint m_TotalVariantsInputCount;
        uint m_TotalVariantsOutputCount;

        public HDRPreprocessShaders()
        {
            // TODO: Grab correct configuration/quality asset.
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdPipelineAsset == null)
                return;

            materialList = HDEditorUtils.GetBaseShaderPreprocessorList();
        }

        void LogShaderVariants(Shader shader, ShaderSnippetData snippetData, ShaderVariantLogLevel logLevel, uint prevVariantsCount, uint currVariantsCount)
        {
            if (logLevel == ShaderVariantLogLevel.AllShaders ||
                (logLevel == ShaderVariantLogLevel.OnlyHDRPShaders && shader.name.Contains("HDRP")))
            {
                float percentageCurrent = ((float)currVariantsCount / prevVariantsCount) * 100.0f;
                float percentageTotal = ((float)m_TotalVariantsOutputCount / m_TotalVariantsInputCount) * 100.0f;

                string result = string.Format("STRIPPING: {0} ({1} pass) ({2}) -" +
                        " Remaining shader variants = {3}/{4} = {5}% - Total = {6}/{7} = {8}%",
                        shader.name, snippetData.passName, snippetData.shaderType.ToString(), currVariantsCount,
                        prevVariantsCount, percentageCurrent, m_TotalVariantsOutputCount, m_TotalVariantsInputCount,
                        percentageTotal);
                Debug.Log(result);
            }
        }


        public int callbackOrder { get { return 0; } }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData)
        {
            // TODO: Grab correct configuration/quality asset.
            HDRenderPipelineAsset hdPipelineAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdPipelineAsset == null)
                return;

            uint preStrippingCount = (uint)inputData.Count;

            // This test will also return if we are not using HDRenderPipelineAsset
            if (hdPipelineAsset == null || !hdPipelineAsset.allowShaderVariantStripping)
                return;

            int inputShaderVariantCount = inputData.Count;

            for (int i = 0; i < inputData.Count; ++i)
            {
                ShaderCompilerData input = inputData[i];

                bool removeInput = false;
                // Call list of strippers
                // Note that all strippers cumulate each other, so be aware of any conflict here
                foreach (BaseShaderPreprocessor material in materialList)
                {
                    if (material.ShadersStripper(hdPipelineAsset, shader, snippet, input))
                        removeInput = true;
                }

                if (removeInput)
                {
                    inputData.RemoveAt(i);
                    i--;
                }
            }

            if (hdPipelineAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                m_TotalVariantsInputCount += preStrippingCount;
                m_TotalVariantsOutputCount += (uint)inputData.Count;
                LogShaderVariants(shader, snippet, hdPipelineAsset.shaderVariantLogLevel, preStrippingCount, (uint)inputData.Count);
            }
        }
    }
}
