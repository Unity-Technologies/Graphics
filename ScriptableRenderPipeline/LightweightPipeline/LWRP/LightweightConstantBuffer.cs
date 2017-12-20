using UnityEngine;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public static class PerFrameBuffer
    {
        public static int _GlossyEnvironmentColor;
        public static int _SubtractiveShadowColor;
    }

    public static class PerCameraBuffer
    {
        public static int _MainLightPosition;
        public static int _MainLightColor;
        public static int _MainLightDistanceAttenuation;
        public static int _MainLightSpotDir;
        public static int _MainLightSpotAttenuation;
        public static int _MainLightCookie;
        public static int _WorldToLight;

        public static int _AdditionalLightCount;
        public static int _AdditionalLightPosition;
        public static int _AdditionalLightColor;
        public static int _AdditionalLightDistanceAttenuation;
        public static int _AdditionalLightSpotDir;
        public static int _AdditionalLightSpotAttenuation;
    }
}
