#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ShaderVariablesScreenSpaceLighting.cs.hlsl"
#else
    // Rough refraction texture
    // Depth pyramid (width, height, lodcount, Unused)
    TEXTURE2D(_DepthPyramidTexture);
    // Ambient occlusion texture
    TEXTURE2D(_AmbientOcclusionTexture);
    TEXTURE2D(_CameraMotionVectorsTexture);
    TEXTURE2D(_SsrLightingTexture);
#endif
