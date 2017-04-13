namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class TilePassResources : ScriptableObject
    {
#if UNITY_EDITOR
        public const string tilePassResources = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/TilePassResources.asset";

        [UnityEditor.MenuItem("HDRenderPipeline/TilePass/CreateTilePassResources")]
        static void CreateTilePassSetup()
        {
            var instance = CreateInstance<TilePassResources>();
            UnityEditor.AssetDatabase.CreateAsset(instance, tilePassResources);
        }

#endif
        public ComputeShader buildScreenAABBShader = null;
        public ComputeShader buildPerTileLightListShader = null;     // FPTL
        public ComputeShader buildPerBigTileLightListShader = null;
        public ComputeShader buildPerVoxelLightListShader = null;    // clustered
        public ComputeShader clearDispatchIndirectShader = null;
        public ComputeShader shadeOpaqueShader = null;

        // Various set of material use in render loop
        public Shader m_DebugViewMaterialGBuffer;

        // For image based lighting
        public Shader m_InitPreFGD;
    }
}
