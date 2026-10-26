using System;
using System.Runtime.InteropServices;
using Dirty = UnityEngine.Rendering.Universal.GlobalShaderVariablesBaseDirty;

namespace UnityEngine.Rendering.Universal
{
    // One bit per pushable field of GlobalShaderVariablesBase, so PushGlobal() only re-uploads what changed.
    //
    // GlobalShaderVariablesUploader is the only thing that raises or clears these bits:
    // - a property setter raises the bit of the field it writes, at record time inside a BeginFill/EndFill window
    // - EndFill snapshots the raised bits as the defaults of the camera, then clears the staging ones
    // - PushGlobal clears the bits it just pushed, so nothing goes out twice
    // - ResetToDefault raises them again from that snapshot, which is how PushDefaultToGlobal re-sends the whole set
    //   recorded for the camera once something else has overwritten the global state
    //
    // Warning! Add a bit here, an accessor in GlobalShaderVariablesUploader and a line in SetGlobals() for every new field.
    [Flags]
    internal enum GlobalShaderVariablesBaseDirty : uint
    {
        None = 0u,
        _Time = 1u << 0,
        _SinTime = 1u << 1,
        _CosTime = 1u << 2,
        unity_DeltaTime = 1u << 3,
        _TimeParameters = 1u << 4,
        _LastTimeParameters = 1u << 5,
        _ScreenParams = 1u << 6,
        _ZBufferParams = 1u << 7,
        unity_OrthoParams = 1u << 8,
        _RTHandleScale = 1u << 9,
        _ScaledScreenParams = 1u << 10,
        _ScreenSize = 1u << 11,
        _ScreenSizeOverride = 1u << 12,
        _ScreenCoordScaleBias = 1u << 13,
        unity_AmbientSky = 1u << 14,
        unity_AmbientEquator = 1u << 15,
        unity_AmbientGround = 1u << 16,
        _GlobalMipBias = 1u << 17,
        _DitheringTextureInvSize = 1u << 18,

        // Environment
        _GlossyEnvironmentColor = 1u << 19,
        _SubtractiveShadowColor = 1u << 20,
        _GlossyEnvironmentCubeMap_HDR = 1u << 21,

        // Keep this referencing the last bit declared above when adding a variable.
        All = (_GlossyEnvironmentCubeMap_HDR << 1) - 1u,
    }

    // Global shader variables shared by URP 2D and 3D, see GlobalShaderVariables for the declaration guidelines.
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlobalShaderVariablesBase
    {
        // The camera matrices and the billboard params are deliberately absent as CommandBuffer.SetViewMatrix() and
        // SetViewProjectionMatrices() push them straight to the engine.
        // Any direct call to these cmd APIs would make the data in this struct diverging from engine.
        // _WorldSpaceCameraPos is absent for a different reason: it is a float3 and would never pack properly here.
        public Vector4 _Time; // UnityInput.hlsl
        public Vector4 _SinTime; // UnityInput.hlsl
        public Vector4 _CosTime; // UnityInput.hlsl
        public Vector4 unity_DeltaTime; // UnityInput.hlsl
        public Vector4 _TimeParameters; // UnityInput.hlsl
        public Vector4 _LastTimeParameters; // UnityInput.hlsl
        public Vector4 _ScreenParams; // UnityInput.hlsl
        public Vector4 _ZBufferParams; // UnityInput.hlsl
        public Vector4 unity_OrthoParams; // UnityInput.hlsl
        public Vector4 _RTHandleScale; // UnityInput.hlsl
        public Vector4 _ScaledScreenParams; // Unity.hlsl
        public Vector4 _ScreenSize; // UnityInput.hlsl
        public Vector4 _ScreenSizeOverride; // Unity.hlsl
        public Vector4 _ScreenCoordScaleBias; // Unity.hlsl

        // Ambient gradient, filled from RenderSettings.
        public Vector4 unity_AmbientSky; // UnityInput.hlsl
        public Vector4 unity_AmbientEquator; // UnityInput.hlsl
        public Vector4 unity_AmbientGround; // UnityInput.hlsl
        public Vector2 _GlobalMipBias; // UnityInput.hlsl
        public float _DitheringTextureInvSize; // LODCrossFade.hlsl
        public float _URPPadding1;

        // Environment, come from RenderSettings.
        public Vector4 _GlossyEnvironmentColor; // Unity.hlsl
        public Vector4 _SubtractiveShadowColor; // Unity.hlsl
        public Vector4 _GlossyEnvironmentCubeMap_HDR; // Unity.hlsl
    }

