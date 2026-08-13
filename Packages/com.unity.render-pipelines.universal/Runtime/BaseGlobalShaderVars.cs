using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    // ScriptableRenderer's global shader variables, usually only used to complete URP2D or 3D final set of variables.
    // Warning! Layout must stay identical to initial fields of GlobalShaderVars and GlobalShaderVars2D so a ref-view via UnsafeUtility.As is valid.
    // If you add a field to this struct, add one at the exact same place in both GlobalShaderVars and GlobalShaderVars2D.
    [StructLayout(LayoutKind.Sequential)]
    internal struct BaseGlobalShaderVars
    {
        // The camera matrices and the billboard params are deliberately absent as CommandBuffer.SetViewMatrix() and
        // SetViewProjectionMatrices() push them straight to the engine. 
        // Any direct call to these cmd APIs would make the data in this struct diverging from engine.
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
    }

    internal static class BaseGlobalShaderVarsExtensions
    {
        /// <summary>
        /// Sets all shader variables as global uniforms using the provided CommandBuffer, only use it directly for custom ScriptableRenderer.
        /// Extension with `in` receiver so callers can invoke it on the ref-readonly return of GetDefaultVars without copying the struct.
        /// </summary>
        internal static void SetGlobals(this in BaseGlobalShaderVars vars, IBaseCommandBuffer cmd)
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
