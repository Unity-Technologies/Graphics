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

        static string s_DefaultMaterialPath
        {
            get { return HDEditorUtils.GetHDRenderPipelinePath() + "RenderPipelineResources/DefaultHDMaterial.mat"; }
        }

        static string s_DefaultShaderPath
        {
            get { return HDEditorUtils.GetHDRenderPipelinePath() + "Material/Lit/Lit.shader"; }
        }

        class DoCreateNewAssetHDRenderPipeline : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<HDRenderPipelineAsset>();
                newAsset.name = Path.GetFileName(pathName);
                // Load default renderPipelineResources / Material / Shader
                newAsset.renderPipelineResources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(s_RenderPipelineResourcesPath);
                newAsset.defaultDiffuseMaterial = AssetDatabase.LoadAssetAtPath<Material>(s_DefaultMaterialPath);
                newAsset.defaultShader = AssetDatabase.LoadAssetAtPath<Shader>(s_DefaultShaderPath);
                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        [MenuItem("Assets/Create/Render Pipeline/High Definition/Render Pipeline", priority = CoreUtils.assetCreateMenuPriority1)]
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
                string PostProcessingPath = HDEditorUtils.GetPostProcessingPath();
                string CorePath = HDEditorUtils.GetCorePath();

                newAsset.debugDisplayLatlongShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader");
                newAsset.debugViewMaterialGBufferShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader");
                newAsset.debugViewTilesShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader");
                newAsset.debugFullScreenShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader");

                newAsset.deferredShader = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader");
                newAsset.subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/Lit/Resources/SubsurfaceScattering.compute");
                newAsset.gaussianPyramidCS = Load<ComputeShader>(PostProcessingPath + "Shaders/Builtins/GaussianDownsample.compute");
                newAsset.depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/DepthDownsample.compute");
                newAsset.copyChannelCS = Load<ComputeShader>(CorePath + "Resources/GPUCopy.compute");
                newAsset.applyDistortionCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ApplyDistorsion.compute");

                newAsset.clearDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/cleardispatchindirect.compute");
                newAsset.buildDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/builddispatchindirect.compute");
                newAsset.buildScreenAABBShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/scrbound.compute");
                newAsset.buildPerTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild.compute");
                newAsset.buildPerBigTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-bigtile.compute");
                newAsset.buildPerVoxelLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-clustered.compute");
                newAsset.buildMaterialFlagsShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/materialflags.compute");
                newAsset.deferredComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/Deferred.compute");

                newAsset.deferredDirectionalShadowComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/DeferredDirectionalShadow.compute");

                // SceneSettings
                // These shaders don't need to be reference by RenderPipelineResource as they are not use at runtime (only to draw in editor)
                // instance.drawSssProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDRenderPipelinePath + "SceneSettings/DrawSssProfile.shader");
                // instance.drawTransmittanceGraphShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDRenderPipelinePath + "SceneSettings/DrawTransmittanceGraph.shader");

                newAsset.cameraMotionVectors = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CameraMotionVectors.shader");

                // Sky
                newAsset.blitCubemap = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader");
                newAsset.buildProbabilityTables = Load<ComputeShader>(HDRenderPipelinePath + "Sky/BuildProbabilityTables.compute");
                newAsset.computeGgxIblSampleData = Load<ComputeShader>(HDRenderPipelinePath + "Sky/ComputeGgxIblSampleData.compute");
                newAsset.GGXConvolve = Load<Shader>(HDRenderPipelinePath + "Sky/GGXConvolve.shader");
                newAsset.opaqueAtmosphericScattering = Load<Shader>(HDRenderPipelinePath + "Sky/OpaqueAtmosphericScattering.shader");

                // Skybox/Cubemap is a builtin shader, must use Sahder.Find to access it. It is fine because we are in the editor
                newAsset.skyboxCubemap = Shader.Find("Skybox/Cubemap");

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        [MenuItem("Assets/Create/Render Pipeline/High Definition/Render Pipeline Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateRenderPipelineResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetHDRenderPipelineResources>(), "New HDRenderPipelineResources.asset", icon, null);
        }
    }
}
