using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    // ScriptableRenderer's global shader variables, usually only used to complete URP2D or 3D final set of variables.
    // Warning! Layout must stay identical to initial fields of GlobalShaderVars and GlobalShaderVars2D so a ref-view via UnsafeUtility.As is valid.
    // If you add a field to this struct, add one at the exact same place in both GlobalShaderVars and GlobalShaderVars2D.
    [StructLayout(LayoutKind.Sequential)]
    internal struct BaseGlobalShaderVars
    {
        public Matrix4x4 unity_WorldToCamera; // UnityInput.hlsl
        public Matrix4x4 unity_CameraToWorld; // UnityInput.hlsl
        public Matrix4x4 unity_MatrixInvV; // UnityInput.hlsl
        public Matrix4x4 unity_MatrixV; // UnityInput.hlsl
        public Matrix4x4 glstate_matrix_projection; // UnityInput.hlsl
        public Matrix4x4 _ProjectionMatrix; // non-GPU adjusted equivalent of the matrix above
        public Matrix4x4 unity_MatrixInvP; // UnityInput.hlsl
        public Matrix4x4 unity_MatrixVP; // UnityInput.hlsl
        public Matrix4x4 unity_MatrixInvVP; // UnityInput.hlsl
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
        public Vector4 _ProjectionParams; // UnityInput.hlsl
        public Vector4 _ScaledScreenParams; // Unity.hlsl
        public Vector4 _ScreenSize; // UnityInput.hlsl
        public Vector4 _ScreenSizeOverride; // Unity.hlsl
        public Vector4 _ScreenCoordScaleBias; // Unity.hlsl
        public Vector4 _WorldSpaceCameraPosVec4; // UnityInput.hlsl - 1 float of padding
        public Vector4 _GlobalMipBiasVec4; // UnityInput.hlsl - 2 floats of padding
        public Vector4 unity_BillboardNormal; // SpeedTree7BillboardInput.hlsl
        public Vector4 unity_BillboardTangent; // SpeedTree7BillboardInput.hlsl
        public Vector4 unity_BillboardCameraParams; // SpeedTree7BillboardInput.hlsl
    }

    internal static class BaseGlobalShaderVarsExtensions
    {
        /// <summary>
        /// Sets all shader variables as global uniforms using the provided CommandBuffer, only use it directly for custom ScriptableRenderer.
        /// Extension with `in` receiver so callers can invoke it on the ref-readonly return of GetDefaultVars without copying the ~880-byte struct.
        /// </summary>
        internal static void SetGlobals(this in BaseGlobalShaderVars vars, IBaseCommandBuffer cmd, bool setCameraMatrices = true)
        {
            if (setCameraMatrices)
            {
                cmd.SetGlobalMatrix(ShaderPropertyId.worldToCameraMatrix, vars.unity_WorldToCamera);
                cmd.SetGlobalMatrix(ShaderPropertyId.cameraToWorldMatrix, vars.unity_CameraToWorld);
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewMatrix, vars.unity_MatrixInvV);
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseProjectionMatrix, vars.unity_MatrixInvP);

                // SetViewProjectionMatrices is required instead of the commented-out raw SetGlobalMatrix calls below:
                // it also refreshes engine-side state that plain uniform writes do not touch, e.g. the camera
                // frustum planes (unity_CameraWorldClipPlanes) and the GPU-adjusted projection sign used to derive
                // _ProjectionParams.x deep in GfxDevice.
                cmd.SetViewProjectionMatrices(vars.unity_MatrixV, vars._ProjectionMatrix);
                //cmd.SetGlobalMatrix(ShaderPropertyId.viewMatrix, vars.unity_MatrixV);
                //cmd.SetGlobalMatrix(ShaderPropertyId.projectionMatrix, vars.glstate_matrix_projection);
                //cmd.SetGlobalMatrix(ShaderPropertyId.viewAndProjectionMatrix, vars.unity_MatrixVP);

                cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewAndProjectionMatrix, vars.unity_MatrixInvVP);
            }

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
            cmd.SetGlobalVector(ShaderPropertyId.projectionParams, vars._ProjectionParams);
            cmd.SetGlobalVector(ShaderPropertyId.scaledScreenParams, vars._ScaledScreenParams);
            cmd.SetGlobalVector(ShaderPropertyId.screenSize, vars._ScreenSize);
            cmd.SetGlobalVector(ShaderPropertyId.screenSizeOverride, vars._ScreenSizeOverride);
            cmd.SetGlobalVector(ShaderPropertyId.screenCoordScaleBias, vars._ScreenCoordScaleBias);
            cmd.SetGlobalVector(ShaderPropertyId.worldSpaceCameraPos, new Vector3(vars._WorldSpaceCameraPosVec4.x, vars._WorldSpaceCameraPosVec4.y, vars._WorldSpaceCameraPosVec4.z));
            cmd.SetGlobalVector(ShaderPropertyId.globalMipBias, new Vector2(vars._GlobalMipBiasVec4.x, vars._GlobalMipBiasVec4.y));
            cmd.SetGlobalVector(ShaderPropertyId.billboardNormal, vars.unity_BillboardNormal);
            cmd.SetGlobalVector(ShaderPropertyId.billboardTangent, vars.unity_BillboardTangent);
            cmd.SetGlobalVector(ShaderPropertyId.billboardCameraParams, vars.unity_BillboardCameraParams);
        }
    }
}
