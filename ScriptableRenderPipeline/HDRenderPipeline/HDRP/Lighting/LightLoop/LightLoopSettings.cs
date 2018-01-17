using System;
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class LightLoopSettings
    {
        public static string kEnableTileCluster = "Enable Tile/Cluster";
        public static string kEnableBigTile = "Enable Big Tile";
        public static string kEnableComputeLighting = "Enable Compute Lighting";
        public static string kEnableLightclassification = "Enable Light Classification";
        public static string kEnableMaterialClassification = "Enable Material Classification";

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
            aggregate.enableFptlForForwardOpaque = aggregate.enableFptlForForwardOpaque && aggregateFrameSettings.enableMSAA;
            // If Deferred, enable Fptl. If we are forward renderer only and not using Fptl for forward opaque, disable Fptl
            aggregate.isFptlEnabled = !aggregateFrameSettings.enableForwardRenderingOnly || aggregate.enableFptlForForwardOpaque;
        }

        static public void RegisterDebug(String menuName, LightLoopSettings lightLoopSettings)
        {
            DebugMenuManager.instance.AddDebugItem<bool>(menuName, kEnableTileCluster, () => lightLoopSettings.enableTileAndCluster, (value) => lightLoopSettings.enableTileAndCluster = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>(menuName, kEnableBigTile, () => lightLoopSettings.enableBigTilePrepass, (value) => lightLoopSettings.enableBigTilePrepass = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>(menuName, kEnableComputeLighting, () => lightLoopSettings.enableComputeLightEvaluation, (value) => lightLoopSettings.enableComputeLightEvaluation = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>(menuName, kEnableLightclassification, () => lightLoopSettings.enableComputeLightVariants, (value) => lightLoopSettings.enableComputeLightVariants = (bool)value);
            DebugMenuManager.instance.AddDebugItem<bool>(menuName, kEnableMaterialClassification, () => lightLoopSettings.enableComputeMaterialVariants, (value) => lightLoopSettings.enableComputeMaterialVariants = (bool)value);
        }

        static public void UnRegisterDebug(String menuName)
        {
            DebugMenuManager.instance.RemoveDebugItem(menuName, kEnableTileCluster);
            DebugMenuManager.instance.RemoveDebugItem(menuName, kEnableBigTile);
            DebugMenuManager.instance.RemoveDebugItem(menuName, kEnableComputeLighting);
            DebugMenuManager.instance.RemoveDebugItem(menuName, kEnableLightclassification);
            DebugMenuManager.instance.RemoveDebugItem(menuName, kEnableMaterialClassification);
        }
    }
}
