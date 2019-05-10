using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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

                // All decal pass name can be see in Decalsystem.s_MaterialDecalPassNames and Decalsystem.s_MaterialSGDecalPassNames
                // All pass that have 3RT in named are use when perChannelMask is false. All 4RT are used when perChannelMask is true.
                // There is one exception, it is DBufferProjector_S that is used for both 4RT and 3RT as mention in Decal.shader
                // there is a multi-compile to handle this pass, so it will be correctly removed by testing m_Decals3RT or m_Decals4RT
                if (snippet.passName != DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector_S])
                {
                    isDecal3RTPass = snippet.passName.Contains("3RT");
                    isDecal4RTPass = !isDecal3RTPass;
                }

                // Note that we can't strip Emissive pass of decal.shader as we don't have the information here if it is used or not...
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
        List<BaseShaderPreprocessor> shaderProcessorsList;


        uint m_TotalVariantsInputCount;
        uint m_TotalVariantsOutputCount;

        public HDRPreprocessShaders()
        {
            // TODO: Grab correct configuration/quality asset.
            if (ShaderBuildPreprocessor.hdrpAssets == null || ShaderBuildPreprocessor.hdrpAssets.Count == 0)
                return;

            shaderProcessorsList = HDEditorUtils.GetBaseShaderPreprocessorList();
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
            var hdPipelineAssets = ShaderBuildPreprocessor.hdrpAssets;
            
            if (hdPipelineAssets.Count == 0)
                return;

            uint preStrippingCount = (uint)inputData.Count;
            
            // Test if striping is enabled in any of the found HDRP assets.
            if ( hdPipelineAssets.Count == 0 || !hdPipelineAssets.Any(a => a.allowShaderVariantStripping) )
                return;

            int inputShaderVariantCount = inputData.Count;

            for (int i = 0; i < inputData.Count; ++i)
            {
                ShaderCompilerData input = inputData[i];

                // Remove the input by default, until we find a HDRP Asset in the list that needs it.
                bool removeInput = true;
                
                foreach (var hdAsset in hdPipelineAssets)
                {
                    var stripedByPreprocessor = false;
                    
                    // Call list of strippers
                    // Note that all strippers cumulate each other, so be aware of any conflict here
                    foreach (BaseShaderPreprocessor shaderPreprocessor in shaderProcessorsList)
                    {
                        if ( shaderPreprocessor.ShadersStripper(hdAsset, shader, snippet, input) )
                        {
                            stripedByPreprocessor = true;
                            break;
                        }
                    }

                    if (!stripedByPreprocessor)
                    {
                        removeInput = false;
                        break;
                    }
                }

                if (removeInput)
                {
                    inputData.RemoveAt(i);
                    i--;
                }
            }

            foreach (var hdAsset in hdPipelineAssets)
            {
                if (hdAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
                {
                    m_TotalVariantsInputCount += preStrippingCount;
                    m_TotalVariantsOutputCount += (uint)inputData.Count;
                    LogShaderVariants(shader, snippet, hdAsset.shaderVariantLogLevel, preStrippingCount, (uint)inputData.Count);
                }
            }
        }
    }
    
    // Build preprocessor to find all potentially used HDRP assets.
    class ShaderBuildPreprocessor : IPreprocessBuildWithReport
    {
        private static List<HDRenderPipelineAsset> _hdrpAssets;

        public static List<HDRenderPipelineAsset> hdrpAssets
        {
            get
            {
                if (_hdrpAssets == null || _hdrpAssets.Count == 0) GetAllValidHDRPAssets();
                return _hdrpAssets;
            }
        }

        static void GetAllValidHDRPAssets()
        {
            if (_hdrpAssets != null) hdrpAssets.Clear();
            else _hdrpAssets = new List<HDRenderPipelineAsset>();
            
            // Add to the list the HDRP asset currently set in the graphic settings.
            if ( GraphicsSettings.renderPipelineAsset != null && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset )
                _hdrpAssets.Add(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            
            // Get all enabled scenes path in the build settings.
            var scenesPaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path);

            // Find all HDRP assets that are dependencies of the scenes.
            _hdrpAssets = scenesPaths.Aggregate( new List<HDRenderPipelineAsset>(),
                (list, scene) =>
                {
                    list.AddRange(
                        AssetDatabase.GetDependencies(scene)
                            .Select(AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>)
                            .Where( a => a != null && !list.Contains(a) )
                        );
                    return list;
                });

            // Add the HDRP assets that are in the Resources folders.
            _hdrpAssets.AddRange(
                Resources.FindObjectsOfTypeAll<HDRenderPipelineAsset>()
                .Where( a => !_hdrpAssets.Contains(a) )
                );
            
            // Prompt a warning if we find 0 HDRP Assets.
            if (_hdrpAssets.Count == 0)
                if (EditorUtility.DisplayDialog("HDRP Asset missing", "No HDRP Asset has been set in the Graphic Settings, and no potential used in the build HDRP Asset has been found. If you want to continue compiling, this might lead no VERY long compilation time.", "Ok", "Cancel"))
                throw new UnityEditor.Build.BuildFailedException("Build canceled");

            /*
            Debug.Log(string.Format("{0} HDRP assets in build:{1}",
                _hdrpAssets.Count,
                _hdrpAssets
                    .Select(a => a.name)
                    .Aggregate("", (current, next) => $"{current}{System.Environment.NewLine}- {next}" )
                ));
            // */
        }
        
        public int callbackOrder { get { return 0; } }
        
        public void OnPreprocessBuild(BuildReport report)
        {
            GetAllValidHDRPAssets();
        }
    }
}
