namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class LightLoopSettings
    {
        // Setup by the users
        public bool enableTileAndCluster;
        public bool enableComputeLightEvaluation;
        public bool enableComputeLightVariants;
        public bool enableComputeMaterialVariants;
        // Deferred opaque always use FPTL, forward opaque can use FPTL or cluster, transparent always use cluster
        // When MSAA is enabled, we only support cluster (Fptl is too slow with MSAA), and we don't support MSAA for deferred path (mean it is ok to keep fptl)
        public bool enableFptlForForwardOpaque;
        public bool enableBigTilePrepass;

        // Setup by system
        public bool isFTPLEnabled;

        public LightLoopSettings()
        {
            enableTileAndCluster = true;
            enableComputeLightEvaluation = true;
            enableComputeLightVariants = true;
            enableComputeMaterialVariants = true;

            enableFptlForForwardOpaque = true;
            enableBigTilePrepass = true;

            isFTPLEnabled = true;
        }

        // aggregateFrameSettings already contain the aggregation of RenderPipelineSettings and FrameSettings (regular and/or debug)
        static public LightLoopSettings InitializeLightLoopSettings(FrameSettings aggregateFrameSettings, RenderPipelineSettings renderPipelineSettings, FrameSettings frameSettings, FrameSettings debugSettings = null)
        {
            LightLoopSettings aggregate;

            aggregate.enableTileAndCluster          = frameSettings.enableTileAndCluster;
            aggregate.enableComputeLightEvaluation  = frameSettings.enableComputeLightEvaluation;
            aggregate.enableComputeLightVariants    = frameSettings.enableComputeLightVariants;
            aggregate.enableComputeMaterialVariants = frameSettings.enableComputeMaterialVariants;
            aggregate.enableFptlForForwardOpaque    = frameSettings.enableFptlForForwardOpaque;
            aggregate.enableBigTilePrepass          = frameSettings.enableBigTilePrepass;

            // Don't take into account debug settings for reflection probe or preview
            if (debugSettings != null && !camera.cameraType == CameraType.Reflection && camera.cameraType != CameraType.Preview)
            {
                aggregate.enableTileAndCluster          = aggregate.enableTileAndCluster && debugSettings.enableTileAndCluster;
                aggregate.enableComputeLightEvaluation  = aggregate.enableComputeLightEvaluation && debugSettings.enableComputeLightEvaluation;
                aggregate.enableComputeLightVariants    = aggregate.enableComputeLightVariants && debugSettings.enableComputeLightVariants;
                aggregate.enableComputeMaterialVariants = aggregate.enableComputeMaterialVariants && debugSettings.enableComputeMaterialVariants;
                aggregate.enableFptlForForwardOpaque    = aggregate.enableFptlForForwardOpaque && debugSettings.enableFptlForForwardOpaque;
                aggregate.enableBigTilePrepass          = aggregate.enableBigTilePrepass && debugSettings.enableBigTilePrepass;
            }

            // Deferred opaque are always using Fptl. Forward opaque can use Fptl or Cluster, transparent use cluster.
            // When MSAA is enabled we disable Fptl as it become expensive compare to cluster
            // In HD, MSAA is only supported for forward only rendering, no MSAA in deferred mode (for code complexity reasons)
            aggregate.enableFptlForForwardOpaque = aggregate.enableFptlForForwardOpaque && aggregateFrameSettings.enableMSAA;
            // If Deferred, enable Fptl. If we are forward renderer only and not using Fptl for forward opaque, disable Fptl
            aggregate.isFTPLEnabled = !aggregateFrameSettings.enableForwardRenderingOnly || aggregate.enableFptlForForwardOpaque;

            return aggregate;
        }
    }
}
