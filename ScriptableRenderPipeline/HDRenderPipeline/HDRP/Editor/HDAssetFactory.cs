using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using UnityObject = UnityEngine.Object;

    static class HDAssetFactory
    {
        static string s_RenderPipelineResourcesPath
        {
            get { return HDEditorUtils.GetHDRenderPipelinePath() + "RenderPipelineResources/HDRenderPipelineResources.asset"; }
        }

        class DoCreateNewAssetHDRenderPipeline : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<HDRenderPipelineAsset>();
                newAsset.name = Path.GetFileName(pathName);
                // Load default renderPipelineResources / Material / Shader
                newAsset.renderPipelineResources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(s_RenderPipelineResourcesPath);
                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        [MenuItem("Assets/Create/Rendering/High Definition Render Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateHDRenderPipeline()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipeline>(), "New HDRenderPipelineAsset.asset", icon, null);
        }

        // Note: move this to a static using once we can target C#6+
        static T Load<T>(string path) where T : UnityObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        class DoCreateNewAssetHDRenderPipelineResources : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<RenderPipelineResources>();
                newAsset.name = Path.GetFileName(pathName);

                // Load default renderPipelineResources / Material / Shader
                string HDRenderPipelinePath = HDEditorUtils.GetHDRenderPipelinePath();
                string CorePath = HDEditorUtils.GetCorePath();

                newAsset.defaultDiffuseMaterial = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDMaterial.mat");
                newAsset.defaultDecalMaterial = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDDecalMaterial.mat");
                newAsset.defaultShader = Load<Shader>(HDRenderPipelinePath + "Material/Lit/Lit.shader");

                newAsset.debugFontTexture = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/DebugFont.tga");
                newAsset.debugDisplayLatlongShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader");
                newAsset.debugViewMaterialGBufferShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader");
                newAsset.debugViewTilesShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader");
                newAsset.debugFullScreenShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader");
                newAsset.debugColorPickerShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugColorPicker.Shader");

                newAsset.deferredShader = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader");
                newAsset.colorPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ColorPyramid.compute");
                newAsset.depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/DepthPyramid.compute");
                newAsset.copyChannelCS = Load<ComputeShader>(CorePath + "CoreResources/GPUCopy.compute");
                newAsset.applyDistortionCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ApplyDistorsion.compute");

                newAsset.clearDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/cleardispatchindirect.compute");
                newAsset.buildDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/builddispatchindirect.compute");
                newAsset.buildScreenAABBShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/scrbound.compute");
                newAsset.buildPerTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild.compute");
                newAsset.buildPerBigTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-bigtile.compute");
                newAsset.buildPerVoxelLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-clustered.compute");
                newAsset.buildMaterialFlagsShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/materialflags.compute");
                newAsset.deferredComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/Deferred.compute");

                newAsset.deferredDirectionalShadowComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/DeferredDirectionalShadow.compute");
                newAsset.volumetricLightingCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Volumetrics/Resources/VolumetricLighting.compute");

                newAsset.subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.compute");
                newAsset.subsurfaceScattering = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.shader");
                newAsset.combineLighting = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/CombineLighting.shader");

                // General
                newAsset.cameraMotionVectors = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CameraMotionVectors.shader");
                newAsset.copyStencilBuffer = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CopyStencilBuffer.shader");
                newAsset.copyDepthBuffer = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CopyDepthBuffer.shader");
                newAsset.blit = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/Blit.shader");

                // Sky
                newAsset.blitCubemap = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader");
                newAsset.buildProbabilityTables = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/BuildProbabilityTables.compute");
                newAsset.computeGgxIblSampleData = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/ComputeGgxIblSampleData.compute");
                newAsset.GGXConvolve = Load<Shader>(HDRenderPipelinePath + "Material/GGXConvolution/GGXConvolve.shader");
                newAsset.opaqueAtmosphericScattering = Load<Shader>(HDRenderPipelinePath + "Sky/OpaqueAtmosphericScattering.shader");

                // Utilities / Core
                newAsset.encodeBC6HCS = Load<ComputeShader>(CorePath + "CoreResources/EncodeBC6H.compute");
                newAsset.cubeToPanoShader = Load<Shader>(CorePath + "CoreResources/CubeToPano.shader");
                newAsset.blitCubeTextureFace = Load<Shader>(CorePath + "CoreResources/BlitCubeTextureFace.shader");

                // Skybox/Cubemap is a builtin shader, must use Sahder.Find to access it. It is fine because we are in the editor
                newAsset.skyboxCubemap = Shader.Find("Skybox/Cubemap");

                // Shadow
                newAsset.shadowClearShader = Load<Shader>(CorePath + "Shadow/ShadowClear.shader");
                newAsset.shadowBlurMoments = Load<ComputeShader>(CorePath + "Shadow/ShadowBlurMoments.compute");
                newAsset.debugShadowMapShader = Load<Shader>(CorePath + "Shadow/DebugDisplayShadowMap.shader");

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        [MenuItem("Assets/Create/Rendering/High Definition Render Pipeline Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateRenderPipelineResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipelineResources>(), "New HDRenderPipelineResources.asset", icon, null);
        }
    }
}
