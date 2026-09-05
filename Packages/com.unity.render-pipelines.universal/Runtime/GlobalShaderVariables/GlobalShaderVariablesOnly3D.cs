using System;
using System.Runtime.InteropServices;
using Dirty = UnityEngine.Rendering.Universal.GlobalShaderVariablesOnly3DDirty;

namespace UnityEngine.Rendering.Universal
{
    // One bit per push performed by SetGlobals, see GlobalShaderVariablesBaseDirty.
    // Warning! Add a bit here, an accessor in GlobalShaderVariablesUploader and a line in SetGlobals() for every new field.
    [Flags]
    internal enum GlobalShaderVariablesOnly3DDirty : uint
    {
        None = 0u,

        // Main Light
        _MainLightWorldToLight = 1u << 0,
        _MainLightPosition = 1u << 1,
        _MainLightColor = 1u << 2,
        _MainLightOcclusionProbes = 1u << 3,
        _MainLightLayerMask = 1u << 4,

        // Forward+ Parameters
        _FPParams0 = 1u << 5,
        _FPParams1 = 1u << 6,
        _FPParams2 = 1u << 7,

        // Main Light Shadows. The five cascade matrices share one bit: they go out as a single SetGlobalMatrixArray, so
        // there is nothing to gain from tracking them apart.
        _MainLightWorldToShadow = 1u << 8,
        _CascadeShadowSplitSpheres0 = 1u << 9,
        _CascadeShadowSplitSpheres1 = 1u << 10,
        _CascadeShadowSplitSpheres2 = 1u << 11,
        _CascadeShadowSplitSpheres3 = 1u << 12,
        _CascadeShadowSplitSphereRadii = 1u << 13,
        _MainLightShadowOffset0 = 1u << 14,
        _MainLightShadowOffset1 = 1u << 15,
        _MainLightShadowmapSize = 1u << 16,

        // Additional Lights Shadows
        _AdditionalShadowOffset0 = 1u << 17,
        _AdditionalShadowOffset1 = 1u << 18,
        _AdditionalShadowmapSize = 1u << 19,

        // Misc
        _MainLightCookieTextureFormat = 1u << 20,
        _AdditionalLightsCookieAtlasTextureFormat = 1u << 21,
        _RenderingLayerMaxInt = 1u << 22,
        _EnableProbeVolumes = 1u << 23,

        // Additional Lights
        _AdditionalLightsCount = 1u << 24,

        // Motion Vectors. The Stereo variants stay loose uniforms, see GlobalShaderVariables.hlsl.
        _NonJitteredViewProjMatrix = 1u << 25,
        _PrevViewProjMatrix = 1u << 26,

        // Keep this referencing the last bit declared above when adding a variable.
        All = (_PrevViewProjMatrix << 1) - 1u,
    }

    /// <summary>
    /// The main light world to shadow matrices, one per cascade plus a trailing no-op slice.
    /// </summary>
    /// <remarks>
    /// Grouped in a nested struct rather than declared as five fields so the dirty flag contract stays one bit per push:
    /// they go out as a single SetGlobalMatrixArray.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MainLightShadowMatrices
    {
        /// <summary> The number of matrices, MAX_SHADOW_CASCADES + 1 in Shadows.hlsl. </summary>
        public const int count = 5;

        /// <summary>The matrix of the first cascade.</summary>
        public Matrix4x4 cascade0;

        /// <summary>The matrix of the second cascade.</summary>
        public Matrix4x4 cascade1;

        /// <summary>The matrix of the third cascade.</summary>
        public Matrix4x4 cascade2;

        /// <summary>The matrix of the fourth cascade.</summary>
        public Matrix4x4 cascade3;

        /// <summary>
        /// The trailing no-op matrix. It always transforms the shadow coord to half3(0, 0, NEAR_PLANE), which avoids
        /// branching since ComputeCascadeIndex can return MAX_SHADOW_CASCADES.
        /// </summary>
        public Matrix4x4 cascade4;

        /// <summary>
        /// Pushes the matrices as a single global matrix array, without copying them to a scratch array first.
        /// </summary>
        /// <param name="cmd">The command buffer used to set the global.</param>
        /// <param name="nameID">The shader property id of the array.</param>
        public readonly unsafe void SetGlobal(IBaseCommandBuffer cmd, int nameID)
        {
            fixed (Matrix4x4* matrices = &cascade0)
                cmd.SetGlobalMatrixArray(nameID, new ReadOnlySpan<Matrix4x4>(matrices, count));
        }

        /// <summary>Gets or sets the matrix of a given cascade.</summary>
        /// <param name="index">The cascade index, between 0 and <see cref="count"/> - 1.</param>
        /// <returns>The matrix stored for the requested cascade.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
        public Matrix4x4 this[int index]
        {
            readonly get => index switch
            {
                0 => cascade0,
                1 => cascade1,
                2 => cascade2,
                3 => cascade3,
                4 => cascade4,
                _ => throw new IndexOutOfRangeException($"index {index} is out of range, it must be between 0 and {count - 1}."),
            };
            set
            {
                switch (index)
                {
                    case 0: cascade0 = value; break;
                    case 1: cascade1 = value; break;
                    case 2: cascade2 = value; break;
                    case 3: cascade3 = value; break;
                    case 4: cascade4 = value; break;
                    default: throw new IndexOutOfRangeException($"index {index} is out of range, it must be between 0 and {count - 1}.");
                }
            }
        }
    }

