using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Experimental.Rendering.HDPipeline.TilePass;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class TileLightLoopProducer : LightLoopProducer
    {
#if UNITY_EDITOR
        public const string TilePassProducer = "Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/TilePassProducer.asset";

        [UnityEditor.MenuItem("HDRenderPipeline/TilePass/Create TileLightLoopProducer")]
        static void CreateTileLightLoopProducer()
        {
            var instance = CreateInstance<TileLightLoopProducer>();
            UnityEditor.AssetDatabase.CreateAsset(instance, TilePassProducer);


            instance.m_PassResources = AssetDatabase.LoadAssetAtPath<TilePassResources>(TilePassResources.tilePassResources);
        }

#endif
        [Serializable]
        public class TileSettings
        {
            public bool enableTileAndCluster; // For debug / test
            public bool enableSplitLightEvaluation;
            public bool enableComputeLightEvaluation;
            public bool enableComputeFeatureVariants;

            // clustered light list specific buffers and data begin
            public bool enableClustered;
            public bool enableFptlForOpaqueWhenClustered; // still useful on opaques. Should be true by default to force tile on opaque.
            public bool enableBigTilePrepass;

            [Range(0.0f, 1.0f)]
            public float diffuseGlobalDimmer = 1.0f;
            [Range(0.0f, 1.0f)]
            public float specularGlobalDimmer = 1.0f;

            public enum TileDebug : int { None = 0, Punctual = 1, Area = 2, AreaAndPunctual = 3, Environment = 4, EnvironmentAndPunctual = 5, EnvironmentAndArea = 6, EnvironmentAndAreaAndPunctual = 7, FeatureVariants = 8 };
            public TileDebug tileDebugByCategory;

            public static TileSettings defaultSettings = new TileSettings
            {
                enableTileAndCluster = true,
                enableSplitLightEvaluation = true,
                enableComputeLightEvaluation = false,
                enableComputeFeatureVariants = false,

                tileDebugByCategory = TileDebug.None,
                enableClustered = true,
                enableFptlForOpaqueWhenClustered = true,
                enableBigTilePrepass = true,
            };
        }

        [SerializeField]
        private TileSettings m_TileSettings = TileSettings.defaultSettings;

        public TileSettings tileSettings
        {
            get { return m_TileSettings; }
            set { m_TileSettings = value; }
        }

        [SerializeField]
        private TilePassResources m_PassResources;

        public TilePassResources passResources
        {
            get { return m_PassResources; }
            set { m_PassResources = value; }
        }

        public override BaseLightLoop CreateLightLoop()
        {
            return new LightLoop(this);
        }
    }
}