    internal static class GlobalShaderVariablesBaseExtensions
    {
        /// <summary>
        /// Sets the shader variables flagged in <paramref name="dirty"/> as global uniforms using the provided CommandBuffer.
        /// Extension with `in` receiver so callers can invoke it without copying the struct.
        /// </summary>
        internal static void SetGlobals(this in GlobalShaderVariablesBase vars, IBaseCommandBuffer cmd, Dirty dirty)
        {
            if (dirty == Dirty.None)
                return;

            if ((dirty & Dirty._Time) != 0) cmd.SetGlobalVector(ShaderPropertyId.time, vars._Time);
            if ((dirty & Dirty._SinTime) != 0) cmd.SetGlobalVector(ShaderPropertyId.sinTime, vars._SinTime);
            if ((dirty & Dirty._CosTime) != 0) cmd.SetGlobalVector(ShaderPropertyId.cosTime, vars._CosTime);
            if ((dirty & Dirty.unity_DeltaTime) != 0) cmd.SetGlobalVector(ShaderPropertyId.deltaTime, vars.unity_DeltaTime);
            if ((dirty & Dirty._TimeParameters) != 0) cmd.SetGlobalVector(ShaderPropertyId.timeParameters, vars._TimeParameters);
            if ((dirty & Dirty._LastTimeParameters) != 0) cmd.SetGlobalVector(ShaderPropertyId.lastTimeParameters, vars._LastTimeParameters);
            if ((dirty & Dirty._ScreenParams) != 0) cmd.SetGlobalVector(ShaderPropertyId.screenParams, vars._ScreenParams);
            if ((dirty & Dirty._ZBufferParams) != 0) cmd.SetGlobalVector(ShaderPropertyId.zBufferParams, vars._ZBufferParams);
            if ((dirty & Dirty.unity_OrthoParams) != 0) cmd.SetGlobalVector(ShaderPropertyId.orthoParams, vars.unity_OrthoParams);
            if ((dirty & Dirty._RTHandleScale) != 0) cmd.SetGlobalVector(ShaderPropertyId.rtHandleScale, vars._RTHandleScale);
            if ((dirty & Dirty._ScaledScreenParams) != 0) cmd.SetGlobalVector(ShaderPropertyId.scaledScreenParams, vars._ScaledScreenParams);
            if ((dirty & Dirty._ScreenSize) != 0) cmd.SetGlobalVector(ShaderPropertyId.screenSize, vars._ScreenSize);
            if ((dirty & Dirty._ScreenSizeOverride) != 0) cmd.SetGlobalVector(ShaderPropertyId.screenSizeOverride, vars._ScreenSizeOverride);
            if ((dirty & Dirty._ScreenCoordScaleBias) != 0) cmd.SetGlobalVector(ShaderPropertyId.screenCoordScaleBias, vars._ScreenCoordScaleBias);
            if ((dirty & Dirty.unity_AmbientSky) != 0) cmd.SetGlobalVector(ShaderPropertyId.ambientSkyColor, vars.unity_AmbientSky);
            if ((dirty & Dirty.unity_AmbientEquator) != 0) cmd.SetGlobalVector(ShaderPropertyId.ambientEquatorColor, vars.unity_AmbientEquator);
            if ((dirty & Dirty.unity_AmbientGround) != 0) cmd.SetGlobalVector(ShaderPropertyId.ambientGroundColor, vars.unity_AmbientGround);
            if ((dirty & Dirty._GlobalMipBias) != 0) cmd.SetGlobalVector(ShaderPropertyId.globalMipBias, vars._GlobalMipBias);
            if ((dirty & Dirty._DitheringTextureInvSize) != 0) cmd.SetGlobalFloat(ShaderPropertyId.ditheringTextureInvSize, vars._DitheringTextureInvSize);

            // Environment
            if ((dirty & Dirty._GlossyEnvironmentColor) != 0) cmd.SetGlobalVector(ShaderPropertyId.glossyEnvironmentColor, vars._GlossyEnvironmentColor);
            if ((dirty & Dirty._SubtractiveShadowColor) != 0) cmd.SetGlobalVector(ShaderPropertyId.subtractiveShadowColor, vars._SubtractiveShadowColor);
            if ((dirty & Dirty._GlossyEnvironmentCubeMap_HDR) != 0) cmd.SetGlobalVector(ShaderPropertyId.glossyEnvironmentCubeMapHDR, vars._GlossyEnvironmentCubeMap_HDR);
        }
    }
}