    // Global shader variables only URP 3D declares and fills, see GlobalShaderVariables for the declaration guidelines.
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlobalShaderVariablesOnly3D
    {
        // Main Light
        public Matrix4x4 _MainLightWorldToLight; // LightCookieInput.hlsl
        public Vector4 _MainLightPosition; // Forward + Deferred Light
        public Vector4 _MainLightColor; // Forward + Deferred Light (half precision in shader)
        public Vector4 _MainLightOcclusionProbes; // Forward Light (half precision in shader)

        // Forward+ Parameters
        public Vector4 _FPParams0; // Input.hlsl + USE_CLUSTER_LIGHT_LOOP
        public Vector4 _FPParams1; // Input.hlsl + USE_CLUSTER_LIGHT_LOOP
        public Vector4 _FPParams2; // Input.hlsl + USE_CLUSTER_LIGHT_LOOP

        // Motion Vectors. In XR single pass the shader macros redirect every read to the Stereo arrays, which stay
        // loose uniforms, so these two are only consumed outside XR. See UnityInput.hlsl.
        public Matrix4x4 _NonJitteredViewProjMatrix; // UnityInput.hlsl
        public Matrix4x4 _PrevViewProjMatrix; // UnityInput.hlsl

        // Additional Lights. The arrays themselves are in the AdditionalLights CBuffer.
        public Vector4 _AdditionalLightsCount; // Input.hlsl (half precision in shader), x holds the count

        // Main Light Shadows
        public MainLightShadowMatrices _MainLightWorldToShadow; // GlobalShaderVariables.hlsl
        public Vector4 _CascadeShadowSplitSpheres0; // Shadows.hlsl
        public Vector4 _CascadeShadowSplitSpheres1; // Shadows.hlsl
        public Vector4 _CascadeShadowSplitSpheres2; // Shadows.hlsl
        public Vector4 _CascadeShadowSplitSpheres3; // Shadows.hlsl
        public Vector4 _CascadeShadowSplitSphereRadii; // Shadows.hlsl
        public Vector4 _MainLightShadowOffset0; // Shadows.hlsl
        public Vector4 _MainLightShadowOffset1; // Shadows.hlsl
        public Vector4 _MainLightShadowmapSize; // Shadows.hlsl

        // Additional Lights Shadows
        public Vector4 _AdditionalShadowOffset0; // Shadows.hlsl
        public Vector4 _AdditionalShadowOffset1; // Shadows.hlsl
        public Vector4 _AdditionalShadowmapSize; // Shadows.hlsl

        // Misc
        public float _MainLightCookieTextureFormat; // LightCookieInput.hlsl
        public float _AdditionalLightsCookieAtlasTextureFormat; // LightCookieInput.hlsl
        public uint _RenderingLayerMaxInt; // Unity.hlsl
        public uint _EnableProbeVolumes; // Forward Light

        // Kept last on purpose: these scalars share a single 16 byte row, so the next 3D fields we add can consume the
        // remaining padding slots instead of needing padding of their own.
        public uint _MainLightLayerMask; // Forward + Deferred Light
        public uint _URPPadding2;
        public uint _URPPadding3;
        public uint _URPPadding4;

        // _ScaleBiasRt is not here on purpose: DrawObjectsPass and RenderingUtils.SetScaleBiasRt derive it from the
        // render target of the pass, so it stays per-pass state until those call sites are converted.
    }

