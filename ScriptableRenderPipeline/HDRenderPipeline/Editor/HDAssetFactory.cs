using UnityEngine;
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

        [MenuItem("RenderPipeline/HDRenderPipeline/Create Pipeline Asset", false, 16)]
        static void CreateHDRenderPipeline()
        {
            var instance = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            AssetDatabase.CreateAsset(instance, HDEditorUtils.GetHDRenderPipelinePath() + "HDRenderPipelineAsset.asset");

            // If it exist, load renderPipelineResources
            instance.renderPipelineResources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(s_RenderPipelineResourcesPath);
        }

        // TODO skybox/cubemap

        [MenuItem("RenderPipeline/HDRenderPipeline/Create Resources Asset", false, 15)]
        static void CreateRenderPipelineResources()
        {
            string HDRenderPipelinePath = HDEditorUtils.GetHDRenderPipelinePath();
            string PostProcessingPath = HDEditorUtils.GetPostProcessingPath();
            string CorePath = HDEditorUtils.GetCorePath();

            var instance = ScriptableObject.CreateInstance<RenderPipelineResources>();

            instance.debugDisplayLatlongShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader");
            instance.debugViewMaterialGBufferShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader");
            instance.debugViewTilesShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader");
            instance.debugFullScreenShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader");

            instance.deferredShader = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader");
            instance.subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/Lit/Resources/SubsurfaceScattering.compute");
            instance.volumetricLightingCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Volumetrics/Resources/VolumetricLighting.compute");
            instance.gaussianPyramidCS = Load<ComputeShader>(PostProcessingPath + "Shaders/Builtins/GaussianDownsample.compute");
            instance.depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/DepthDownsample.compute");
            instance.copyChannelCS = Load<ComputeShader>(CorePath + "Resources/GPUCopy.compute");
            instance.applyDistortionCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ApplyDistorsion.compute");

            instance.clearDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/cleardispatchindirect.compute");
            instance.buildDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/builddispatchindirect.compute");
            instance.buildScreenAABBShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/scrbound.compute");
            instance.buildPerTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/lightlistbuild.compute");
            instance.buildPerBigTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/lightlistbuild-bigtile.compute");
            instance.buildPerVoxelLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/lightlistbuild-clustered.compute");
            instance.buildMaterialFlagsShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/materialflags.compute");
            instance.deferredComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/Deferred.compute");

            instance.deferredDirectionalShadowComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/TilePass/DeferredDirectionalShadow.compute");

            // SceneSettings
            // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime (only to draw in editor)
            // instance.drawSssProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDRenderPipelinePath + "SceneSettings/DrawSssProfile.shader");
            // instance.drawTransmittanceGraphShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDRenderPipelinePath + "SceneSettings/DrawTransmittanceGraph.shader");

            instance.cameraMotionVectors = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CameraMotionVectors.shader");

            // Sky
            instance.blitCubemap = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader");
            instance.buildProbabilityTables = Load<ComputeShader>(HDRenderPipelinePath + "Sky/BuildProbabilityTables.compute");
            instance.computeGgxIblSampleData = Load<ComputeShader>(HDRenderPipelinePath + "Sky/ComputeGgxIblSampleData.compute");
            instance.GGXConvolve = Load<Shader>(HDRenderPipelinePath + "Sky/GGXConvolve.shader");
            instance.opaqueAtmosphericScattering = Load<Shader>(HDRenderPipelinePath + "Sky/OpaqueAtmosphericScattering.shader");

            // Skybox/Cubemap is a builtin shader, must use Sahder.Find to access it. It is fine because we are in the editor
            instance.skyboxCubemap = Shader.Find("Skybox/Cubemap");

            AssetDatabase.CreateAsset(instance, s_RenderPipelineResourcesPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Note: move this to a static using once we can target C#6+
        static T Load<T>(string path)
            where T : UnityObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
