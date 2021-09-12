namespace UnityEngine.Rendering.HighDefinition
{
    // Global Constant Buffers - b registers. Unity supports a maximum of 16 global constant buffers.
    enum ConstantRegister
    {
        Global = 0,
        XR = 1,
        PBRSky = 2,
        RayTracing = 3,
        RayTracingLightLoop = 4,
    }

    // We need to keep the number of different constant buffers low.
    // Indeed, those are bound for every single drawcall so if we split things in various CB (lightloop, SSS, Fog, etc)
    // We multiply the number of CB we have to bind per drawcall.
    // This is why this CB is big.
    // It should only contain 2 sorts of things:
    // - Global data for a camera (view matrices, RTHandle stuff, etc)
    // - Things that are needed per draw call (like fog or lighting info for forward rendering)
    // Anything else (such as engine passes) can have their own constant buffers (and still use this one as well).

    // PARAMETERS DECLARATION GUIDELINES:
    // All data is aligned on Vector4 size, arrays elements included.
    // - Shader side structure will be padded for anything not aligned to Vector4. Add padding accordingly.
    // - Base element size for array should be 4 components of 4 bytes (Vector4 or Vector4Int basically) otherwise the array will be interlaced with padding on shader side.
    // - In Metal the float3 and float4 are both actually sized and aligned to 16 bytes, whereas for Vulkan/SPIR-V, the alignment is the same. Do not use Vector3!
    // Try to keep data grouped by access and rendering system as much as possible (fog params or light params together for example).
    // => Don't move a float parameter away from where it belongs for filling a hole. Add padding in this case.
    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.Global)]
    unsafe struct ShaderVariablesGlobal
    {
        public const int RenderingLightLayersMask       = 0x000000FF;
        public const int RenderingLightLayersMaskShift  = 0;
        public const int RenderingDecalLayersMask       = 0x0000FF00;
        public const int RenderingDecalLayersMaskShift  = 8;
        public const int DefaultRenderingLayerMask      = 0x0101;       

        // TODO: put commonly used vars together (below), and then sort them by the frequency of use (descending).
        // Note: a matrix is 4 * 4 * 4 = 64 bytes (1x cache line), so no need to sort those.

        // ================================
        //     PER VIEW CONSTANTS
        // ================================
        // TODO: all affine matrices should be 3x4.
        public Matrix4x4 _ViewMatrix;
        public Matrix4x4 _InvViewMatrix;
        public Matrix4x4 _ProjMatrix;
        public Matrix4x4 _InvProjMatrix;
        public Matrix4x4 _ViewProjMatrix;
        public Matrix4x4 _CameraViewProjMatrix;
        public Matrix4x4 _InvViewProjMatrix;
        public Matrix4x4 _NonJitteredViewProjMatrix;
        public Matrix4x4 _PrevViewProjMatrix; // non-jittered
        public Matrix4x4 _PrevInvViewProjMatrix; // non-jittered

#if !USING_STEREO_MATRICES
        public Vector4 _WorldSpaceCameraPos_Internal;
        public Vector4 _PrevCamPosRWS_Internal;
#endif
        public Vector4 _ScreenSize;                 // { w, h, 1 / w, 1 / h }

        // Those two uniforms are specific to the RTHandle system
        public Vector4 _RTHandleScale;        // { w / RTHandle.maxWidth, h / RTHandle.maxHeight } : xy = currFrame, zw = prevFrame
        public Vector4 _RTHandleScaleHistory; // Same as above but the RTHandle handle size is that of the history buffer

        // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
        // x = 1 - f/n
        // y = f/n
        // z = 1/f - 1/n
        // w = 1/n
        // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
        // x = -1 + f/n
        // y = 1
        // z = -1/n + -1/f
        // w = 1/f
        public Vector4 _ZBufferParams;

        // x = 1 or -1 (-1 if projection is flipped)
        // y = near plane
        // z = far plane
        // w = 1/far plane
        public Vector4 _ProjectionParams;

        // x = orthographic camera's width
        // y = orthographic camera's height
        // z = unused
        // w = 1.0 if camera is ortho, 0.0 if perspective
        public Vector4 unity_OrthoParams;

        // x = width
        // y = height
        // z = 1 + 1.0/width
        // w = 1 + 1.0/height
        public Vector4 _ScreenParams;

        [HLSLArray(6, typeof(Vector4))]
        public fixed float _FrustumPlanes[6 * 4]; // { (a, b, c) = N, d = -dot(N, P) } [L, R, T, B, N, F]

        [HLSLArray(6, typeof(Vector4))]
        public fixed float _ShadowFrustumPlanes[6 * 4];     // { (a, b, c) = N, d = -dot(N, P) } [L, R, T, B, N, F]

        // TAA Frame Index ranges from 0 to 7.
        public Vector4 _TaaFrameInfo;               // { taaSharpenStrength, unused, taaFrameIndex, taaEnabled ? 1 : 0 }

        // Current jitter strength (0 if TAA is disabled)
        public Vector4 _TaaJitterStrength;          // { x, y, x/width, y/height }

        // t = animateMaterials ? Time.realtimeSinceStartup : 0.
        // We keep all those time value for compatibility with legacy unity but prefer _TimeParameters instead.
        public Vector4 _Time;                       // { t/20, t, t*2, t*3 }
        public Vector4 _SinTime;                    // { sin(t/8), sin(t/4), sin(t/2), sin(t) }
        public Vector4 _CosTime;                    // { cos(t/8), cos(t/4), cos(t/2), cos(t) }
        public Vector4 unity_DeltaTime;             // { dt, 1/dt, smoothdt, 1/smoothdt }
        public Vector4 _TimeParameters;             // { t, sin(t), cos(t) }
        public Vector4 _LastTimeParameters;         // { t, sin(t), cos(t) }

        // Volumetric lighting / Fog.
        public int      _FogEnabled;
        public int      _PBRFogEnabled;
        public int      _EnableVolumetricFog;
        public float    _MaxFogDistance;
        public Vector4  _FogColor; // color in rgb
        public float    _FogColorMode;
        public float    _Pad0;
        public float    _Pad1;
        public float    _Pad2;
        public Vector4  _MipFogParameters;
        public Vector4  _HeightFogBaseScattering;
        public float    _HeightFogBaseExtinction;
        public float    _HeightFogBaseHeight;
        public float    _GlobalFogAnisotropy;
        public int      _VolumetricFilteringEnabled;
        public Vector2  _HeightFogExponents; // { 1/H, H }
        public float    _Pad4;
        public float    _Pad5;

        // VBuffer
        public Vector4  _VBufferViewportSize;           // { w, h, 1/w, 1/h }
        public Vector4  _VBufferLightingViewportScale;  // Necessary us to work with sub-allocation (resource aliasing) in the RTHandle system
        public Vector4  _VBufferLightingViewportLimit;  // Necessary us to work with sub-allocation (resource aliasing) in the RTHandle system
        public Vector4  _VBufferDistanceEncodingParams; // See the call site for description
        public Vector4  _VBufferDistanceDecodingParams; // See the call site for description
        public uint     _VBufferSliceCount;
        public float    _VBufferRcpSliceCount;
        public float    _VBufferRcpInstancedViewCount;  // Used to remap VBuffer coordinates for XR
        public float    _VBufferLastSliceDist;          // The distance to the middle of the last slice

        // Light Loop
        public const int s_MaxEnv2DLight = 32;

        public Vector4 _ShadowAtlasSize;
        public Vector4 _CascadeShadowAtlasSize;
        public Vector4 _AreaShadowAtlasSize;
        public Vector4 _CachedShadowAtlasSize;
        public Vector4 _CachedAreaShadowAtlasSize;

        [HLSLArray(s_MaxEnv2DLight, typeof(Matrix4x4))]
        public fixed float _Env2DCaptureVP[s_MaxEnv2DLight * 4 * 4];
        [HLSLArray(s_MaxEnv2DLight, typeof(Vector4))]
        public fixed float _Env2DCaptureForward[s_MaxEnv2DLight * 4];
        [HLSLArray(s_MaxEnv2DLight, typeof(Vector4))]
        public fixed float _Env2DAtlasScaleOffset[s_MaxEnv2DLight * 4];

        public uint     _DirectionalLightCount;
        public uint     _PunctualLightCount;
        public uint     _AreaLightCount;
        public uint     _EnvLightCount;

        public int      _EnvLightSkyEnabled;
        public uint     _CascadeShadowCount;
        public int      _DirectionalShadowIndex;
        public uint     _EnableLightLayers;

        public uint     _EnableSkyReflection;
        public uint     _EnableSSRefraction;
        public float    _SSRefractionInvScreenWeightDistance; // Distance for screen space smoothstep with fallback
        public float    _ColorPyramidLodCount;

        public float    _DirectionalTransmissionMultiplier;
        public float    _ProbeExposureScale;
        public float    _ContactShadowOpacity;
        public float    _ReplaceDiffuseForIndirect;

        public Vector4 _AmbientOcclusionParam; // xyz occlusion color, w directLightStrenght

        public float _IndirectDiffuseLightingMultiplier;
        public uint _IndirectDiffuseLightingLayers;
        public float _ReflectionLightingMultiplier;
        public uint _ReflectionLightingLayers;

        public float _MicroShadowOpacity;
        public uint _EnableProbeVolumes;
        public uint _ProbeVolumeCount;
        public float _SlopeScaleDepthBias;

        public Vector4  _CookieAtlasSize;
        public Vector4  _CookieAtlasData;
        public Vector4  _PlanarAtlasData;

        // Tile/Cluster
        public uint     _NumTileFtplX;
        public uint     _NumTileFtplY;
        public float    g_fClustScale;
        public float    g_fClustBase;

        public float    g_fNearPlane;
        public float    g_fFarPlane;
        public int      g_iLog2NumClusters; // We need to always define these to keep constant buffer layouts compatible
        public uint     g_isLogBaseBufferEnabled;

        public uint     _NumTileClusteredX;
        public uint     _NumTileClusteredY;
        public int      _EnvSliceSize;
        public float    _Pad7;

        // Subsurface scattering
        // Use float4 to avoid any packing issue between compute and pixel shaders
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _ShapeParamsAndMaxScatterDists[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];   // RGB = S = 1 / D, A = d = RgbMax(D)
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _TransmissionTintsAndFresnel0[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];  // RGB = 1/4 * color, A = fresnel0
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _WorldScalesAndFilterRadiiAndThicknessRemaps[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4]; // X = meters per world unit, Y = filter radius (in mm), Z = remap start, W = end - start
        // Because of constant buffer limitation, arrays can only hold 4 components elements (otherwise we get alignment issues)
        // We could pack the 16 values inside 4 uint4 but then the generated code is inefficient and generates a lots of swizzle operations instead of a single load.
        // That's why we have 16 uint and only use the first component of each element.
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(ShaderGenUInt4))]
        public fixed uint _DiffusionProfileHashTable[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4]; // TODO: constant

        public uint _EnableSubsurfaceScattering; // Globally toggles subsurface and transmission scattering on/off
        public uint _TexturingModeFlags;         // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        public uint _TransmissionFlags;          // 1 bit/profile; 0 = regular, 1 = thin
        public uint _DiffusionProfileCount;

        // Decals
        public Vector2  _DecalAtlasResolution;
        public uint     _EnableDecals;
        public uint     _DecalCount;

        public uint _OffScreenRendering;
        public uint _OffScreenDownsampleFactor;
        public uint _XRViewCount;
        public int  _FrameCount;

        public Vector4 _CoarseStencilBufferSize;

        public int      _IndirectDiffuseMode; // Match IndirectDiffuseMode enum in LightLoop.cs
        public int      _EnableRayTracedReflections;
        public int      _RaytracingFrameIndex;  // Index of the current frame [0, 7]
        public uint     _EnableRecursiveRayTracing;

        // Probe Volumes
        public Vector4  _ProbeVolumeAtlasResolutionAndSliceCount;
        public Vector4  _ProbeVolumeAtlasResolutionAndSliceCountInverse;
        public Vector4  _ProbeVolumeAtlasOctahedralDepthResolutionAndInverse;

        public int      _ProbeVolumeLeakMitigationMode;
        public float    _ProbeVolumeBilateralFilterWeightMin;
        public float    _ProbeVolumeBilateralFilterWeight;
        public uint     _EnableDecalLayers;

        [HLSLArray(7, typeof(Vector4))]
        public fixed float _ProbeVolumeAmbientProbeFallbackPackedCoeffs[7 * 4]; // 3 bands of SH, packed for storing global ambient probe lighting as fallback to probe volumes.

        public int      _TransparentCameraOnlyMotionVectors;

        // Can be set to 0 to globally "disable" tessellation
        // Because the DepthPrepass doesn't have a DEBUG_DISPLAY variant, it is the only way to disable it for debug modes
        public float    _GlobalTessellationFactorMultiplier;

        public float    _SpecularOcclusionBlend;
        public float    _Pad9;
    }
}
