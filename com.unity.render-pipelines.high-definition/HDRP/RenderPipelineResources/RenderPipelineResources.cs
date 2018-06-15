namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineResources : ScriptableObject
    {
        [HideInInspector]
        public float version = 1.0f;

        // Default Material / Shader
        public Material defaultDiffuseMaterial;
        public Material defaultMirrorMaterial;
        public Material defaultDecalMaterial;
        public Shader defaultShader;

        // Debug
        public Texture2D debugFontTexture;
        public Shader debugDisplayLatlongShader;
        public Shader debugViewMaterialGBufferShader;
        public Shader debugViewTilesShader;
        public Shader debugFullScreenShader;
        public Shader debugColorPickerShader;

        // Lighting resources
        public Shader deferredShader;
        public ComputeShader colorPyramidCS;
        public ComputeShader depthPyramidCS;
        public ComputeShader copyChannelCS;
        public ComputeShader texturePaddingCS;
        public ComputeShader applyDistortionCS;

        // Lighting tile pass resources
        public ComputeShader clearDispatchIndirectShader;
        public ComputeShader buildDispatchIndirectShader;
        public ComputeShader buildScreenAABBShader;
        public ComputeShader buildPerTileLightListShader;     // FPTL
        public ComputeShader buildPerBigTileLightListShader;
        public ComputeShader buildPerVoxelLightListShader;    // clustered
        public ComputeShader buildMaterialFlagsShader;
        public ComputeShader deferredComputeShader;
        public ComputeShader screenSpaceShadowComputeShader;
        public ComputeShader volumeVoxelizationCS;
        public ComputeShader volumetricLightingCS;

        public ComputeShader subsurfaceScatteringCS; // Disney SSS
        public Shader subsurfaceScattering; // Jimenez SSS
        public Shader combineLighting;

        // General
        public Shader cameraMotionVectors;
        public Shader copyStencilBuffer;
        public Shader copyDepthBuffer;
        public Shader blit;

        // Sky
        public Shader blitCubemap;
        public ComputeShader buildProbabilityTables;
        public ComputeShader computeGgxIblSampleData;
        public Shader GGXConvolve;
        public Shader opaqueAtmosphericScattering;
        public Shader hdriSky;
        public Shader proceduralSky;
        public Shader skyboxCubemap;

        // Material
        public Shader preIntegratedFGD_GGXDisneyDiffuse;
        public Shader preIntegratedFGD_CharlieClothLambert;

        // Utilities / Core
        public ComputeShader encodeBC6HCS;
        public Shader cubeToPanoShader;
        public Shader blitCubeTextureFace;

        // Shadow
        public Shader shadowClearShader;
        public ComputeShader shadowBlurMoments;
        public Shader debugShadowMapShader;
    }
}
