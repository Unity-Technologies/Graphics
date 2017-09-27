using UnityEngine;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public static class PerFrameBuffer
    {
        public static int _GlossyEnvironmentColor;
        public static int _AttenuationTexture;
    }

    public static class PerCameraBuffer
    {
        public static int _MainLightPosition;
        public static int _MainLightColor;
        public static int _MainLightAttenuationParams;
        public static int _MainLightSpotDir;

        public static int _AdditionalLightCount;
        public static int _AdditionalLightPosition;
        public static int _AdditionalLightColor;
        public static int _AdditionalLightAttenuationParams;
        public static int _AdditionalLightSpotDir;
    }
}