    internal static class GlobalShaderVariablesOnly3DExtensions
    {
        /// <summary>
        /// Sets the URP 3D shader variables flagged in <paramref name="dirty"/> as global uniforms using the provided
        /// CommandBuffer. This is a fallback when persistent constant buffer mode is disabled.
        /// Extension with `in` receiver so callers can invoke it without copying the struct.
        /// </summary>
        internal static void SetGlobals(this in GlobalShaderVariablesOnly3D vars, IBaseCommandBuffer cmd, Dirty dirty)
        {
            if (dirty == Dirty.None)
                return;

            // Main Light
            if ((dirty & Dirty._MainLightWorldToLight) != 0) cmd.SetGlobalMatrix(PropertyId.mainLightWorldToLight, vars._MainLightWorldToLight);
            if ((dirty & Dirty._MainLightPosition) != 0) cmd.SetGlobalVector(PropertyId.mainLightPosition, vars._MainLightPosition);
            if ((dirty & Dirty._MainLightColor) != 0) cmd.SetGlobalVector(PropertyId.mainLightColor, vars._MainLightColor);
            if ((dirty & Dirty._MainLightOcclusionProbes) != 0) cmd.SetGlobalVector(PropertyId.mainLightOcclusionProbes, vars._MainLightOcclusionProbes);
            if ((dirty & Dirty._MainLightLayerMask) != 0) cmd.SetGlobalInt(PropertyId.mainLightLayerMask, (int)vars._MainLightLayerMask);

            // Forward+ Parameters
            if ((dirty & Dirty._FPParams0) != 0) cmd.SetGlobalVector(PropertyId.fpParams0, vars._FPParams0);
            if ((dirty & Dirty._FPParams1) != 0) cmd.SetGlobalVector(PropertyId.fpParams1, vars._FPParams1);
            if ((dirty & Dirty._FPParams2) != 0) cmd.SetGlobalVector(PropertyId.fpParams2, vars._FPParams2);

            // Motion Vectors
            if ((dirty & Dirty._NonJitteredViewProjMatrix) != 0) cmd.SetGlobalMatrix(ShaderPropertyId.viewProjectionNoJitter, vars._NonJitteredViewProjMatrix);
            if ((dirty & Dirty._PrevViewProjMatrix) != 0) cmd.SetGlobalMatrix(ShaderPropertyId.previousViewProjectionNoJitter, vars._PrevViewProjMatrix);

            // Additional Lights
            if ((dirty & Dirty._AdditionalLightsCount) != 0) cmd.SetGlobalVector(PropertyId.additionalLightsCount, vars._AdditionalLightsCount);

            // Main Light Shadows
            if ((dirty & Dirty._MainLightWorldToShadow) != 0) vars._MainLightWorldToShadow.SetGlobal(cmd, PropertyId.mainLightWorldToShadow);
            if ((dirty & Dirty._CascadeShadowSplitSpheres0) != 0) cmd.SetGlobalVector(PropertyId.cascadeShadowSplitSpheres0, vars._CascadeShadowSplitSpheres0);
            if ((dirty & Dirty._CascadeShadowSplitSpheres1) != 0) cmd.SetGlobalVector(PropertyId.cascadeShadowSplitSpheres1, vars._CascadeShadowSplitSpheres1);
            if ((dirty & Dirty._CascadeShadowSplitSpheres2) != 0) cmd.SetGlobalVector(PropertyId.cascadeShadowSplitSpheres2, vars._CascadeShadowSplitSpheres2);
            if ((dirty & Dirty._CascadeShadowSplitSpheres3) != 0) cmd.SetGlobalVector(PropertyId.cascadeShadowSplitSpheres3, vars._CascadeShadowSplitSpheres3);
            if ((dirty & Dirty._CascadeShadowSplitSphereRadii) != 0) cmd.SetGlobalVector(PropertyId.cascadeShadowSplitSphereRadii, vars._CascadeShadowSplitSphereRadii);
            if ((dirty & Dirty._MainLightShadowOffset0) != 0) cmd.SetGlobalVector(PropertyId.mainLightShadowOffset0, vars._MainLightShadowOffset0);
            if ((dirty & Dirty._MainLightShadowOffset1) != 0) cmd.SetGlobalVector(PropertyId.mainLightShadowOffset1, vars._MainLightShadowOffset1);
            if ((dirty & Dirty._MainLightShadowmapSize) != 0) cmd.SetGlobalVector(PropertyId.mainLightShadowmapSize, vars._MainLightShadowmapSize);

            // Additional Lights Shadows
            if ((dirty & Dirty._AdditionalShadowOffset0) != 0) cmd.SetGlobalVector(PropertyId.additionalShadowOffset0, vars._AdditionalShadowOffset0);
            if ((dirty & Dirty._AdditionalShadowOffset1) != 0) cmd.SetGlobalVector(PropertyId.additionalShadowOffset1, vars._AdditionalShadowOffset1);
            if ((dirty & Dirty._AdditionalShadowmapSize) != 0) cmd.SetGlobalVector(PropertyId.additionalShadowmapSize, vars._AdditionalShadowmapSize);

            // Misc
            if ((dirty & Dirty._MainLightCookieTextureFormat) != 0) cmd.SetGlobalFloat(PropertyId.mainLightCookieTextureFormat, vars._MainLightCookieTextureFormat);
            if ((dirty & Dirty._AdditionalLightsCookieAtlasTextureFormat) != 0) cmd.SetGlobalFloat(PropertyId.additionalLightsCookieAtlasTextureFormat, vars._AdditionalLightsCookieAtlasTextureFormat);
            if ((dirty & Dirty._RenderingLayerMaxInt) != 0) cmd.SetGlobalInt(ShaderPropertyId.renderingLayerMaxInt, (int)vars._RenderingLayerMaxInt);
            if ((dirty & Dirty._EnableProbeVolumes) != 0) cmd.SetGlobalInt(PropertyId.enableProbeVolumes, (int)vars._EnableProbeVolumes);
        }

