namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineResources : ScriptableObject
    {
#if UNITY_EDITOR
        public const string renderPipelineResourcesPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/RenderPipelineResources/HDRenderPipelineResources.asset";

        public const string clearDispatchIndirectShaderPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/cleardispatchindirect.compute";
        public const string buildScreenAABBShaderPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/scrbound.compute";
        public const string buildPerTileLightListShaderPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/lightlistbuild.compute";
        public const string buildPerBigTileLightListShaderPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/lightlistbuild-bigtile.compute";
        public const string buildPerVoxelLightListShaderPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/lightlistbuild-clustered.compute";
        public const string shadeOpaqueShaderPath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/shadeopaque.compute";

        [UnityEditor.MenuItem("RenderPipeline/HDRenderPipeline/Create Resources Asset")]
        static void CreateRenderPipelineResources()
        {
            var instance = CreateInstance<RenderPipelineResources>();

            instance.clearDispatchIndirectShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(clearDispatchIndirectShaderPath);
            instance.buildScreenAABBShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(buildScreenAABBShaderPath);
            instance.buildPerTileLightListShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(buildPerTileLightListShaderPath);
            instance.buildPerBigTileLightListShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(buildPerBigTileLightListShaderPath);
            instance.buildPerVoxelLightListShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(buildPerVoxelLightListShaderPath);
            instance.shadeOpaqueShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(shadeOpaqueShaderPath);

            UnityEditor.AssetDatabase.CreateAsset(instance, renderPipelineResourcesPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
        // Tile pass resources
        public ComputeShader clearDispatchIndirectShader = null;
        public ComputeShader buildScreenAABBShader = null;
        public ComputeShader buildPerTileLightListShader = null;     // FPTL
        public ComputeShader buildPerBigTileLightListShader = null;
        public ComputeShader buildPerVoxelLightListShader = null;    // clustered
        public ComputeShader shadeOpaqueShader = null;
    }
}
