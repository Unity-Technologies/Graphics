// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
// using UnityEngine.Experimental.Rendering;

// [ExecuteInEditMode]
namespace UnityEngine.Rendering.SDFRP
{
    [CreateAssetMenu(menuName = "SDF/CreateSDFAssetPipeline")]
    public class SDFRenderPipelineAsset : RenderPipelineAsset
    {
        public Color clearColor = Color.green;

        [Header("Post-Processing")]
        public bool EnableDepthOfField = true;
        public int lensRes = 9;
        public float lensSiz = 2.0f;
        public float focalDis = 11.0f;

        [Header("Realtime GI")]
        public bool enableGI = true;

        public enum GIQualityLevels
        {
            Low = 0,
            Medium,
            High
        }
        public GIQualityLevels GIQuality = GIQualityLevels.Medium;

        // Currently it's not advised to change m_ProbeAtlasTextureResolution and m_ProbeResolution: I did not make addtional checks and tweaks to make sure the parameters would fit
        // But as long as you use pairs like 4096-256, 2048-128, and 1024-64, things should be fine
        internal int probeAtlasTextureResolution { get { return GIQuality == GIQualityLevels.Low ? 512 : (GIQuality == GIQualityLevels.Medium ? 1024 : 2048); } }
        internal int probeResolution { get { return GIQuality == GIQualityLevels.Low ? 32 : (GIQuality == GIQualityLevels.Medium ? 64 : 128); } }

        // Please make sure that m_GridSize.x * m_GridSize.y * m_GridSize.z <= 256 for the same reason above
        public Vector3Int gridSize = new Vector3Int(6, 6, 6);
        public Vector3 gridOrigin = new Vector3(0, 0, 0);
        public Vector3 probeDistance = new Vector3(1, 1, 1);

        internal ComputeShader rayMarchingCS;
        internal Shader tileCullingShader;
        internal ComputeShader tileDataCompressionShader;
        internal ComputeShader gatherIrradianceCS;
        internal ComputeShader giShadingCS;

        // This is a hack, normally the loader shouldn't be here
        private static string defaultPath = "Packages/com.unity.render-pipelines.sdf/";
        private static T SafeLoadAssetAtPath<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            return asset;
        }

        protected override RenderPipeline CreatePipeline()
        {
            // TODO - Need to figure out how to do this using the defaultResources
            rayMarchingCS = Resources.Load<ComputeShader>("RayMarch");
            tileCullingShader = Resources.Load<Shader>("ObjectList");
            tileDataCompressionShader = Resources.Load<ComputeShader>("TileCompression");

            gatherIrradianceCS = SafeLoadAssetAtPath<ComputeShader>(defaultPath + "Shaders/GI/GatherIrradiance.compute");
            giShadingCS = SafeLoadAssetAtPath<ComputeShader>(defaultPath + "Shaders/GI/GIShading.compute");

            return new SDFRenderPipeline();
        }
    }
}
