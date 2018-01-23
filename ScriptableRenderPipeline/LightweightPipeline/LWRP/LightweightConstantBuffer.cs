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

    public static class ShadowConstantBuffer
    {
        public static int _WorldToShadow;
        public static int _ShadowData;
        public static int _DirShadowSplitSpheres;
        public static int _DirShadowSplitSphereRadii;
        public static int _ShadowOffset0;
        public static int _ShadowOffset1;
        public static int _ShadowOffset2;
        public static int _ShadowOffset3;
        public static int _ShadowmapSize;
    }
}