        // Private on purpose: a raw SetGlobal with one of these ids would bypass the staging dirty flags, and in
        // persistent constant buffer mode the next push would overwrite it anyway. Values reach the shader through
        // the GlobalShaderVariablesUploader accessors and nothing else.
        private static class PropertyId
        {
            // Main Light
            public static readonly int mainLightWorldToLight = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int mainLightPosition = Shader.PropertyToID("_MainLightPosition");
            public static readonly int mainLightColor = Shader.PropertyToID("_MainLightColor");
            public static readonly int mainLightOcclusionProbes = Shader.PropertyToID("_MainLightOcclusionProbes");
            public static readonly int mainLightLayerMask = Shader.PropertyToID("_MainLightLayerMask");

            // Forward+
            public static readonly int fpParams0 = Shader.PropertyToID("_FPParams0");
            public static readonly int fpParams1 = Shader.PropertyToID("_FPParams1");
            public static readonly int fpParams2 = Shader.PropertyToID("_FPParams2");

            // Main Light Shadows
            public static readonly int mainLightWorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            public static readonly int cascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            public static readonly int cascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            public static readonly int cascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            public static readonly int cascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            public static readonly int cascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            public static readonly int mainLightShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
            public static readonly int mainLightShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
            public static readonly int mainLightShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");

            // Additional Lights
            public static readonly int additionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");

            // Additional Lights Shadows
            public static readonly int additionalShadowOffset0 = Shader.PropertyToID("_AdditionalShadowOffset0");
            public static readonly int additionalShadowOffset1 = Shader.PropertyToID("_AdditionalShadowOffset1");
            public static readonly int additionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");

            // Misc
            public static readonly int mainLightCookieTextureFormat = Shader.PropertyToID("_MainLightCookieTextureFormat");
            public static readonly int additionalLightsCookieAtlasTextureFormat = Shader.PropertyToID("_AdditionalLightsCookieAtlasTextureFormat");
            public static readonly int enableProbeVolumes = Shader.PropertyToID("_EnableProbeVolumes");
        }
    }
}
