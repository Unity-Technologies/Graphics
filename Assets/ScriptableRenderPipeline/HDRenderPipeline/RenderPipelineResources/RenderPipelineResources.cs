namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineResources : ScriptableObject
    {
#if UNITY_EDITOR
        public const string renderPipelineResourcesPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/RenderPipelineResources/HDRenderPipelineResources.asset";

        // TODO skybox/cubemap

        [UnityEditor.MenuItem("RenderPipeline/HDRenderPipeline/Create Resources Asset")]
        static void CreateRenderPipelineResources()
        {
            var instance = CreateInstance<RenderPipelineResources>();

            instance.debugDisplayLatlongShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Debug/DebugDisplayLatlong.Shader");
            instance.debugDisplayShadowMapShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Debug/DebugDisplayShadowMap.Shader");
            instance.debugViewMaterialGBufferShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Debug/DebugViewMaterialGBuffer.Shader");
            instance.debugViewTilesShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Debug/DebugViewTiles.Shader");

            instance.deferredShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/Deferred.Shader");

            instance.clearDispatchIndirectShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/cleardispatchindirect.compute");
            instance.buildScreenAABBShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/scrbound.compute");
            instance.buildPerTileLightListShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/lightlistbuild.compute");
            instance.buildPerBigTileLightListShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/lightlistbuild-bigtile.compute");
            instance.buildPerVoxelLightListShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/lightlistbuild-clustered.compute");
            instance.shadeOpaqueShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/shadeopaque.compute");

            // SceneSettings
            instance.drawGaussianProfileShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/SceneSettings/DrawGaussianProfile.shader");
            instance.drawTransmittanceGraphShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/SceneSettings/DrawTransmittanceGraph.shader");

            // Sky
            instance.blitCubemap = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Sky/BlitCubemap.shader");
            instance.buildProbabilityTables = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Sky/BuildProbabilityTables.compute");
            instance.computeGgxIblSampleData = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Sky/ComputeGgxIblSampleData.compute");
            instance.GGXConvolve = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ScriptableRenderPipeline/HDRenderPipeline/Sky/GGXConvolve.shader");

            // Skybox/Cubemap is a builtin shader, must use Sahder.Find to access it. It is fine because we are in the editor
            instance.skyboxCubemap = Shader.Find("Skybox/Cubemap");

            UnityEditor.AssetDatabase.CreateAsset(instance, renderPipelineResourcesPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
        // Debug
        public Shader debugDisplayLatlongShader;
        public Shader debugDisplayShadowMapShader;
        public Shader debugViewMaterialGBufferShader;
        public Shader debugViewTilesShader;

        // Lighting resources
        public Shader deferredShader;

        // Lighting tile pass resources
        public ComputeShader clearDispatchIndirectShader;
        public ComputeShader buildScreenAABBShader;
        public ComputeShader buildPerTileLightListShader;     // FPTL
        public ComputeShader buildPerBigTileLightListShader;
        public ComputeShader buildPerVoxelLightListShader;    // clustered
        public ComputeShader shadeOpaqueShader;

        // SceneSettings
        public Shader drawGaussianProfileShader;
        public Shader drawTransmittanceGraphShader;

        // Sky
        public Shader blitCubemap;
        public ComputeShader buildProbabilityTables;
        public ComputeShader computeGgxIblSampleData;
        public Shader GGXConvolve;

        public Shader skyboxCubemap;
    }
}
