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
            public bool enableDrawLightBoundsDebug;
            public bool disableTileAndCluster; // For debug / test
            public bool disableDeferredShadingInCompute;
            public bool enableSplitLightEvaluation;
            public bool enableComputeLightEvaluation;

            // clustered light list specific buffers and data begin
            public int debugViewTilesFlags;
            public bool enableClustered;
            public bool disableFptlWhenClustered; // still useful on opaques. Should be false by default to force tile on opaque.
            public bool enableBigTilePrepass;

            public static TileSettings defaultSettings = new TileSettings
            {
                enableDrawLightBoundsDebug = false,
                disableTileAndCluster = false,
                disableDeferredShadingInCompute = true,
                enableSplitLightEvaluation = true,
                enableComputeLightEvaluation = false,

                debugViewTilesFlags = 0,
                enableClustered = true,
                disableFptlWhenClustered = false,
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
