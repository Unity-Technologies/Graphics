using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class LightLoopSettings
    {
        // Setup by the users
        public bool enableTileAndCluster = true;
        public bool enableComputeLightEvaluation = true;
        public bool enableComputeLightVariants = true;
        public bool enableComputeMaterialVariants = true;
        // Deferred opaque always use FPTL, forward opaque can use FPTL or cluster, transparent always use cluster
        // When MSAA is enabled, we only support cluster (Fptl is too slow with MSAA), and we don't support MSAA for deferred path (mean it is ok to keep fptl)
        public bool enableFptlForForwardOpaque = true;
        public bool enableBigTilePrepass = true;

        // Setup by system
        public bool isFptlEnabled = true;

        static DebugUI.Widget[] s_DebugEntries;

        public void CopyTo(LightLoopSettings lightLoopSettings)
        {
            lightLoopSettings.enableTileAndCluster          = this.enableTileAndCluster;
            lightLoopSettings.enableComputeLightEvaluation  = this.enableComputeLightEvaluation;
            lightLoopSettings.enableComputeLightVariants    = this.enableComputeLightVariants;
            lightLoopSettings.enableComputeMaterialVariants = this.enableComputeMaterialVariants;

            lightLoopSettings.enableFptlForForwardOpaque    = this.enableFptlForForwardOpaque;
            lightLoopSettings.enableBigTilePrepass          = this.enableBigTilePrepass;

            lightLoopSettings.isFptlEnabled                 = this.isFptlEnabled;
        }

        // aggregateFrameSettings already contain the aggregation of RenderPipelineSettings and FrameSettings (regular and/or debug)
        public static void InitializeLightLoopSettings(Camera camera, FrameSettings aggregateFrameSettings,
                                                                    RenderPipelineSettings renderPipelineSettings, FrameSettings frameSettings,
                                                                    ref LightLoopSettings aggregate)
        {
            if (aggregate == null)
                aggregate = new LightLoopSettings();

            aggregate.enableTileAndCluster          = frameSettings.lightLoopSettings.enableTileAndCluster;
            aggregate.enableComputeLightEvaluation  = frameSettings.lightLoopSettings.enableComputeLightEvaluation;
            aggregate.enableComputeLightVariants    = frameSettings.lightLoopSettings.enableComputeLightVariants;
            aggregate.enableComputeMaterialVariants = frameSettings.lightLoopSettings.enableComputeMaterialVariants;
            aggregate.enableFptlForForwardOpaque    = frameSettings.lightLoopSettings.enableFptlForForwardOpaque;
            aggregate.enableBigTilePrepass          = frameSettings.lightLoopSettings.enableBigTilePrepass;

            // Deferred opaque are always using Fptl. Forward opaque can use Fptl or Cluster, transparent use cluster.
            // When MSAA is enabled we disable Fptl as it become expensive compare to cluster
            // In HD, MSAA is only supported for forward only rendering, no MSAA in deferred mode (for code complexity reasons)
            aggregate.enableFptlForForwardOpaque = aggregate.enableFptlForForwardOpaque && !aggregateFrameSettings.enableMSAA;

            // disable FPTL for stereo for now
            aggregate.enableFptlForForwardOpaque = aggregate.enableFptlForForwardOpaque && !aggregateFrameSettings.enableStereo;

            // If Deferred, enable Fptl. If we are forward renderer only and not using Fptl for forward opaque, disable Fptl
            aggregate.isFptlEnabled = !aggregateFrameSettings.enableForwardRenderingOnly || aggregate.enableFptlForForwardOpaque;
        }

        public static void RegisterDebug(string menuName, LightLoopSettings lightLoopSettings)
        {
            s_DebugEntries = new DebugUI.Widget[]
            {
                new DebugUI.BoolField { displayName = "Enable Fptl for Forward Opaque", getter = () => lightLoopSettings.enableFptlForForwardOpaque, setter = value => lightLoopSettings.enableFptlForForwardOpaque = value },
                new DebugUI.BoolField { displayName = "Enable Tile/Cluster", getter = () => lightLoopSettings.enableTileAndCluster, setter = value => lightLoopSettings.enableTileAndCluster = value },
                new DebugUI.BoolField { displayName = "Enable Big Tile", getter = () => lightLoopSettings.enableBigTilePrepass, setter = value => lightLoopSettings.enableBigTilePrepass = value },
                new DebugUI.BoolField { displayName = "Enable Compute Lighting", getter = () => lightLoopSettings.enableComputeLightEvaluation, setter = value => lightLoopSettings.enableComputeLightEvaluation = value },
                new DebugUI.BoolField { displayName = "Enable Light Classification", getter = () => lightLoopSettings.enableComputeLightVariants, setter = value => lightLoopSettings.enableComputeLightVariants = value },
                new DebugUI.BoolField { displayName = "Enable Material Classification", getter = () => lightLoopSettings.enableComputeMaterialVariants, setter = value => lightLoopSettings.enableComputeMaterialVariants = value }
            };

            var panel = DebugManager.instance.GetPanel(menuName, true);
            panel.children.Add(s_DebugEntries);
        }

        public static void UnRegisterDebug(string menuName)
        {
            var panel = DebugManager.instance.GetPanel(menuName);

            if (panel != null)
                panel.children.Remove(s_DebugEntries);
        }
    }
}
