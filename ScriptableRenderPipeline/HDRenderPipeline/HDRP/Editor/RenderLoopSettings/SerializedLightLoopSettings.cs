using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    public class SerializedLightLoopSettings
    {
        public SerializedProperty root;

        public SerializedProperty enableTileAndCluster;
        public SerializedProperty enableComputeLightEvaluation;
        public SerializedProperty enableComputeLightVariants;
        public SerializedProperty enableComputeMaterialVariants;
        public SerializedProperty enableFptlForForwardOpaque;
        public SerializedProperty enableBigTilePrepass;
        public SerializedProperty isFptlEnabled;

        public SerializedLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            enableTileAndCluster = root.Find((LightLoopSettings l) => l.enableTileAndCluster);
            enableComputeLightEvaluation = root.Find((LightLoopSettings l) => l.enableComputeLightEvaluation);
            enableComputeLightVariants = root.Find((LightLoopSettings l) => l.enableComputeLightVariants);
            enableComputeMaterialVariants = root.Find((LightLoopSettings l) => l.enableComputeMaterialVariants);
            enableFptlForForwardOpaque = root.Find((LightLoopSettings l) => l.enableFptlForForwardOpaque);
            enableBigTilePrepass = root.Find((LightLoopSettings l) => l.enableBigTilePrepass);
            isFptlEnabled = root.Find((LightLoopSettings l) => l.isFptlEnabled);
        }
    }
}
