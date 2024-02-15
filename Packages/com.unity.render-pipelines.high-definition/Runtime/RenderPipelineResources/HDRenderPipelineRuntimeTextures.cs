using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Textures", Order = 1000), HideInInspector]
    class HDRenderPipelineRuntimeTextures : IRenderPipelineResources
    {
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        // Debug
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/DebugFont.tga")]
        private Texture2D m_DebugFontTex;
        public Texture2D debugFontTex
        {
            get => m_DebugFontTex;
            set => this.SetValueAndNotify(ref m_DebugFontTex, value);
        }

        [SerializeField][ResourcePath("Runtime/Debug/ColorGradient.png")]
        private Texture2D m_ColorGradient;
        public Texture2D colorGradient
        {
            get => m_ColorGradient;
            set => this.SetValueAndNotify(ref m_ColorGradient, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/Matcap/DefaultMatcap.png")]
        private Texture2D m_MatcapTex;
        public Texture2D matcapTex
        {
            get => m_MatcapTex;
            set => this.SetValueAndNotify(ref m_MatcapTex, value);
        }

        // Pre-baked noise
        [SerializeField][ResourceFormattedPaths("Runtime/RenderPipelineResources/Texture/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
        private Texture2D[] m_BlueNoise16LTex = new Texture2D[32];
        public Texture2D[] blueNoise16LTex
        {
            get => m_BlueNoise16LTex;
            set => this.SetValueAndNotify(ref m_BlueNoise16LTex, value);
        }

        [SerializeField][ResourceFormattedPaths("Runtime/RenderPipelineResources/Texture/BlueNoise16/RGB/LDR_RGB1_{0}.png", 0, 32)]
        private Texture2D[] m_BlueNoise16RGBTex = new Texture2D[32];
        public Texture2D[] blueNoise16RGBTex
        {
            get => m_BlueNoise16RGBTex;
            set => this.SetValueAndNotify(ref m_BlueNoise16RGBTex, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/OwenScrambledNoise4.png")]
        private Texture2D m_OwenScrambledRGBATex;
        public Texture2D owenScrambledRGBATex
        {
            get => m_OwenScrambledRGBATex;
            set => this.SetValueAndNotify(ref m_OwenScrambledRGBATex, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/OwenScrambledNoise256.png")]
        private Texture2D m_OwenScrambled256Tex;
        public Texture2D owenScrambled256Tex
        {
            get => m_OwenScrambled256Tex;
            set => this.SetValueAndNotify(ref m_OwenScrambled256Tex, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/ScrambleNoise.png")]
        private Texture2D m_ScramblingTex;
        public Texture2D scramblingTex
        {
            get => m_ScramblingTex;
            set => this.SetValueAndNotify(ref m_ScramblingTex, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/RankingTile1SPP.png")]
        private Texture2D m_RankingTile1SPP;
        public Texture2D rankingTile1SPP
        {
            get => m_RankingTile1SPP;
            set => this.SetValueAndNotify(ref m_RankingTile1SPP, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/ScramblingTile1SPP.png")]
        private Texture2D m_ScramblingTile1SPP;
        public Texture2D scramblingTile1SPP
        {
            get => m_ScramblingTile1SPP;
            set => this.SetValueAndNotify(ref m_ScramblingTile1SPP, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/RankingTile8SPP.png")]
        private Texture2D m_RankingTile8SPP;
        public Texture2D rankingTile8SPP
        {
            get => m_RankingTile8SPP;
            set => this.SetValueAndNotify(ref m_RankingTile8SPP, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/ScramblingTile8SPP.png")]
        private Texture2D m_ScramblingTile8SPP;
        public Texture2D scramblingTile8SPP
        {
            get => m_ScramblingTile8SPP;
            set => this.SetValueAndNotify(ref m_ScramblingTile8SPP, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/RankingTile256SPP.png")]
        private Texture2D m_RankingTile256SPP;
        public Texture2D rankingTile256SPP
        {
            get => m_RankingTile256SPP;
            set => this.SetValueAndNotify(ref m_RankingTile256SPP, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/CoherentNoise/ScramblingTile256SPP.png")]
        private Texture2D m_ScramblingTile256SPP;
        public Texture2D scramblingTile256SPP
        {
            get => m_ScramblingTile256SPP;
            set => this.SetValueAndNotify(ref m_ScramblingTile256SPP, value);
        }

        // Precalculated eye caustic LUT
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/EyeCausticLUT16R.exr")]
        private Texture3D m_EyeCausticLUT;
        public Texture3D eyeCausticLUT
        {
            get => m_EyeCausticLUT;
            set => this.SetValueAndNotify(ref m_EyeCausticLUT, value);
        }

        // Hair LUT
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/HairAttenuationLUT.asset")]
        public Texture3D m_HairAttenuationLUT;
        public Texture3D hairAttenuationLUT
        {
            get => m_HairAttenuationLUT;
            set => this.SetValueAndNotify(ref m_HairAttenuationLUT, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/HairAzimuthalScatteringLUT.asset")]
        public Texture3D m_HairAzimuthalScatteringLUT;
        public Texture3D hairAzimuthalScatteringLUT
        {
            get => m_HairAzimuthalScatteringLUT;
            set => this.SetValueAndNotify(ref m_HairAzimuthalScatteringLUT, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/HairLongitudinalScatteringLUT.asset")]
        public Texture3D m_HairLongitudinalScatteringLUT;
        public Texture3D hairLongitudinalScatteringLUT
        {
            get => m_HairLongitudinalScatteringLUT;
            set => this.SetValueAndNotify(ref m_HairLongitudinalScatteringLUT, value);
        }

        // Clouds textures
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/CloudLutRainAO.png")]
        private Texture2D m_CloudLutRainAO;
        public Texture2D cloudLutRainAO
        {
            get => m_CloudLutRainAO;
            set => this.SetValueAndNotify(ref m_CloudLutRainAO, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/WorleyNoise128RGBA.png")]
        private Texture3D m_WorleyNoise128RGBA;
        public Texture3D worleyNoise128RGBA
        {
            get => m_WorleyNoise128RGBA;
            set => this.SetValueAndNotify(ref m_WorleyNoise128RGBA, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/WorleyNoise32RGB.png")]
        private Texture3D m_WorleyNoise32RGB;
        public Texture3D worleyNoise32RGB
        {
            get => m_WorleyNoise32RGB;
            set => this.SetValueAndNotify(ref m_WorleyNoise32RGB, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/VolumetricClouds/PerlinNoise32RGB.png")]
        private Texture3D m_PerlinNoise32RGB;
        public Texture3D perlinNoise32RGB
        {
            get => m_PerlinNoise32RGB;
            set => this.SetValueAndNotify(ref m_PerlinNoise32RGB, value);
        }

        // Water textures
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/Water/FoamMask.png")]
        private Texture2D m_FoamMask;
        public Texture2D foamMask
        {
            get => m_FoamMask;
            set => this.SetValueAndNotify(ref m_FoamMask, value);
        }
        // Post-processing
        [SerializeField][ResourcePaths(new[]
        {
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Thin01.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Thin02.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Medium01.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Medium02.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Medium03.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Medium04.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Medium05.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Medium06.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Large01.png",
            "Runtime/RenderPipelineResources/Texture/FilmGrain/Large02.png"
        })]
        private Texture2D[] m_FilmGrainTex;
        public Texture2D[] filmGrainTex
        {
            get => m_FilmGrainTex;
            set => this.SetValueAndNotify(ref m_FilmGrainTex, value);
        }
        
        // SMAA Textures
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/SMAA/SearchTex.tga")]
        private Texture2D m_SMAASearchTex;
        public Texture2D SMAASearchTex
        {
            get => m_SMAASearchTex;
            set => this.SetValueAndNotify(ref m_SMAASearchTex, value);
        }

        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/SMAA/AreaTex.tga")]
        private Texture2D m_SMAAAreaTex;
        public Texture2D SMAAAreaTex
        {
            get => m_SMAAAreaTex;
            set => this.SetValueAndNotify(ref m_SMAAAreaTex, value);
        }

        // Default HDRISky
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/DefaultHDRISky.exr")]
        private Cubemap m_DefaultHDRISky;
        public Cubemap defaultHDRISky
        {
            get => m_DefaultHDRISky;
            set => this.SetValueAndNotify(ref m_DefaultHDRISky, value);
        }

        // Default Cloud Map
        [SerializeField][ResourcePath("Runtime/RenderPipelineResources/Texture/DefaultCloudMap.png")]
        private Texture2D m_DefaultCloudMap;
        public Texture2D defaultCloudMap
        {
            get => m_DefaultCloudMap;
            set => this.SetValueAndNotify(ref m_DefaultCloudMap, value);
        }
    }
}
