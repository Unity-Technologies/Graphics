namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum ShadowCascades
    {
        NO_CASCADES = 1,
        TWO_CASCADES = 2,
        FOUR_CASCADES = 4,
    }

    public enum ShadowType
    {
        NO_SHADOW = 0,
        HARD_SHADOWS,
        SOFT_SHADOWS,
    }

    public enum ShadowResolution
    {
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048
    }

    public class LightweightPipelineAsset : RenderPipelineAsset
    {
        private static readonly string m_PipelineFolder = "Assets/ScriptableRenderPipeline/LightweightPipeline";
        private static readonly string m_AssetName = "LightweightPipelineAsset.asset";

#if UNITY_EDITOR
        [UnityEditor.MenuItem("RenderPipeline/LightweightPipeline/Create Pipeline Asset")]
        static void CreateLightweightPipeline()
        {
            var instance = ScriptableObject.CreateInstance<LightweightPipelineAsset>();

            string[] paths = m_PipelineFolder.Split('/');
            string currentPath = paths[0];
            for (int i = 1; i < paths.Length; ++i)
            {
                string folder = currentPath + "/" + paths[i];
                if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                    UnityEditor.AssetDatabase.CreateFolder(currentPath, paths[i]);

                currentPath = folder;
            }

            UnityEditor.AssetDatabase.CreateAsset(instance, m_PipelineFolder + "/" + m_AssetName);
        }
#endif

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new LightweightPipeline(this);
        }

        void OnValidate()
        {
            DestroyCreatedInstances();
        }

        #region PipelineAssetSettings

        [SerializeField] private int m_MaxPixelLights = 1;
        [SerializeField] private bool m_SupportsVertexLight = true;
        [SerializeField] private bool m_EnableLightmaps = true;
        [SerializeField] private bool m_EnableAmbientProbe = true;
        [SerializeField] private ShadowType m_ShadowType = ShadowType.HARD_SHADOWS;
        [SerializeField] private ShadowResolution m_ShadowAtlasResolution = ShadowResolution._1024;
        [SerializeField] private float m_ShadowNearPlaneOffset = 2.0f;
        [SerializeField] private float m_ShadowDistance = 50.0f;
        [SerializeField] private float m_MinShadowNormalBias = 0.0005f;
        [SerializeField] private float m_ShadowNormalBias = 0.05f;
        [SerializeField] private ShadowCascades m_ShadowCascades = ShadowCascades.NO_CASCADES;
        [SerializeField] private float m_Cascade2Split = 0.25f;
        [SerializeField] private Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);

        [SerializeField] private Material m_DefaultDiffuseMaterial;
        [SerializeField] private Material m_DefaultSpriteMaterial;
        [SerializeField] private Shader m_DefaultShader;

        public int MaxSupportedPixelLights
        {
            get { return m_MaxPixelLights; }
            private set { m_MaxPixelLights = value; }
        }

        public bool SupportsVertexLight
        {
            get { return m_SupportsVertexLight; }
            private set { m_SupportsVertexLight = value; }
        }

        public bool EnableLightmap
        {
            get { return m_EnableLightmaps; }
            private set { m_EnableLightmaps = value; }
        }

        public bool EnableAmbientProbe
        {
            get { return m_EnableAmbientProbe; }
            private set { m_EnableAmbientProbe = value; }
        }

        public ShadowType CurrShadowType
        {
            get { return m_ShadowType; }
            private set { m_ShadowType = value; }
        }

        public int ShadowAtlasResolution
        {
            get { return (int)m_ShadowAtlasResolution; }
            private set { m_ShadowAtlasResolution = (ShadowResolution)value; }
        }

        public float ShadowNearOffset
        {
            get { return m_ShadowNearPlaneOffset; }
            private set { m_ShadowNearPlaneOffset = value; }
        }

        public float ShadowDistance
        {
            get { return m_ShadowDistance; }
            private set { m_ShadowDistance = value; }
        }

		public float ShadowMinNormalBias
        {
            get { return m_MinShadowNormalBias; }
            private set { m_MinShadowNormalBias = value; }
        }

        public float ShadowNormalBias
        {
            get { return m_ShadowNormalBias; }
            private set { m_ShadowNormalBias = value; }
        }

        public int CascadeCount
        {
            get { return (int)m_ShadowCascades; }
            private set { m_ShadowCascades = (ShadowCascades)value; }
        }

        public float Cascade2Split
        {
            get { return m_Cascade2Split; }
            private set { m_Cascade2Split = value; }
        }

        public Vector3 Cascade4Split
        {
            get { return m_Cascade4Split; }
            private set { m_Cascade4Split = value; }
        }

        #endregion
        public override Material GetDefaultMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultParticleMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultLineMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultUIMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultUIOverdrawMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultUIETC1SupportedMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefault2DMaterial()
        {
            return m_DefaultSpriteMaterial;
        }
        public override Shader GetDefaultShader()
        {
            return m_DefaultShader;
        }
    }
}
