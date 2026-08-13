using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    // URP 3D global shader variables.
    // Warning! First part of this struct must stay identical to BaseGlobalShaderVars layout.
    // If an additional field must be added but shouldn't be included in BaseGlobalShaderVars nor GlobalShaderVars2D (URP2D), add it after their common fields.
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlobalShaderVars
    {
        // Start of BaseGlobalShaderVars fields - keep them together and first
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
        public Vector4 _GlobalMipBiasVec4; // UnityInput.hlsl - 2 floats of padding
        // End of BaseGlobalShaderVars fields
    }

    internal static class GlobalShaderVarsExtensions
    {
        // Reinterprets the BaseGlobalShaderVars-shaped initial part of GlobalShaderVars as a ref BaseGlobalShaderVars,
        // so the base fields can be filled in place without an intermediate copy. Relies on the identical
        // sequential layout of initial part of GlobalShaderVars and BaseGlobalShaderVars.
        internal static ref BaseGlobalShaderVars AsBase(this ref GlobalShaderVars vars)
            => ref UnsafeUtility.As<GlobalShaderVars, BaseGlobalShaderVars>(ref vars);

        /// <summary>
        /// Sets all shader variables as global uniforms using the provided CommandBuffer.
        /// This is a fallback when persistent constant buffer mode is disabled.
        /// Extension with `in` receiver so callers can invoke it on the ref-readonly return of GetDefaultVars without copying the struct.
        /// </summary>
        internal static void SetGlobals(this in GlobalShaderVars vars, IBaseCommandBuffer cmd)
        {
            cmd.SetGlobalVector(ShaderPropertyId.time, vars._Time);
            cmd.SetGlobalVector(ShaderPropertyId.sinTime, vars._SinTime);
            cmd.SetGlobalVector(ShaderPropertyId.cosTime, vars._CosTime);
            cmd.SetGlobalVector(ShaderPropertyId.deltaTime, vars.unity_DeltaTime);
            cmd.SetGlobalVector(ShaderPropertyId.timeParameters, vars._TimeParameters);
            cmd.SetGlobalVector(ShaderPropertyId.lastTimeParameters, vars._LastTimeParameters);
            cmd.SetGlobalVector(ShaderPropertyId.screenParams, vars._ScreenParams);
            cmd.SetGlobalVector(ShaderPropertyId.zBufferParams, vars._ZBufferParams);
            cmd.SetGlobalVector(ShaderPropertyId.orthoParams, vars.unity_OrthoParams);
            cmd.SetGlobalVector(ShaderPropertyId.rtHandleScale, vars._RTHandleScale);
            cmd.SetGlobalVector(ShaderPropertyId.scaledScreenParams, vars._ScaledScreenParams);
            cmd.SetGlobalVector(ShaderPropertyId.screenSize, vars._ScreenSize);
            cmd.SetGlobalVector(ShaderPropertyId.screenSizeOverride, vars._ScreenSizeOverride);
            cmd.SetGlobalVector(ShaderPropertyId.screenCoordScaleBias, vars._ScreenCoordScaleBias);
            cmd.SetGlobalVector(ShaderPropertyId.globalMipBias, new Vector2(vars._GlobalMipBiasVec4.x, vars._GlobalMipBiasVec4.y));
        }
    }
}
