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
        public static int _MainLightCookie;
        public static int _WorldToLight;

        public static int _AdditionalLightCount;
        public static int _AdditionalLightPosition;
        public static int _AdditionalLightColor;
        public static int _AdditionalLightDistanceAttenuation;
        public static int _AdditionalLightSpotDir;
        public static int _AdditionalLightSpotAttenuation;

        public static int _LightIndexBuffer;

        public static int _ScaledScreenParams;
    }

    public static class DirectionalShadowConstantBuffer
    {
        public static int _WorldToShadow;
        public static int _ShadowData;
        public static int _DirShadowSplitSpheres0;
        public static int _DirShadowSplitSpheres1;
        public static int _DirShadowSplitSpheres2;
        public static int _DirShadowSplitSpheres3;
        public static int _DirShadowSplitSphereRadii;
        public static int _ShadowOffset0;
        public static int _ShadowOffset1;
        public static int _ShadowOffset2;
        public static int _ShadowOffset3;
        public static int _ShadowmapSize;
    }

    public static class LocalShadowConstantBuffer
    {
        public static int _LocalWorldToShadowAtlas;
        public static int _LocalShadowStrength;
        public static int _LocalShadowOffset0;
        public static int _LocalShadowOffset1;
        public static int _LocalShadowOffset2;
        public static int _LocalShadowOffset3;
        public static int _LocalShadowmapSize;
    }
}
