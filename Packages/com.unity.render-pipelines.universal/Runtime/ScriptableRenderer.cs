using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    ///  Class <c>ScriptableRenderer</c> implements a rendering strategy. It describes how culling and lighting works and
    /// the effects supported.
    ///
    /// TODO RENDERGRAPH: UPDATE THIS DOC FOR THE RENDERGRAPH PATH
    ///  A renderer can be used for all cameras or be overridden on a per-camera basis. It will implement light culling and setup
    /// and describe a list of <c>ScriptableRenderPass</c> to execute in a frame. The renderer can be extended to support more effect with additional
    ///  <c>ScriptableRendererFeature</c>. Resources for the renderer are serialized in <c>ScriptableRendererData</c>.
    ///
    /// The renderer resources are serialized in <c>ScriptableRendererData</c>.
    /// <seealso cref="ScriptableRendererData"/>
    /// <seealso cref="ScriptableRendererFeature"/>
    /// <seealso cref="ScriptableRenderPass"/>
    /// </summary>
    public abstract partial class ScriptableRenderer : IDisposable
    {
        private static partial class Profiling
        {
            private const string k_Name = nameof(ScriptableRenderer);
            public static readonly ProfilingSampler setPerCameraShaderVariables = new ProfilingSampler($"{k_Name}.{nameof(SetPerCameraShaderVariables)}");
            public static readonly ProfilingSampler sortRenderPasses = new ProfilingSampler($"Sort Render Passes");
            public static readonly ProfilingSampler recordRenderGraph = new ProfilingSampler($"On Record Render Graph");
            public static readonly ProfilingSampler setupLights = new ProfilingSampler($"{k_Name}.{nameof(SetupLights)}");
            public static readonly ProfilingSampler setupCamera = new ProfilingSampler($"Setup Camera Properties");
            public static readonly ProfilingSampler vfxProcessCamera = new ProfilingSampler($"VFX Process Camera");
            public static readonly ProfilingSampler addRenderPasses = new ProfilingSampler($"{k_Name}.{nameof(AddRenderPasses)}");
            public static readonly ProfilingSampler setupRenderPasses = new ProfilingSampler($"{k_Name}.{nameof(SetupRenderPasses)}");
            public static readonly ProfilingSampler clearRenderingState = new ProfilingSampler($"{k_Name}.{nameof(ClearRenderingState)}");
            public static readonly ProfilingSampler internalStartRendering = new ProfilingSampler($"{k_Name}.{nameof(InternalStartRendering)}");
            public static readonly ProfilingSampler internalFinishRenderingCommon = new ProfilingSampler($"{k_Name}.{nameof(InternalFinishRenderingCommon)}");
            public static readonly ProfilingSampler drawGizmos = new ProfilingSampler($"{nameof(DrawGizmos)}");
            public static readonly ProfilingSampler drawWireOverlay = new ProfilingSampler($"{nameof(DrawWireOverlay)}");
            internal static readonly ProfilingSampler beginXRRendering = new ProfilingSampler($"Begin XR Rendering");
            internal static readonly ProfilingSampler endXRRendering = new ProfilingSampler($"End XR Rendering");
            internal static readonly ProfilingSampler initRenderGraphFrame = new ProfilingSampler($"Initialize Frame");
            internal static readonly ProfilingSampler setEditorTarget = new ProfilingSampler($"Set Editor Target");

            public static class RenderBlock
            {
                private const string k_Name = nameof(RenderPassBlock);
                public static readonly ProfilingSampler beforeRendering = new ProfilingSampler($"{k_Name}.{nameof(RenderPassBlock.BeforeRendering)}");
                public static readonly ProfilingSampler mainRenderingOpaque = new ProfilingSampler($"{k_Name}.{nameof(RenderPassBlock.MainRenderingOpaque)}");
                public static readonly ProfilingSampler mainRenderingTransparent = new ProfilingSampler($"{k_Name}.{nameof(RenderPassBlock.MainRenderingTransparent)}");
                public static readonly ProfilingSampler afterRendering = new ProfilingSampler($"{k_Name}.{nameof(RenderPassBlock.AfterRendering)}");
            }

            public static class RenderPass
            {
                private const string k_Name = nameof(ScriptableRenderPass);

                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                public static readonly ProfilingSampler configure = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderPass.Configure)}");
                #pragma warning restore CS0618

                public static readonly ProfilingSampler setRenderPassAttachments = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderer.SetRenderPassAttachments)}");
            }
        }

        /// <summary>
        /// This setting controls if the camera editor should display the camera stack category.
        /// If your renderer is not supporting stacking this one should return 0.
        /// For the UI to show the Camera Stack widget this must support CameraRenderType.Base.
        /// <see cref="CameraRenderType"/>
        /// </summary>
        /// <returns>The bitmask of the supported camera render types in the renderer's current state.</returns>
        public virtual int SupportedCameraStackingTypes()
        {
            return 0;
        }

        /// <summary>
        /// Check if the given camera render type is supported in the renderer's current state.
        /// </summary>
        /// <param name="cameraRenderType">The camera render type that is checked if supported.</param>
        /// <returns>True if the given camera render type is supported in the renderer's current state.</returns>
        public bool SupportsCameraStackingType(CameraRenderType cameraRenderType)
        {
            return (SupportedCameraStackingTypes() & 1 << (int)cameraRenderType) != 0;
        }


        // NOTE: This is a temporary solution until ScriptableRenderer has a system for partially shared features.
        // TAA (and similar) affect the whole pipe. The code is split into two parts in terms of ownership.
        // The ScriptableRenderer "shared" code (Camera) and the ScriptableRenderer "specific" code (the ScriptableRenderPasses).
        // For example: TAA is enabled and configured from the Camera, which is used by any ScriptableRenderer.
        // TAA also jitters the Camera matrix for all ScriptableRenderers.
        // However a Renderer might not implement a motion vector pass, which the TAA needs to function correctly.
        //
        /// <summary>
        /// Check if the ScriptableRenderer implements a motion vector pass for temporal techniques.
        /// The Camera will check this to enable/disable features and/or apply jitter when required.
        ///
        /// For example, Temporal Anti-aliasing in the Camera settings is enabled only if the ScriptableRenderer can support motion vectors.
        /// </summary>
        /// <returns>Returns true if the ScriptableRenderer implements a motion vector pass. False otherwise.</returns>
        protected internal virtual bool SupportsMotionVectors()
        {
            return false;
        }

        /// <summary>
        /// Override to provide a custom profiling name
        /// </summary>
        protected ProfilingSampler profilingExecute { get; set; }

        /// <summary>
        /// Used to determine whether to release render targets used by the renderer when the renderer is no more active
        /// </summary>
        internal bool hasReleasedRTs = true;

        /// <summary>
        /// Configures the supported features for this renderer. When creating custom renderers
        /// for Universal Render Pipeline you can choose to opt-in or out for specific features.
        /// </summary>
        public class RenderingFeatures
        {
            /// <summary>
            /// This setting controls if the camera editor should display the camera stack category.
            /// Renderers that don't support camera stacking will only render camera of type CameraRenderType.Base
            /// <see cref="CameraRenderType"/>
            /// <seealso cref="UniversalAdditionalCameraData.cameraStack"/>
            /// </summary>
            [Obsolete("cameraStacking has been deprecated use SupportedCameraRenderTypes() in ScriptableRenderer instead.", true)]
            public bool cameraStacking { get; set; } = false;

            /// <summary>
            /// This setting controls if the Universal Render Pipeline asset should expose MSAA option.
            /// </summary>
            public bool msaa { get; set; } = true;
        }

        /// <summary>
        /// The class responsible for providing access to debug view settings to renderers and render passes.
        /// </summary>
        internal DebugHandler DebugHandler { get; }

        /// <summary>
        /// The renderer we are currently rendering with, for low-level render control only.
        /// <c>current</c> is null outside rendering scope.
        /// Similar to https://docs.unity3d.com/ScriptReference/Camera-current.html
        /// </summary>
        internal static ScriptableRenderer current = null;

        /// <summary>
        /// Set camera matrices. This method will set <c>UNITY_MATRIX_V</c>, <c>UNITY_MATRIX_P</c>, <c>UNITY_MATRIX_VP</c> to camera matrices.
        /// Additionally this will also set <c>unity_CameraProjection</c> and <c>unity_CameraProjection</c>.
        /// If <c>setInverseMatrices</c> is set to true this function will also set <c>UNITY_MATRIX_I_V</c> and <c>UNITY_MATRIX_I_VP</c>.
        /// This function has no effect when rendering in stereo. When in stereo rendering you cannot override camera matrices.
        /// If you need to set general purpose view and projection matrices call <see cref="SetViewAndProjectionMatrices(CommandBuffer, Matrix4x4, Matrix4x4, bool)"/> instead.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>
        public static void SetCameraMatrices(CommandBuffer cmd, ref CameraData cameraData, bool setInverseMatrices)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            SetCameraMatrices(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData.universalCameraData, setInverseMatrices, cameraData.IsCameraProjectionMatrixFlipped());
            #pragma warning restore CS0618
        }

        /// <summary>
        /// Set camera matrices. This method will set <c>UNITY_MATRIX_V</c>, <c>UNITY_MATRIX_P</c>, <c>UNITY_MATRIX_VP</c> to camera matrices.
        /// Additionally this will also set <c>unity_CameraProjection</c> and <c>unity_CameraProjection</c>.
        /// If <c>setInverseMatrices</c> is set to true this function will also set <c>UNITY_MATRIX_I_V</c> and <c>UNITY_MATRIX_I_VP</c>.
        /// This function has no effect when rendering in stereo. When in stereo rendering you cannot override camera matrices.
        /// If you need to set general purpose view and projection matrices call <see cref="SetViewAndProjectionMatrices(CommandBuffer, Matrix4x4, Matrix4x4, bool)"/> instead.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        /// <param name="setInverseMatrices">Set this to true if you also need to set inverse camera matrices.</param>
        public static void SetCameraMatrices(CommandBuffer cmd, UniversalCameraData cameraData, bool setInverseMatrices)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            SetCameraMatrices(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData, setInverseMatrices, cameraData.IsCameraProjectionMatrixFlipped());
            #pragma warning restore CS0618
        }

        internal static void SetCameraMatrices(RasterCommandBuffer cmd, UniversalCameraData cameraData, bool setInverseMatrices, bool isTargetFlipped)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                cameraData.PushBuiltinShaderConstantsXR(cmd, isTargetFlipped);
                XRSystemUniversal.MarkShaderProperties(cmd, cameraData.xrUniversal, isTargetFlipped);
                return;
            }
#endif

            // NOTE: the URP default main view/projection matrices are the CameraData view/projection matrices.
            Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
            Matrix4x4 projectionMatrix = cameraData.GetProjectionMatrix(); // Jittered, non-gpu

            // TODO: Investigate why SetViewAndProjectionMatrices is causing y-flip / winding order issue
            // for now using cmd.SetViewProjecionMatrices
            //SetViewAndProjectionMatrices(cmd, viewMatrix, cameraData.GetDeviceProjectionMatrix(), setInverseMatrices);

            // Set the default view/projection, note: projectionMatrix will be set as a gpu-projection (gfx api adjusted) for rendering.
            cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

            if (setInverseMatrices)
            {
                Matrix4x4 gpuProjectionMatrix = cameraData.GetGPUProjectionMatrix(isTargetFlipped); // TODO: invProjection might NOT match the actual projection (invP*P==I) as the target flip logic has diverging paths.
                Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(viewMatrix);
                Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(gpuProjectionMatrix);
                Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;

                // There's an inconsistency in handedness between unity_matrixV and unity_WorldToCamera
                // Unity changes the handedness of unity_WorldToCamera (see Camera::CalculateMatrixShaderProps)
                // we will also change it here to avoid breaking existing shaders. (case 1257518)
                Matrix4x4 worldToCameraMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * viewMatrix;
                Matrix4x4 cameraToWorldMatrix = worldToCameraMatrix.inverse;
                cmd.SetGlobalMatrix(ShaderPropertyId.worldToCameraMatrix, worldToCameraMatrix);
                cmd.SetGlobalMatrix(ShaderPropertyId.cameraToWorldMatrix, cameraToWorldMatrix);

                cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewMatrix, inverseViewMatrix);
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseProjectionMatrix, inverseProjectionMatrix);
                cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewAndProjectionMatrix, inverseViewProjection);
            }

            // TODO: Add SetPerCameraClippingPlaneProperties here once we are sure it correctly behaves in overlay camera for some time
        }

        /// <summary>
        /// Set camera and screen shader variables as described in https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        /// <typeparam name="T">Base type for the CommandBuffer</typeparam>
        void SetPerCameraShaderVariables(RasterCommandBuffer cmd, UniversalCameraData cameraData)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            SetPerCameraShaderVariables(cmd, cameraData, new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height), cameraData.IsCameraProjectionMatrixFlipped());
            #pragma warning restore CS0618
        }

        void SetPerCameraShaderVariables(RasterCommandBuffer cmd, UniversalCameraData cameraData, Vector2Int cameraTargetSizeCopy, bool isTargetFlipped)
        {
            using var profScope = new ProfilingScope(Profiling.setPerCameraShaderVariables);

            Camera camera = cameraData.camera;

            float scaledCameraTargetWidth = (float)cameraTargetSizeCopy.x;
            float scaledCameraTargetHeight = (float)cameraTargetSizeCopy.y;
            float cameraWidth = (float)camera.pixelWidth;
            float cameraHeight = (float)camera.pixelHeight;

            // Overlay cameras don't have a viewport. Must use the computed/inherited viewport instead of the camera one.
            if (cameraData.renderType == CameraRenderType.Overlay)
            {
                // Overlay cameras inherits viewport from base.
                // pixelRect/Width/Height is the viewport in pixels.
                cameraWidth = cameraData.pixelWidth;
                cameraHeight = cameraData.pixelHeight;
            }

            // Use eye texture's width and height as screen params when XR is enabled
            if (cameraData.xr.enabled)
            {
                cameraWidth = (float)cameraTargetSizeCopy.x;
                cameraHeight = (float)cameraTargetSizeCopy.y;

                useRenderPassEnabled = false;
            }

            if (camera.allowDynamicResolution)
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                // Use eye texture's scaled width and height as screen params when XR is enabled
                if (cameraData.xr.enabled)
                {
                    scaledCameraTargetWidth = (float)cameraData.xr.renderTargetScaledWidth;
                    scaledCameraTargetHeight = (float)cameraData.xr.renderTargetScaledHeight;
                }
                else
#endif
                {
                scaledCameraTargetWidth *= ScalableBufferManager.widthScaleFactor;
                scaledCameraTargetHeight *= ScalableBufferManager.heightScaleFactor;
                }
            }

            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;
            float invNear = Mathf.Approximately(near, 0.0f) ? 0.0f : 1.0f / near;
            float invFar = Mathf.Approximately(far, 0.0f) ? 0.0f : 1.0f / far;
            float isOrthographic = camera.orthographic ? 1.0f : 0.0f;

            // From http://www.humus.name/temp/Linearize%20depth.txt
            // But as depth component textures on OpenGL always return in 0..1 range (as in D3D), we have to use
            // the same constants for both D3D and OpenGL here.
            // OpenGL would be this:
            // zc0 = (1.0 - far / near) / 2.0;
            // zc1 = (1.0 + far / near) / 2.0;
            // D3D is this:
            float zc0 = 1.0f - far * invNear;
            float zc1 = far * invNear;

            Vector4 zBufferParams = new Vector4(zc0, zc1, zc0 * invFar, zc1 * invFar);

            if (SystemInfo.usesReversedZBuffer)
            {
                zBufferParams.y += zBufferParams.x;
                zBufferParams.x = -zBufferParams.x;
                zBufferParams.w += zBufferParams.z;
                zBufferParams.z = -zBufferParams.z;
            }

            // Projection flip sign logic is very deep in GfxDevice::SetInvertProjectionMatrix
            // This setup is tailored especially for overlay camera game view
            // For other scenarios this will be overwritten correctly by SetupCameraProperties
            float projectionFlipSign = isTargetFlipped ? -1.0f : 1.0f;
            Vector4 projectionParams = new Vector4(projectionFlipSign, near, far, 1.0f * invFar);
            cmd.SetGlobalVector(ShaderPropertyId.projectionParams, projectionParams);

            Vector4 orthoParams = new Vector4(camera.orthographicSize * cameraData.aspectRatio, camera.orthographicSize, 0.0f, isOrthographic);

            // Camera and Screen variables as described in https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
            cmd.SetGlobalVector(ShaderPropertyId.worldSpaceCameraPos, cameraData.worldSpaceCameraPos);
            cmd.SetGlobalVector(ShaderPropertyId.screenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
            cmd.SetGlobalVector(ShaderPropertyId.scaledScreenParams, new Vector4(scaledCameraTargetWidth, scaledCameraTargetHeight, 1.0f + 1.0f / scaledCameraTargetWidth, 1.0f + 1.0f / scaledCameraTargetHeight));
            cmd.SetGlobalVector(ShaderPropertyId.zBufferParams, zBufferParams);
            cmd.SetGlobalVector(ShaderPropertyId.orthoParams, orthoParams);

            cmd.SetGlobalVector(ShaderPropertyId.screenSize, new Vector4(scaledCameraTargetWidth, scaledCameraTargetHeight, 1.0f / scaledCameraTargetWidth, 1.0f / scaledCameraTargetHeight));
            cmd.SetKeyword(ShaderGlobalKeywords.SCREEN_COORD_OVERRIDE, cameraData.useScreenCoordOverride);
            cmd.SetGlobalVector(ShaderPropertyId.screenSizeOverride, cameraData.screenSizeOverride);
            cmd.SetGlobalVector(ShaderPropertyId.screenCoordScaleBias, cameraData.screenCoordScaleBias);

            // { w / RTHandle.maxWidth, h / RTHandle.maxHeight } : xy = currFrame, zw = prevFrame
            // TODO(@sandy-carter) set to RTHandles.rtHandleProperties.rtHandleScale once dynamic scaling is set up
            cmd.SetGlobalVector(ShaderPropertyId.rtHandleScale, Vector4.one);

            // Calculate a bias value which corrects the mip lod selection logic when image scaling is active.
            // We clamp this value to 0.0 or less to make sure we don't end up reducing image detail in the downsampling case.
            float mipBias = Math.Min((float)-Math.Log(cameraWidth / scaledCameraTargetWidth, 2.0f), 0.0f);
            // Temporal Anti-aliasing can use negative mip bias to increase texture sharpness and new information for the jitter.
            float taaMipBias = Math.Min(cameraData.taaSettings.mipBias, 0.0f);
            mipBias = Math.Min(mipBias, taaMipBias);
            cmd.SetGlobalVector(ShaderPropertyId.globalMipBias, new Vector2(mipBias, Mathf.Pow(2.0f, mipBias)));

            //Set per camera matrices.
            SetCameraMatrices(cmd, cameraData, true, isTargetFlipped);
        }

        /// <summary>
        /// Set the Camera billboard properties.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        void SetPerCameraBillboardProperties(RasterCommandBuffer cmd, UniversalCameraData cameraData)
        {
            Matrix4x4 worldToCameraMatrix = cameraData.GetViewMatrix();
            Vector3 cameraPos = cameraData.worldSpaceCameraPos;

            cmd.SetKeyword(ShaderGlobalKeywords.BillboardFaceCameraPos, QualitySettings.billboardsFaceCameraPosition);

            Vector3 billboardTangent;
            Vector3 billboardNormal;
            float cameraXZAngle;
            CalculateBillboardProperties(worldToCameraMatrix, out billboardTangent, out billboardNormal, out cameraXZAngle);

            cmd.SetGlobalVector(ShaderPropertyId.billboardNormal, new Vector4(billboardNormal.x, billboardNormal.y, billboardNormal.z, 0.0f));
            cmd.SetGlobalVector(ShaderPropertyId.billboardTangent, new Vector4(billboardTangent.x, billboardTangent.y, billboardTangent.z, 0.0f));
            cmd.SetGlobalVector(ShaderPropertyId.billboardCameraParams, new Vector4(cameraPos.x, cameraPos.y, cameraPos.z, cameraXZAngle));
        }

        private static void CalculateBillboardProperties(
            in Matrix4x4 worldToCameraMatrix,
            out Vector3 billboardTangent,
            out Vector3 billboardNormal,
            out float cameraXZAngle)
        {
            Matrix4x4 cameraToWorldMatrix = worldToCameraMatrix;
            cameraToWorldMatrix = cameraToWorldMatrix.transpose;

            Vector3 cameraToWorldMatrixAxisX = new Vector3(cameraToWorldMatrix.m00, cameraToWorldMatrix.m10, cameraToWorldMatrix.m20);
            Vector3 cameraToWorldMatrixAxisY = new Vector3(cameraToWorldMatrix.m01, cameraToWorldMatrix.m11, cameraToWorldMatrix.m21);
            Vector3 cameraToWorldMatrixAxisZ = new Vector3(cameraToWorldMatrix.m02, cameraToWorldMatrix.m12, cameraToWorldMatrix.m22);

            Vector3 front = cameraToWorldMatrixAxisZ;

            Vector3 worldUp = Vector3.up;
            Vector3 cross = Vector3.Cross(front, worldUp);
            billboardTangent = !Mathf.Approximately(cross.sqrMagnitude, 0.0f)
                ? cross.normalized
                : cameraToWorldMatrixAxisX;

            billboardNormal = Vector3.Cross(worldUp, billboardTangent);
            billboardNormal = !Mathf.Approximately(billboardNormal.sqrMagnitude, 0.0f)
                ? billboardNormal.normalized
                : cameraToWorldMatrixAxisY;

            // SpeedTree generates billboards starting from looking towards X- and rotates counter clock-wisely
            Vector3 worldRight = new Vector3(0, 0, 1);
            // signed angle is calculated on X-Z plane
            float s = worldRight.x * billboardTangent.z - worldRight.z * billboardTangent.x;
            float c = worldRight.x * billboardTangent.x + worldRight.z * billboardTangent.z;
            cameraXZAngle = Mathf.Atan2(s, c);

            // convert to [0,2PI)
            if (cameraXZAngle < 0)
                cameraXZAngle += 2 * Mathf.PI;
        }

        private void SetPerCameraClippingPlaneProperties(RasterCommandBuffer cmd, UniversalCameraData cameraData)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            SetPerCameraClippingPlaneProperties(cmd, in cameraData, cameraData.IsCameraProjectionMatrixFlipped());
            #pragma warning restore CS0618
        }

        private void SetPerCameraClippingPlaneProperties(RasterCommandBuffer cmd, in UniversalCameraData cameraData, bool isTargetFlipped)
        {
            Matrix4x4 projectionMatrix = cameraData.GetGPUProjectionMatrix(isTargetFlipped);
            Matrix4x4 viewMatrix = cameraData.GetViewMatrix();

            Matrix4x4 viewProj = CoreMatrixUtils.MultiplyProjectionMatrix(projectionMatrix, viewMatrix, cameraData.camera.orthographic);
            Plane[] planes = s_Planes;
            GeometryUtility.CalculateFrustumPlanes(viewProj, planes);

            Vector4[] cameraWorldClipPlanes = s_VectorPlanes;
            for (int i = 0; i < planes.Length; ++i)
                cameraWorldClipPlanes[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);

            cmd.SetGlobalVectorArray(ShaderPropertyId.cameraWorldClipPlanes, cameraWorldClipPlanes);
        }

        /// <summary>
        /// Set shader time variables as described in https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="time">Time.</param>
        /// <param name="deltaTime">Delta time.</param>
        /// <param name="smoothDeltaTime">Smooth delta time.</param>
        static void SetShaderTimeValues(IBaseCommandBuffer cmd, float time, float deltaTime, float smoothDeltaTime)
        {
            float timeEights = time / 8f;
            float timeFourth = time / 4f;
            float timeHalf = time / 2f;

            float lastTime = time - ShaderUtils.PersistentDeltaTime;

            // Time values
            Vector4 timeVector = time * new Vector4(1f / 20f, 1f, 2f, 3f);
            Vector4 sinTimeVector = new Vector4(Mathf.Sin(timeEights), Mathf.Sin(timeFourth), Mathf.Sin(timeHalf), Mathf.Sin(time));
            Vector4 cosTimeVector = new Vector4(Mathf.Cos(timeEights), Mathf.Cos(timeFourth), Mathf.Cos(timeHalf), Mathf.Cos(time));
            Vector4 deltaTimeVector = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
            Vector4 timeParametersVector = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);
            Vector4 lastTimeParametersVector = new Vector4(lastTime, Mathf.Sin(lastTime), Mathf.Cos(lastTime), 0.0f);

            cmd.SetGlobalVector(ShaderPropertyId.time, timeVector);
            cmd.SetGlobalVector(ShaderPropertyId.sinTime, sinTimeVector);
            cmd.SetGlobalVector(ShaderPropertyId.cosTime, cosTimeVector);
            cmd.SetGlobalVector(ShaderPropertyId.deltaTime, deltaTimeVector);
            cmd.SetGlobalVector(ShaderPropertyId.timeParameters, timeParametersVector);
            cmd.SetGlobalVector(ShaderPropertyId.lastTimeParameters, lastTimeParametersVector);
        }

        /// <summary>
        /// Returns the camera color target for this renderer.
        /// It's only valid to call cameraColorTarget in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete("Use cameraColorTargetHandle", true)]
        public RenderTargetIdentifier cameraColorTarget => throw new NotSupportedException("cameraColorTarget has been deprecated. Use cameraColorTargetHandle instead");

        /// <summary>
        /// Returns the camera color target for this renderer.
        /// It's only valid to call cameraColorTargetHandle in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public RTHandle cameraColorTargetHandle
        {
            get
            {
                if (!m_IsPipelineExecuting)
                {
                    Debug.LogError("You can only call cameraColorTargetHandle inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
                    return null;
                }

                return m_CameraColorTarget;
            }
        }


        /// <summary>
        /// Returns the frontbuffer color target. Returns null if not implemented by the renderer.
        /// It's only valid to call GetCameraColorFrontBuffer in the scope of <c>ScriptableRenderPass</c>.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        virtual internal RTHandle GetCameraColorFrontBuffer(CommandBuffer cmd)
        {
            return null;
        }


        /// <summary>
        /// Returns the backbuffer color target. Returns null if not implemented by the renderer.
        /// It's only valid to call GetCameraColorBackBuffer in the scope of <c>ScriptableRenderPass</c>.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        virtual internal RTHandle GetCameraColorBackBuffer(CommandBuffer cmd)
        {
            return null;
        }

        /// <summary>
        /// Returns the camera depth target for this renderer.
        /// It's only valid to call cameraDepthTarget in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete("Use cameraDepthTargetHandle", true)]
        public RenderTargetIdentifier cameraDepthTarget => throw new NotSupportedException("cameraDepthTarget has been deprecated. Use cameraDepthTargetHandle instead");

        /// <summary>
        /// Returns the camera depth target for this renderer.
        /// It's only valid to call cameraDepthTargetHandle in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public RTHandle cameraDepthTargetHandle
        {
            get
            {
                if (!m_IsPipelineExecuting)
                {
                    Debug.LogError("You can only call cameraDepthTargetHandle inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
                    return null;
                }

                return m_CameraDepthTarget;
            }
        }

        /// <summary>
        /// Returns a list of renderer features added to this renderer.
        /// <seealso cref="ScriptableRendererFeature"/>
        /// </summary>
        protected List<ScriptableRendererFeature> rendererFeatures
        {
            get => m_RendererFeatures;
        }

        /// <summary>
        /// Returns a list of render passes scheduled to be executed by this renderer.
        /// <seealso cref="ScriptableRenderPass"/>
        /// </summary>
        protected List<ScriptableRenderPass> activeRenderPassQueue
        {
            get => m_ActiveRenderPassQueue;
        }

        /// <summary>
        /// Supported rendering features by this renderer.
        /// <see cref="SupportedRenderingFeatures"/>
        /// </summary>
        public RenderingFeatures supportedRenderingFeatures { get; set; } = new RenderingFeatures();

        /// <summary>
        /// List of unsupported Graphics APIs for this renderer.
        /// <see cref="unsupportedGraphicsDeviceTypes"/>
        /// </summary>
        public GraphicsDeviceType[] unsupportedGraphicsDeviceTypes { get; set; } = new GraphicsDeviceType[0];

        static class RenderPassBlock
        {
            // Executes render passes that are inputs to the main rendering
            // but don't depend on camera state. They all render in monoscopic mode. f.ex, shadow maps.
            public static readonly int BeforeRendering = 0;

            // Main bulk of render pass execution. They required camera state to be properly set
            // and when enabled they will render in stereo.
            public static readonly int MainRenderingOpaque = 1;
            public static readonly int MainRenderingTransparent = 2;

            // Execute after Post-processing.
            public static readonly int AfterRendering = 3;
        }

        private StoreActionsOptimization m_StoreActionsOptimizationSetting = StoreActionsOptimization.Auto;
        private static bool m_UseOptimizedStoreActions = false;

        const int k_RenderPassBlockCount = 4;

        /// <summary>
        /// The RTHandle for the Camera Target.
        /// </summary>
        protected static readonly RTHandle k_CameraTarget = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);

        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>(32);
        List<ScriptableRendererFeature> m_RendererFeatures = new List<ScriptableRendererFeature>(10);

        RTHandle m_CameraColorTarget;
        RTHandle m_CameraDepthTarget;
        RTHandle m_CameraResolveTarget;

        bool m_FirstTimeCameraColorTargetIsBound = true; // flag used to track when m_CameraColorTarget should be cleared (if necessary), as well as other special actions only performed the first time m_CameraColorTarget is bound as a render target
        bool m_FirstTimeCameraDepthTargetIsBound = true; // flag used to track when m_CameraDepthTarget should be cleared (if necessary), the first time m_CameraDepthTarget is bound as a render target

        // The pipeline can only guarantee the camera target texture are valid when the pipeline is executing.
        // Trying to access the camera target before or after might be that the pipeline texture have already been disposed.
        bool m_IsPipelineExecuting = false;

        // Temporary variable to disable custom passes using render pass ( due to it potentially breaking projects with custom render features )
        // To enable it - override SupportsNativeRenderPass method in the feature and return true
        internal bool disableNativeRenderPassInFeatures = false;

        internal bool useRenderPassEnabled = false;
        // Used to cache nameID of m_ActiveColorAttachments for CoreUtils without allocating arrays at each call
        static RenderTargetIdentifier[] m_ActiveColorAttachmentIDs = new RenderTargetIdentifier[8];
        static RTHandle[] m_ActiveColorAttachments = new RTHandle[8];
        static RTHandle m_ActiveDepthAttachment;

        ContextContainer m_frameData = new();
        internal ContextContainer frameData => m_frameData;

        private static RenderBufferStoreAction[] m_ActiveColorStoreActions = new RenderBufferStoreAction[]
        {
            RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store,
            RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store
        };

        private static RenderBufferStoreAction m_ActiveDepthStoreAction = RenderBufferStoreAction.Store;

        // CommandBuffer.SetRenderTarget(RenderTargetIdentifier[] colors, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice);
        // called from CoreUtils.SetRenderTarget will issue a warning assert from native c++ side if "colors" array contains some invalid RTIDs.
        // To avoid that warning assert we trim the RenderTargetIdentifier[] arrays we pass to CoreUtils.SetRenderTarget.
        // To avoid re-allocating a new array every time we do that, we re-use one of these arrays for both RTHandles and RenderTargetIdentifiers:
        static RenderTargetIdentifier[][] m_TrimmedColorAttachmentCopyIDs =
        {
            Array.Empty<RenderTargetIdentifier>(), // only used to make indexing code easier to read
            new RenderTargetIdentifier[1],
            new RenderTargetIdentifier[2],
            new RenderTargetIdentifier[3],
            new RenderTargetIdentifier[4],
            new RenderTargetIdentifier[5],
            new RenderTargetIdentifier[6],
            new RenderTargetIdentifier[7],
            new RenderTargetIdentifier[8],
        };
        static RTHandle[][] m_TrimmedColorAttachmentCopies =
        {
            Array.Empty<RTHandle>(), // only used to make indexing code easier to read
            new RTHandle[1],
            new RTHandle[2],
            new RTHandle[3],
            new RTHandle[4],
            new RTHandle[5],
            new RTHandle[6],
            new RTHandle[7],
            new RTHandle[8],
        };

        private static Plane[] s_Planes = new Plane[6];
        private static Vector4[] s_VectorPlanes = new Vector4[6];

        internal bool useDepthPriming { get; set; } = false;

        internal bool stripShadowsOffVariants { get; set; } = false;

        internal bool stripAdditionalLightOffVariants { get; set; } = false;

        /// <summary>
        /// Creates a new <c>ScriptableRenderer</c> instance.
        /// </summary>
        /// <param name="data">The <c>ScriptableRendererData</c> data to initialize the renderer.</param>
        /// <seealso cref="ScriptableRendererData"/>
        public ScriptableRenderer(ScriptableRendererData data)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            DebugHandler = new DebugHandler();
#endif
            profilingExecute = new ProfilingSampler($"{nameof(ScriptableRenderer)}.{nameof(ScriptableRenderer.Execute)}: {data.name}");

            foreach (var feature in data.rendererFeatures)
            {
                if (feature == null)
                    continue;

                feature.Create();
                m_RendererFeatures.Add(feature);
            }

            ResetNativeRenderPassFrameData();
            useRenderPassEnabled = data.useNativeRenderPass;
            Clear(CameraRenderType.Base);
            m_ActiveRenderPassQueue.Clear();

            if (UniversalRenderPipeline.asset)
            {
                m_StoreActionsOptimizationSetting = UniversalRenderPipeline.asset.storeActionsOptimization;
            }

            m_UseOptimizedStoreActions = m_StoreActionsOptimizationSetting != StoreActionsOptimization.Store;
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// Cleans up resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            // Dispose all renderer features...
            for (int i = 0; i < m_RendererFeatures.Count; ++i)
            {
                if (rendererFeatures[i] == null)
                    continue;

                rendererFeatures[i].Dispose();
            }

            Dispose(true);
            hasReleasedRTs = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called by Dispose().
        /// Override this function to clean up resources in your renderer.
        /// Be sure to call this base dispose in your overridden function to free resources allocated by the base.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            DebugHandler?.Dispose();
        }

        internal virtual void ReleaseRenderTargets()
        {
        }

        /// <summary>
        /// Configures the camera target.
        /// </summary>
        /// <param name="colorTarget">Camera color target. Pass BuiltinRenderTextureType.CameraTarget if rendering to backbuffer.</param>
        /// <param name="depthTarget">Camera depth target. Pass BuiltinRenderTextureType.CameraTarget if color has depth or rendering to backbuffer.</param>
        [Obsolete("Use RTHandles for colorTarget and depthTarget", true)]
        public void ConfigureCameraTarget(RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
        {
            throw new NotSupportedException("ConfigureCameraTarget with RenderTargetIdentifier has been deprecated. Use it with RTHandles instead");
        }

        /// <summary>
        /// Configures the camera target.
        /// </summary>
        /// <param name="colorTarget">Camera color target. Pass k_CameraTarget if rendering to backbuffer.</param>
        /// <param name="depthTarget">Camera depth target. Pass k_CameraTarget if color has depth or rendering to backbuffer.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void ConfigureCameraTarget(RTHandle colorTarget, RTHandle depthTarget)
        {
            m_CameraColorTarget = colorTarget;
            m_CameraDepthTarget = depthTarget;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void ConfigureCameraTarget(RTHandle colorTarget, RTHandle depthTarget, RTHandle resolveTarget)
        {
            m_CameraColorTarget = colorTarget;
            m_CameraDepthTarget = depthTarget;
            m_CameraResolveTarget = resolveTarget;
        }

        // This should be removed when early camera color target assignment is removed.
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal void ConfigureCameraColorTarget(RTHandle colorTarget)
        {
            m_CameraColorTarget = colorTarget;
        }

        /// <summary>
        /// Configures the render passes that will execute for this renderer.
        /// This method is called per-camera every frame.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        /// <seealso cref="ScriptableRenderPass"/>
        /// <seealso cref="ScriptableRendererFeature"/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public abstract void Setup(ScriptableRenderContext context, ref RenderingData renderingData);

        /// <summary>
        /// Override this method to implement the lighting setup for the renderer. You can use this to
        /// compute and upload light CBUFFER for example.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public virtual void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        /// <summary>
        /// Override this method to configure the culling parameters for the renderer. You can use this to configure if
        /// lights should be culled per-object or the maximum shadow distance for example.
        /// </summary>
        /// <param name="cullingParameters">Use this to change culling parameters used by the render pipeline.</param>
        /// <param name="cameraData">Current render state information.</param>
        public virtual void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
        }

        /// <summary>
        /// Called upon finishing rendering the camera stack. You can release any resources created by the renderer here.
        /// </summary>
        /// <param name="cmd"></param>
        public virtual void FinishRendering(CommandBuffer cmd)
        {
        }

        /// <summary>
        /// Override this method to initialize before recording the render graph, such as resources.
        /// </summary>
        public virtual void OnBeginRenderGraphFrame()
        {
        }

        /// <summary>
        /// Override this method to record the RenderGraph passes to be used by the RenderGraph render path.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        internal virtual void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
        }

        /// <summary>
        /// Override this method to cleanup things after recording the render graph, such as resources.
        /// </summary>
        public virtual void OnEndRenderGraphFrame()
        {
        }

        private void InitRenderGraphFrame(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddUnsafePass<PassData>(Profiling.initRenderGraphFrame.name, out var passData,
                Profiling.initRenderGraphFrame))
            {
                passData.renderer = this;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext rgContext) =>
                {
                    UnsafeCommandBuffer cmd = rgContext.cmd;
#if UNITY_EDITOR
                    float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
                    float time = Time.time;
#endif
                    float deltaTime = Time.deltaTime;
                    float smoothDeltaTime = Time.smoothDeltaTime;

                    ClearRenderingState(cmd);
                    SetShaderTimeValues(cmd, time, deltaTime, smoothDeltaTime);
                });
            }
        }

        private class VFXProcessCameraPassData
        {
            internal UniversalRenderingData renderingData;
            internal Camera camera;
            internal VFX.VFXCameraXRSettings cameraXRSettings;
            internal XRPass xrPass;
        };

        internal void ProcessVFXCameraCommand(RenderGraph renderGraph)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            XRPass xr = cameraData.xr;

            using (var builder = renderGraph.AddUnsafePass<VFXProcessCameraPassData>("ProcessVFXCameraCommand", out var passData,
                       Profiling.vfxProcessCamera))
            {
                passData.camera = cameraData.camera;
                passData.renderingData = renderingData;

                passData.cameraXRSettings.viewTotal = xr.enabled ? 2u : 1u;
                passData.cameraXRSettings.viewCount = xr.enabled ? (uint)xr.viewCount : 1u;
                passData.cameraXRSettings.viewOffset = (uint)xr.multipassId;
                passData.xrPass = xr.enabled ? xr : null;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((VFXProcessCameraPassData data, UnsafeGraphContext context) =>
                {
                    if (data.xrPass != null)
                        data.xrPass.StartSinglePass(context.cmd);

                    //Triggers dispatch per camera, all global parameters should have been setup at this stage.
                    CommandBufferHelpers.VFXManager_ProcessCameraCommand(data.camera, context.cmd, data.cameraXRSettings, data.renderingData.cullResults);

                    if (data.xrPass != null)
                        data.xrPass.StopSinglePass(context.cmd);
                });

            }
        }
        internal void SetupRenderGraphCameraProperties(RenderGraph renderGraph, bool isTargetBackbuffer)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(Profiling.setupCamera.name, out var passData,
                Profiling.setupCamera))
            {
                passData.renderer = this;
                passData.cameraData = frameData.Get<UniversalCameraData>();
                passData.cameraTargetSizeCopy = new Vector2Int(passData.cameraData.cameraTargetDescriptor.width, passData.cameraData.cameraTargetDescriptor.height);
                passData.isTargetBackbuffer = isTargetBackbuffer;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    bool yFlip = !SystemInfo.graphicsUVStartsAtTop || data.isTargetBackbuffer;

                    // This is still required because of the following reasons:
                    // - Camera billboard properties.
                    // - Camera frustum planes: unity_CameraWorldClipPlanes[6]
                    // - _ProjectionParams.x logic is deep inside GfxDevice
                    // NOTE: The only reason we have to call this here and not at the beginning (before shadows)
                    // is because this need to be called for each eye in multi pass VR.
                    // The side effect is that this will override some shader properties we already setup and we will have to
                    // reset them.
                    if (data.cameraData.renderType == CameraRenderType.Base)
                    {
                        context.cmd.SetupCameraProperties(data.cameraData.camera);
                        data.renderer.SetPerCameraShaderVariables(context.cmd, data.cameraData, data.cameraTargetSizeCopy, !yFlip);
                    }
                    else
                    {
                        // Set new properties
                        data.renderer.SetPerCameraShaderVariables(context.cmd, data.cameraData, data.cameraTargetSizeCopy, !yFlip);
                        data.renderer.SetPerCameraClippingPlaneProperties(context.cmd, in data.cameraData, !yFlip);
                        data.renderer.SetPerCameraBillboardProperties(context.cmd, data.cameraData);
                    }

#if UNITY_EDITOR
                    float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
                    float time = Time.time;
#endif
                    float deltaTime = Time.deltaTime;
                    float smoothDeltaTime = Time.smoothDeltaTime;

                    // Reset shader time variables as they were overridden in SetupCameraProperties. If we don't do it we might have a mismatch between shadows and main rendering
                    SetShaderTimeValues(context.cmd, time, deltaTime, smoothDeltaTime);
                });
            }
        }


        private class DrawGizmosPassData
        {
            public RendererListHandle gizmoRenderList;
            public TextureHandle color;
            public TextureHandle depth;
        };

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="color"></param>
        /// <param name="depth"></param>
        /// <param name="gizmoSubset"></param>
        /// <param name="renderingData"></param>
        internal void DrawRenderGraphGizmos(RenderGraph renderGraph, ContextContainer frameData, TextureHandle color, TextureHandle depth, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!Handles.ShouldRenderGizmos() || cameraData.camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                return;

            // We cannot draw gizmo rendererlists from an raster pass as the gizmo rendering triggers the  MonoBehaviour.OnDrawGizmos or MonoBehaviour.OnDrawGizmosSelected callbacks that could run arbitrary graphics code
            // like SetRenderTarget, texture and resource loading, ...
            using (var builder = renderGraph.AddUnsafePass<DrawGizmosPassData>("Draw Gizmos Pass", out var passData,
                Profiling.drawGizmos))
            {
                builder.UseTexture(color, AccessFlags.Write);
                builder.UseTexture(depth, AccessFlags.ReadWrite);

                passData.gizmoRenderList = renderGraph.CreateGizmoRendererList(cameraData.camera, gizmoSubset);
                passData.color = color;
                passData.depth = depth;
                builder.UseRendererList(passData.gizmoRenderList);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DrawGizmosPassData data, UnsafeGraphContext rgContext) =>
                {
                    using (new ProfilingScope(rgContext.cmd, Profiling.drawGizmos))
                    {
                        rgContext.cmd.SetRenderTarget(data.color, data.depth);
                        rgContext.cmd.DrawRendererList(data.gizmoRenderList);
                    }
                });
            }
#endif
        }

        private class DrawWireOverlayPassData
        {
            public RendererListHandle wireOverlayList;
        };

        internal void DrawRenderGraphWireOverlay(RenderGraph renderGraph, ContextContainer frameData, TextureHandle color)
        {
#if UNITY_EDITOR
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!cameraData.isSceneViewCamera)
                return;

            using (var builder = renderGraph.AddRasterRenderPass<DrawWireOverlayPassData>(Profiling.drawWireOverlay.name, out var passData,
                       Profiling.drawWireOverlay))
            {
                builder.SetRenderAttachment(color, 0, AccessFlags.Write);

                passData.wireOverlayList = renderGraph.CreateWireOverlayRendererList(cameraData.camera);
                builder.UseRendererList(passData.wireOverlayList);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (DrawWireOverlayPassData data, RasterGraphContext rgContext) =>
                {
                    using (new ProfilingScope(rgContext.cmd, Profiling.drawWireOverlay))
                    {
                        rgContext.cmd.DrawRendererList(data.wireOverlayList);
                    }
                });
            }
#endif
        }

        private class BeginXRPassData
        {
            internal UniversalCameraData cameraData;
        };

        internal void BeginRenderGraphXRRendering(RenderGraph renderGraph)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.xr.enabled)
                return;

            bool isDefaultXRViewport = XRSystem.GetRenderViewportScale() == 1.0f;
            // For untethered XR, intermediate pass' foveation is currenlty unsupported with non-default viewport.
            // Must be configured during the recording timeline before adding other XR intermediate passes.
            cameraData.xrUniversal.canFoveateIntermediatePasses = !PlatformAutoDetect.isXRMobile || isDefaultXRViewport;

            using (var builder = renderGraph.AddRasterRenderPass<BeginXRPassData>("BeginXRRendering", out var passData,
                Profiling.beginXRRendering))
            {
                passData.cameraData = cameraData;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((BeginXRPassData data, RasterGraphContext context) =>
                {
                    if (data.cameraData.xr.enabled)
                    {
                        if (data.cameraData.xrUniversal.isLateLatchEnabled)
                            data.cameraData.xrUniversal.canMarkLateLatch = true;

                        data.cameraData.xr.StartSinglePass(context.cmd);
                        if (data.cameraData.xr.supportsFoveatedRendering)
                        {
                            context.cmd.ConfigureFoveatedRendering(data.cameraData.xr.foveatedRenderingInfo);

                            if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster))
                                context.cmd.SetKeyword(ShaderGlobalKeywords.FoveatedRenderingNonUniformRaster, true);
                        }
                    }
                });
            }
#endif
        }

        private class EndXRPassData
        {
            public UniversalCameraData cameraData;
        };

        internal void EndRenderGraphXRRendering(RenderGraph renderGraph)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (!cameraData.xr.enabled)
                return;

            using (var builder = renderGraph.AddRasterRenderPass<EndXRPassData>("EndXRRendering", out var passData,
                Profiling.endXRRendering))
            {
                passData.cameraData = cameraData;

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((EndXRPassData data, RasterGraphContext context) =>
                {
                    if (data.cameraData.xr.enabled)
                    {
                        data.cameraData.xr.StopSinglePass(context.cmd);
                    }

                    if (XRSystem.foveatedRenderingCaps != FoveatedRenderingCaps.None)
                    {
                        if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster))
                            context.cmd.SetKeyword(ShaderGlobalKeywords.FoveatedRenderingNonUniformRaster, false);

                        context.cmd.ConfigureFoveatedRendering(IntPtr.Zero);
                    }
                });
            }
#endif
        }

        private class DummyData
        {
        };

        private void SetEditorTarget(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddUnsafePass<DummyData>("SetEditorTarget", out var passData,
                Profiling.setEditorTarget))
            {
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DummyData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, // color
                        RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare); // depth
                });
            }
        }

        private class PassData
        {
            internal ScriptableRenderer renderer;
            internal UniversalCameraData cameraData;
            internal bool isTargetBackbuffer;

            // The size of the camera target changes during the frame so we must make a copy of it here to preserve its record-time value.
            internal Vector2Int cameraTargetSizeCopy;
        };


        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        internal void RecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RecordRenderGraph)))
            {
                OnBeginRenderGraphFrame();

                using (new ProfilingScope(Profiling.sortRenderPasses))
                {
                    // Sort the render pass queue
                    SortStable(m_ActiveRenderPassQueue);
                }

                InitRenderGraphFrame(renderGraph);

                using (new ProfilingScope(Profiling.recordRenderGraph))
                {
                    OnRecordRenderGraph(renderGraph, context);
                }

                OnEndRenderGraphFrame();

                // The editor scene view still relies on some builtin passes (i.e. drawing the scene grid). The builtin
                // passes are not explicitly setting RTs and rely on the last active render target being set. Unfortunately
                // this does not play nice with the NRP RG path, since we don't use the SetRenderTarget API anymore.
                // For this reason, as a workaround, in editor scene view we set explicitly set the RT to SceneViewRT.
                // TODO: this will go away once we remove the builtin dependencies and implement the grid in SRP.
#if UNITY_EDITOR
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (cameraData.isSceneViewCamera)
                    SetEditorTarget(renderGraph);
#endif
            }
        }

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        internal void FinishRenderGraphRendering(CommandBuffer cmd)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            OnFinishRenderGraphRendering(cmd);
            InternalFinishRenderingCommon(cmd, cameraData.resolveFinalTarget);
        }

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        internal virtual void OnFinishRenderGraphRendering(CommandBuffer cmd)
        {
        }

        internal void RecordCustomRenderGraphPassesInEventRange(RenderGraph renderGraph, RenderPassEvent eventStart, RenderPassEvent eventEnd)
        {
            // Only iterate over the active pass queue if we have a non-empty range
            if (eventStart != eventEnd)
            {
                foreach (ScriptableRenderPass pass in m_ActiveRenderPassQueue)
                {
                    if (pass.renderPassEvent >= eventStart && pass.renderPassEvent < eventEnd)
                        pass.RecordRenderGraph(renderGraph, m_frameData);
                }
            }
        }

        internal void CalculateSplitEventRange(RenderPassEvent startInjectionPoint, RenderPassEvent targetEvent, out RenderPassEvent startEvent, out RenderPassEvent splitEvent, out RenderPassEvent endEvent)
        {
            int range = ScriptableRenderPass.GetRenderPassEventRange(startInjectionPoint);

            startEvent = startInjectionPoint;
            endEvent = startEvent + range;

            splitEvent = (RenderPassEvent)Math.Clamp((int)targetEvent, (int)startEvent, (int)endEvent);
        }

        internal void RecordCustomRenderGraphPasses(RenderGraph renderGraph, RenderPassEvent startInjectionPoint, RenderPassEvent endInjectionPoint)
        {
            int range = ScriptableRenderPass.GetRenderPassEventRange(endInjectionPoint);

            RecordCustomRenderGraphPassesInEventRange(renderGraph, startInjectionPoint, endInjectionPoint + range);
        }

        internal void RecordCustomRenderGraphPasses(RenderGraph renderGraph, RenderPassEvent injectionPoint)
        {
            RecordCustomRenderGraphPasses(renderGraph, injectionPoint, injectionPoint);
        }

        // ScriptableRenderPass if executed in a critical point (such as in between Deferred and GBuffer) has to have
        // interruptFramebufferFetchEvent set to actually interrupt it so we could fall back to non framebuffer fetch path
        internal bool InterruptFramebufferFetch(FramebufferFetchEvent fetchEvent, RenderPassEvent startInjectionPoint, RenderPassEvent endInjectionPoint)
        {
            int range = ScriptableRenderPass.GetRenderPassEventRange(endInjectionPoint);
            int nextValue = (int) endInjectionPoint + range;

            foreach (ScriptableRenderPass pass in m_ActiveRenderPassQueue)
            {
                if (pass.renderPassEvent >= startInjectionPoint && (int) pass.renderPassEvent < nextValue)
                    switch (fetchEvent)
                    {
                        case FramebufferFetchEvent.FetchGbufferInDeferred:
                            if (pass.breakGBufferAndDeferredRenderPass)
                                return true;
                            break;
                        default:
                            continue;
                    }
            }
            return false;
        }

        internal void SetPerCameraProperties(ScriptableRenderContext context, UniversalCameraData cameraData, Camera camera,
            CommandBuffer cmd)
        {
            if (cameraData.renderType == CameraRenderType.Base)
            {
                context.SetupCameraProperties(camera);
                SetPerCameraShaderVariables(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData);
            }
            else
            {
                // Set new properties
                SetPerCameraShaderVariables(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData);
                SetPerCameraClippingPlaneProperties(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData);
                SetPerCameraBillboardProperties(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData);
            }
        }

        /// <summary>
        /// Execute the enqueued render passes. This automatically handles editor and stereo rendering.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;
            hasReleasedRTs = false;
            m_IsPipelineExecuting = true;
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            Camera camera = cameraData.camera;

            // Let renderer features call their own setup functions when targets are valid
            if (rendererFeatures.Count != 0 && !renderingData.cameraData.isPreviewCamera)
                SetupRenderPasses(in renderingData);

            CommandBuffer cmd = renderingData.commandBuffer;

            // TODO: move skybox code from C++ to URP in order to remove the call to context.Submit() inside DrawSkyboxPass
            // Until then, we can't use nested profiling scopes with XR multipass
            CommandBuffer cmdScope = renderingData.cameraData.xr.enabled ? null : cmd;

            using (new ProfilingScope(cmdScope, profilingExecute))
            {
                InternalStartRendering(context, ref renderingData);

                // Cache the time for after the call to `SetupCameraProperties` and set the time variables in shader
                // For now we set the time variables per camera, as we plan to remove `SetupCameraProperties`.
                // Setting the time per frame would take API changes to pass the variable to each camera render.
                // Once `SetupCameraProperties` is gone, the variable should be set higher in the call-stack.
#if UNITY_EDITOR
                float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
                float time = Time.time;
#endif
                float deltaTime = Time.deltaTime;
                float smoothDeltaTime = Time.smoothDeltaTime;

                // Initialize Camera Render State
                ClearRenderingState(CommandBufferHelpers.GetRasterCommandBuffer(cmd));
                SetShaderTimeValues(CommandBufferHelpers.GetRasterCommandBuffer(cmd), time, deltaTime, smoothDeltaTime);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                using (new ProfilingScope(Profiling.sortRenderPasses))
                {
                    // Sort the render pass queue
                    SortStable(m_ActiveRenderPassQueue);

                }

                using (new ProfilingScope(Profiling.RenderPass.configure))
                {
                    foreach (var pass in activeRenderPassQueue)
                    {
                        // Disable obsolete warning for internal usage
                        #pragma warning disable CS0618
                        pass.Configure(cmd, cameraData.cameraTargetDescriptor);
                        #pragma warning restore CS0618
                    }

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                SetupNativeRenderPassFrameData(cameraData, useRenderPassEnabled);

                using var renderBlocks = new RenderBlocks(m_ActiveRenderPassQueue);

                using (new ProfilingScope(Profiling.setupLights))
                {
                    SetupLights(context, ref renderingData);
                }

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                using (new ProfilingScope(Profiling.setupCamera))
                {
                    //Camera variables need to be setup for the VFXManager.ProcessCameraCommand to work properly.
                    //VFXManager.ProcessCameraCommand needs to be called before any rendering (incl. shadows)
                    SetPerCameraProperties(context, cameraData, camera, cmd);

                    VFX.VFXCameraXRSettings cameraXRSettings;
                    cameraXRSettings.viewTotal = cameraData.xr.enabled ? 2u : 1u;
                    cameraXRSettings.viewCount = cameraData.xr.enabled ? (uint)cameraData.xr.viewCount : 1u;
                    cameraXRSettings.viewOffset = (uint)cameraData.xr.multipassId;

                    if (cameraData.xr.enabled)
                        cameraData.xr.StartSinglePass(cmd);

                    VFX.VFXManager.ProcessCameraCommand(camera, cmd, cameraXRSettings, renderingData.cullResults);

                    if (cameraData.xr.enabled)
                        cameraData.xr.StopSinglePass(cmd);
                }
#endif

                // Before Render Block. This render blocks always execute in mono rendering.
                // Camera is not setup.
                // Used to render input textures like shadowmaps.
                if (renderBlocks.GetLength(RenderPassBlock.BeforeRendering) > 0)
                {
                    // TODO: Separate command buffers per pass break the profiling scope order/hierarchy.
                    // If a single buffer is used and passed as a param to passes,
                    // put all of the "block" scopes back into the command buffer. (null -> cmd)
                    using var profScope = new ProfilingScope(Profiling.RenderBlock.beforeRendering);
                    ExecuteBlock(RenderPassBlock.BeforeRendering, in renderBlocks, context, ref renderingData);
                }

                using (new ProfilingScope(Profiling.setupCamera))
                {
                    // This is still required because of the following reasons:
                    // - Camera billboard properties.
                    // - Camera frustum planes: unity_CameraWorldClipPlanes[6]
                    // - _ProjectionParams.x logic is deep inside GfxDevice
                    // NOTE: The only reason we have to call this here and not at the beginning (before shadows)
                    // is because this need to be called for each eye in multi pass VR.
                    // The side effect is that this will override some shader properties we already setup and we will have to
                    // reset them.
                    SetPerCameraProperties(context, cameraData, camera, cmd);

                    // Reset shader time variables as they were overridden in SetupCameraProperties. If we don't do it we might have a mismatch between shadows and main rendering
                    SetShaderTimeValues(CommandBufferHelpers.GetRasterCommandBuffer(cmd), time, deltaTime, smoothDeltaTime);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                BeginXRRendering(cmd, context, ref renderingData.cameraData);

                // In the opaque and transparent blocks the main rendering executes.

                // Opaque blocks...
                if (renderBlocks.GetLength(RenderPassBlock.MainRenderingOpaque) > 0)
                {
                    // TODO: Separate command buffers per pass break the profiling scope order/hierarchy.
                    // If a single buffer is used (passed as a param) for passes,
                    // put all of the "block" scopes back into the command buffer. (i.e. null -> cmd)
                    using var profScope = new ProfilingScope(Profiling.RenderBlock.mainRenderingOpaque);
                    ExecuteBlock(RenderPassBlock.MainRenderingOpaque, in renderBlocks, context, ref renderingData);
                }

                // Transparent blocks...
                if (renderBlocks.GetLength(RenderPassBlock.MainRenderingTransparent) > 0)
                {
                    using var profScope = new ProfilingScope(Profiling.RenderBlock.mainRenderingTransparent);
                    ExecuteBlock(RenderPassBlock.MainRenderingTransparent, in renderBlocks, context, ref renderingData);
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                // Late latching is not supported after this point in the frame
                if (cameraData.xr.enabled)
                    cameraData.xrUniversal.canMarkLateLatch = false;
#endif

                // Draw Gizmos...
                if (drawGizmos)
                {
                    DrawGizmos(context, camera, GizmoSubset.PreImageEffects, ref renderingData);
                }

                // In this block after rendering drawing happens, e.g, post processing, video player capture.
                if (renderBlocks.GetLength(RenderPassBlock.AfterRendering) > 0)
                {
                    using var profScope = new ProfilingScope(Profiling.RenderBlock.afterRendering);
                    ExecuteBlock(RenderPassBlock.AfterRendering, in renderBlocks, context, ref renderingData);
                }

                EndXRRendering(cmd, context, ref renderingData.cameraData);

                DrawWireOverlay(context, camera);

                if (drawGizmos)
                {
                    DrawGizmos(context, camera, GizmoSubset.PostImageEffects, ref renderingData);
                }

                InternalFinishRenderingExecute(context, cmd, cameraData.resolveFinalTarget);

                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                {
                    m_ActiveRenderPassQueue[i].m_ColorAttachmentIndices.Dispose();
                    m_ActiveRenderPassQueue[i].m_InputAttachmentIndices.Dispose();
                }
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        /// <summary>
        /// Enqueues a render pass for execution.
        /// </summary>
        /// <param name="pass">Render pass to be enqueued.</param>
        public void EnqueuePass(ScriptableRenderPass pass)
        {
            m_ActiveRenderPassQueue.Add(pass);
            if (disableNativeRenderPassInFeatures)
                pass.useNativeRenderPass = false;
        }

        /// <summary>
        /// Returns a clear flag based on CameraClearFlags.
        /// </summary>
        /// <param name="cameraData">The Camera data.</param>
        /// <returns>A clear flag that tells if color and/or depth should be cleared.</returns>
        /// <seealso cref="CameraData"/>
        protected static ClearFlag GetCameraClearFlag(ref CameraData cameraData)
        {
            var universalCameraData = cameraData.universalCameraData;
            return GetCameraClearFlag(universalCameraData);
        }

        /// <summary>
        /// Returns a clear flag based on CameraClearFlags.
        /// </summary>
        /// <param name="cameraData">The Camera data.</param>
        /// <returns>A clear flag that tells if color and/or depth should be cleared.</returns>
        /// <seealso cref="CameraData"/>
        protected static ClearFlag GetCameraClearFlag(UniversalCameraData cameraData)
        {
            var cameraClearFlags = cameraData.camera.clearFlags;

            // Universal RP doesn't support CameraClearFlags.DepthOnly and CameraClearFlags.Nothing.
            // CameraClearFlags.DepthOnly has the same effect of CameraClearFlags.SolidColor
            // CameraClearFlags.Nothing clears Depth on PC/Desktop and in mobile it clears both
            // depth and color.
            // CameraClearFlags.Skybox clears depth only.

            // Implementation details:
            // Camera clear flags are used to initialize the attachments on the first render pass.
            // ClearFlag is used together with Tile Load action to figure out how to clear the camera render target.
            // In Tile Based GPUs ClearFlag.Depth + RenderBufferLoadAction.DontCare becomes DontCare load action.

            // RenderBufferLoadAction.DontCare in PC/Desktop behaves as not clearing screen
            // RenderBufferLoadAction.DontCare in Vulkan/Metal behaves as DontCare load action
            // RenderBufferLoadAction.DontCare in GLES behaves as glInvalidateBuffer

            // Overlay cameras composite on top of previous ones. They don't clear color.
            // For overlay cameras we check if depth should be cleared on not.
            if (cameraData.renderType == CameraRenderType.Overlay)
                return (cameraData.clearDepth) ? ClearFlag.DepthStencil : ClearFlag.None;

            // Certain debug modes (e.g. wireframe/overdraw modes) require that we override clear flags and clear everything.
            var debugHandler = cameraData.renderer.DebugHandler;
            if (debugHandler != null && debugHandler.IsActiveForCamera(cameraData.isPreviewCamera) && debugHandler.IsScreenClearNeeded)
                return ClearFlag.All;

            // XRTODO: remove once we have visible area of occlusion mesh available
            if (cameraClearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null && cameraData.postProcessEnabled && cameraData.xr.enabled)
                return ClearFlag.All;

            if ((cameraClearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) ||
                cameraClearFlags == CameraClearFlags.Nothing)
            {
                // Clear color if msaa is used. If color is not cleared will alpha to coverage blend with previous frame if alpha clipping is enabled of any opaque objects.
                if (cameraData.cameraTargetDescriptor.msaaSamples > 1)
                {
                    // Sets the clear color to black to make the alpha to coverage blending blend with black when using alpha clipping.
                    cameraData.camera.backgroundColor = Color.black;
                    return ClearFlag.DepthStencil | ClearFlag.Color;
                }
                else
                {
                    return ClearFlag.DepthStencil;
                }
            }

            return ClearFlag.All;
        }

        /// <summary>
        /// Calls <c>OnCull</c> for each feature added to this renderer.
        /// <seealso cref="ScriptableRendererFeature.OnCameraPreCull(ScriptableRenderer, in CameraData)"/>
        /// </summary>
        /// <param name="cameraData">Current render state information.</param>
        internal void OnPreCullRenderPasses(in CameraData cameraData)
        {
            // Add render passes from custom renderer features
            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                if (!rendererFeatures[i].isActive)
                {
                    continue;
                }
                rendererFeatures[i].OnCameraPreCull(this, in cameraData);
            }
        }

        /// <summary>
        /// Calls <c>AddRenderPasses</c> for each feature added to this renderer.
        /// <seealso cref="ScriptableRendererFeature.AddRenderPasses(ScriptableRenderer, ref RenderingData)"/>
        /// </summary>
        /// <param name="renderingData"></param>
        internal void AddRenderPasses(ref RenderingData renderingData)
        {
            using var profScope = new ProfilingScope(Profiling.addRenderPasses);

            // Add render passes from custom renderer features
            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                if (!rendererFeatures[i].isActive)
                {
                    continue;
                }

                if (!rendererFeatures[i].SupportsNativeRenderPass())
                    disableNativeRenderPassInFeatures = true;

                rendererFeatures[i].AddRenderPasses(this, ref renderingData);
                disableNativeRenderPassInFeatures = false;
            }

            // Remove any null render pass that might have been added by user by mistake
            int count = activeRenderPassQueue.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (activeRenderPassQueue[i] == null)
                    activeRenderPassQueue.RemoveAt(i);
            }

            // if any pass was injected, the "automatic" store optimization policy will disable the optimized load actions
            if (count > 0 && m_StoreActionsOptimizationSetting == StoreActionsOptimization.Auto)
                m_UseOptimizedStoreActions = false;
        }

        /// <summary>
        /// Calls <c>Setup</c> for each feature added to this renderer.
        /// <seealso cref="ScriptableRendererFeature.SetupRenderPasses(ScriptableRenderer, in RenderingData)"/>
        /// </summary>
        /// <param name="renderingData"></param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        protected void SetupRenderPasses(in RenderingData renderingData)
        {
            using var profScope = new ProfilingScope(Profiling.setupRenderPasses);

            // Add render passes from custom renderer features
            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                if (!rendererFeatures[i].isActive)
                    continue;

                rendererFeatures[i].SetupRenderPasses(this, in renderingData);
            }
        }

        static void ClearRenderingState(IBaseCommandBuffer cmd)
        {
            using var profScope = new ProfilingScope(Profiling.clearRenderingState);

            // Reset per-camera shader keywords. They are enabled depending on which render passes are executed.
            cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadows, false);
            cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowCascades, false);
            cmd.SetKeyword(ShaderGlobalKeywords.AdditionalLightsVertex, false);
            cmd.SetKeyword(ShaderGlobalKeywords.AdditionalLightsPixel, false);
            cmd.SetKeyword(ShaderGlobalKeywords.ClusterLightLoop, false);
            cmd.SetKeyword(ShaderGlobalKeywords.ForwardPlus, false); // Backward compatibility. Deprecated in 6.1.
            cmd.SetKeyword(ShaderGlobalKeywords.AdditionalLightShadows, false);
            cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeBlending, false);
            cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeBoxProjection, false);
            cmd.SetKeyword(ShaderGlobalKeywords.ReflectionProbeAtlas, false);
            cmd.SetKeyword(ShaderGlobalKeywords.SoftShadows, false);
            cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsLow, false);
            cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsMedium, false);
            cmd.SetKeyword(ShaderGlobalKeywords.SoftShadowsHigh, false);
            cmd.SetKeyword(ShaderGlobalKeywords.MixedLightingSubtractive, false);
            cmd.SetKeyword(ShaderGlobalKeywords.LightmapShadowMixing, false);
            cmd.SetKeyword(ShaderGlobalKeywords.ShadowsShadowMask, false);
            cmd.SetKeyword(ShaderGlobalKeywords.LinearToSRGBConversion, false);
            cmd.SetKeyword(ShaderGlobalKeywords.LightLayers, false);
            cmd.SetGlobalVector(ScreenSpaceAmbientOcclusionPass.s_AmbientOcclusionParamID, Vector4.zero);
        }

        internal void Clear(CameraRenderType cameraType)
        {
            m_ActiveColorAttachments[0] = k_CameraTarget;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = null;
            for (int i = 0; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachmentIDs[i] = m_ActiveColorAttachments[i]?.nameID ?? 0;

            m_ActiveDepthAttachment = k_CameraTarget;

            m_FirstTimeCameraColorTargetIsBound = cameraType == CameraRenderType.Base;
            m_FirstTimeCameraDepthTargetIsBound = true;

            m_CameraColorTarget = null;
            m_CameraDepthTarget = null;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        void ExecuteBlock(int blockIndex, in RenderBlocks renderBlocks,
            ScriptableRenderContext context, ref RenderingData renderingData, bool submit = false)
        {
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();

            foreach (int currIndex in renderBlocks.GetRange(blockIndex))
            {
                var renderPass = m_ActiveRenderPassQueue[currIndex];
                ExecuteRenderPass(context, renderPass, cameraData, ref renderingData);
            }

            if (submit)
                context.Submit();
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        private bool IsRenderPassEnabled(ScriptableRenderPass renderPass)
        {
            return renderPass.useNativeRenderPass && useRenderPassEnabled;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        void ExecuteRenderPass(ScriptableRenderContext context, ScriptableRenderPass renderPass, UniversalCameraData cameraData, ref RenderingData renderingData)
        {
            // TODO: Separate command buffers per pass break the profiling scope order/hierarchy.
            // If a single buffer is used (passed as a param) and passed to renderPass.Execute, put the scope into command buffer (i.e. null -> cmd)
            using var profScope = new ProfilingScope(renderPass.profilingSampler);


            var cmd = renderingData.commandBuffer;

            // Selectively enable foveated rendering
            if (cameraData.xr.supportsFoveatedRendering)
            {
                if ((renderPass.renderPassEvent >= RenderPassEvent.BeforeRenderingPrePasses && renderPass.renderPassEvent < RenderPassEvent.BeforeRenderingPostProcessing)
                    || (renderPass.renderPassEvent > RenderPassEvent.AfterRendering && XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.FoveationImage)))
                {
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
                }
            }

            // Track CPU only as GPU markers for this scope were "too noisy".
            using (new ProfilingScope(Profiling.RenderPass.setRenderPassAttachments))
                SetRenderPassAttachments(cmd, renderPass, cameraData);

            // Also, we execute the commands recorded at this point to ensure SetRenderTarget is called before RenderPass.Execute
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (IsRenderPassEnabled(renderPass) && cameraData.isRenderPassSupportedCamera)
                ExecuteNativeRenderPass(context, renderPass, cameraData, ref renderingData); // cmdBuffer is executed inside
            else
            {
                // Disable obsolete warning for internal usage
                #pragma warning disable CS0618
                renderPass.Execute(context, ref renderingData);
                #pragma warning restore CS0618
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            if (cameraData.xr.enabled)
            {
                if (cameraData.xr.supportsFoveatedRendering)
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);

                // Inform the late latching system for XR once we're done with a render pass
                XRSystemUniversal.UnmarkShaderProperties(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData.xrUniversal);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
        }

        // Scene filtering is enabled when in prefab editing mode
        internal bool IsSceneFilteringEnabled(Camera camera)
        {
#if UNITY_EDITOR
            if (CoreUtils.IsSceneFilteringEnabled() && camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                return true;
#endif
            return false;
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        void SetRenderPassAttachments(CommandBuffer cmd, ScriptableRenderPass renderPass, UniversalCameraData cameraData)
        {
            Camera camera = cameraData.camera;
            ClearFlag cameraClearFlag = GetCameraClearFlag(cameraData);

            // Invalid configuration - use current attachment setup
            // Note: we only check color buffers. This is only technically correct because for shadowmaps and depth only passes
            // we bind depth as color and Unity handles it underneath. so we never have a situation that all color buffers are null and depth is bound.
            uint validColorBuffersCount = RenderingUtils.GetValidColorBufferCount(renderPass.colorAttachmentHandles);
            if (validColorBuffersCount == 0)
                return;

            // We use a different code path for MRT since it calls a different version of API SetRenderTarget
            if (RenderingUtils.IsMRT(renderPass.colorAttachmentHandles))
            {
                // In the MRT path we assume that all color attachments are REAL color attachments,
                // and that the depth attachment is a REAL depth attachment too.

                // Determine what attachments need to be cleared. ----------------

                bool needCustomCameraColorClear = false;
                bool needCustomCameraDepthClear = false;

                int cameraColorTargetIndex = RenderingUtils.IndexOf(renderPass.colorAttachmentHandles, m_CameraColorTarget);
                if (cameraColorTargetIndex != -1 && (m_FirstTimeCameraColorTargetIsBound))
                {
                    m_FirstTimeCameraColorTargetIsBound = false; // register that we did clear the camera target the first time it was bound

                    // Overlay cameras composite on top of previous ones. They don't clear.
                    // MTT: Commented due to not implemented yet
                    //                    if (renderingData.cameraData.renderType == CameraRenderType.Overlay)
                    //                        clearFlag = ClearFlag.None;

                    // We need to specifically clear the camera color target.
                    // But there is still a chance we don't need to issue individual clear() on each render-targets if they all have the same clear parameters.
                    needCustomCameraColorClear = (cameraClearFlag & ClearFlag.Color) != (renderPass.clearFlag & ClearFlag.Color)
                        || cameraData.backgroundColor != renderPass.clearColor;
                }

                // Note: if we have to give up the assumption that no depthTarget can be included in the MRT colorAttachments, we might need something like this:
                // int cameraTargetDepthIndex = IndexOf(renderPass.colorAttachments, m_CameraDepthTarget);
                // if( !renderTargetAlreadySet && cameraTargetDepthIndex != -1 && m_FirstTimeCameraDepthTargetIsBound)
                // { ...
                // }
                var depthTargetID = m_CameraDepthTarget.nameID;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                    depthTargetID = new RenderTargetIdentifier(depthTargetID, 0, CubemapFace.Unknown, -1);
#endif
                if (new RenderTargetIdentifier(renderPass.depthAttachmentHandle.nameID, 0) == new RenderTargetIdentifier(depthTargetID, 0)  // Strip the depthSlice
                    && m_FirstTimeCameraDepthTargetIsBound)
                {
                    m_FirstTimeCameraDepthTargetIsBound = false;
                    needCustomCameraDepthClear = (cameraClearFlag & ClearFlag.DepthStencil) != (renderPass.clearFlag & ClearFlag.DepthStencil);
                }

                // Perform all clear operations needed. ----------------
                // We try to minimize calls to SetRenderTarget().

                // We get here only if cameraColorTarget needs to be handled separately from the rest of the color attachments.
                if (needCustomCameraColorClear)
                {
                    // Clear camera color render-target separately from the rest of the render-targets.

                    if ((cameraClearFlag & ClearFlag.Color) != 0 && (!IsRenderPassEnabled(renderPass) || !cameraData.isRenderPassSupportedCamera))
                        SetRenderTarget(cmd, renderPass.colorAttachmentHandles[cameraColorTargetIndex], renderPass.depthAttachmentHandle, ClearFlag.Color, cameraData.backgroundColor);

                    if ((renderPass.clearFlag & ClearFlag.Color) != 0)
                    {
                        uint otherTargetsCount = RenderingUtils.CountDistinct(renderPass.colorAttachmentHandles, m_CameraColorTarget);
                        var nonCameraAttachments = m_TrimmedColorAttachmentCopies[otherTargetsCount];
                        int writeIndex = 0;
                        for (int readIndex = 0; readIndex < renderPass.colorAttachmentHandles.Length; ++readIndex)
                        {
                            if (renderPass.colorAttachmentHandles[readIndex] != null &&
                                renderPass.colorAttachmentHandles[readIndex].nameID != 0 &&
                                renderPass.colorAttachmentHandles[readIndex].nameID != m_CameraColorTarget.nameID)
                            {
                                nonCameraAttachments[writeIndex] = renderPass.colorAttachmentHandles[readIndex];
                                ++writeIndex;
                            }
                        }
                        var nonCameraAttachmentIDs = m_TrimmedColorAttachmentCopyIDs[otherTargetsCount];
                        for (int i = 0; i < otherTargetsCount; ++i)
                            nonCameraAttachmentIDs[i] = nonCameraAttachments[i].nameID;

                        if (writeIndex != otherTargetsCount)
                            Debug.LogError("writeIndex and otherTargetsCount values differed. writeIndex:" + writeIndex + " otherTargetsCount:" + otherTargetsCount);
                        if (!IsRenderPassEnabled(renderPass) || !cameraData.isRenderPassSupportedCamera)
                            SetRenderTarget(cmd, nonCameraAttachments, nonCameraAttachmentIDs, m_CameraDepthTarget, ClearFlag.Color, renderPass.clearColor);
                    }
                }

                // Bind all attachments, clear color only if there was no custom behaviour for cameraColorTarget, clear depth as needed.
                ClearFlag finalClearFlag = ClearFlag.None;
                finalClearFlag |= needCustomCameraDepthClear ? (cameraClearFlag & ClearFlag.DepthStencil) : (renderPass.clearFlag & ClearFlag.DepthStencil);
                finalClearFlag |= needCustomCameraColorClear ? (IsRenderPassEnabled(renderPass) ? (cameraClearFlag & ClearFlag.Color) : 0) : (renderPass.clearFlag & ClearFlag.Color);

                if (IsRenderPassEnabled(renderPass) && cameraData.isRenderPassSupportedCamera)
                    SetNativeRenderPassMRTAttachmentList(renderPass, cameraData, needCustomCameraColorClear, finalClearFlag);

                // Only setup render target if current render pass attachments are different from the active ones.
                if (!RenderingUtils.SequenceEqual(renderPass.colorAttachmentHandles, m_ActiveColorAttachments)
                    || renderPass.depthAttachmentHandle.nameID != m_ActiveDepthAttachment
                    || finalClearFlag != ClearFlag.None)
                {
                    int lastValidRTindex = RenderingUtils.LastValid(renderPass.colorAttachmentHandles);
                    if (lastValidRTindex >= 0)
                    {
                        int rtCount = lastValidRTindex + 1;
                        var trimmedAttachments = m_TrimmedColorAttachmentCopies[rtCount];
                        for (int i = 0; i < rtCount; ++i)
                            trimmedAttachments[i] = renderPass.colorAttachmentHandles[i];
                        var trimmedAttachmentIDs = m_TrimmedColorAttachmentCopyIDs[rtCount];
                        for (int i = 0; i < rtCount; ++i)
                            trimmedAttachmentIDs[i] = trimmedAttachments[i].nameID;

                        if (!IsRenderPassEnabled(renderPass) || !cameraData.isRenderPassSupportedCamera)
                        {
                            var depthAttachment = m_CameraDepthTarget;

                            if (renderPass.overrideCameraTarget)
                                depthAttachment = renderPass.depthAttachmentHandle;
                            else
                                m_FirstTimeCameraDepthTargetIsBound = false;

                            // Only one RTHandle is necessary to set the viewport in dynamic scaling, use depth
                            SetRenderTarget(cmd, trimmedAttachments, trimmedAttachmentIDs, depthAttachment, finalClearFlag, renderPass.clearColor);
                        }

#if ENABLE_VR && ENABLE_XR_MODULE
                        if (cameraData.xr.enabled)
                        {
                            // SetRenderTarget might alter the internal device state(winding order).
                            // Non-stereo buffer is already updated internally when switching render target. We update stereo buffers here to keep the consistency.
                            int xrTargetIndex = RenderingUtils.IndexOf(renderPass.colorAttachmentHandles, cameraData.xr.renderTarget);
                            bool renderIntoTexture = xrTargetIndex == -1;
                            cameraData.PushBuiltinShaderConstantsXR(CommandBufferHelpers.GetRasterCommandBuffer(cmd), renderIntoTexture);
                            XRSystemUniversal.MarkShaderProperties(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData.xrUniversal, renderIntoTexture);
                        }
#endif
                    }
                }
            }
            else
            {
                // Currently in non-MRT case, color attachment can actually be a depth attachment.

                var passColorAttachment = renderPass.colorAttachmentHandle;
                var passDepthAttachment = renderPass.depthAttachmentHandle;

                // When render pass doesn't call ConfigureTarget we assume it's expected to render to camera target
                // which might be backbuffer or the framebuffer render textures.

                if (!renderPass.overrideCameraTarget)
                {
                    // Default render pass attachment for passes before main rendering is current active
                    // early return so we don't change current render target setup.
                    if (renderPass.renderPassEvent < RenderPassEvent.BeforeRenderingPrePasses)
                        return;

                    // Otherwise default is the pipeline camera target.
                    passColorAttachment = m_CameraColorTarget;
                    passDepthAttachment = m_CameraDepthTarget;
                }

                ClearFlag finalClearFlag = ClearFlag.None;
                Color finalClearColor;

                if (passColorAttachment.nameID == m_CameraColorTarget.nameID && m_FirstTimeCameraColorTargetIsBound)
                {
                    m_FirstTimeCameraColorTargetIsBound = false; // register that we did clear the camera target the first time it was bound

                    finalClearFlag |= (cameraClearFlag & ClearFlag.Color);

                    // on platforms that support Load and Store actions having the clear flag means that the action will be DontCare, which is something we want when the color target is bound the first time
                    // (passColorAttachment.nameID != BuiltinRenderTextureType.CameraTarget) check below ensures camera UI's clearFlag is respected when targeting built-in backbuffer.
                    if (SystemInfo.usesLoadStoreActions && new RenderTargetIdentifier(passColorAttachment.nameID, 0, depthSlice: 0) != BuiltinRenderTextureType.CameraTarget)
                        finalClearFlag |= renderPass.clearFlag;

                    finalClearColor = cameraData.backgroundColor;

                    if (m_FirstTimeCameraDepthTargetIsBound)
                    {
                        // m_CameraColorTarget can be an opaque pointer to a RenderTexture with depth-surface.
                        // We cannot infer this information here, so we must assume both camera color and depth are first-time bound here (this is the legacy behaviour).
                        m_FirstTimeCameraDepthTargetIsBound = false;
                        finalClearFlag |= (cameraClearFlag & ClearFlag.DepthStencil);
                    }
                }
                else
                {
                    finalClearFlag |= (renderPass.clearFlag & ClearFlag.Color);
                    finalClearColor = renderPass.clearColor;
                }

                // Condition (m_CameraDepthTarget!=BuiltinRenderTextureType.CameraTarget) below prevents m_FirstTimeCameraDepthTargetIsBound flag from being reset during non-camera passes (such as Color Grading LUT). This ensures that in those cases, cameraDepth will actually be cleared during the later camera pass.
                if (new RenderTargetIdentifier(m_CameraDepthTarget.nameID, 0, depthSlice: 0) != BuiltinRenderTextureType.CameraTarget && (passDepthAttachment.nameID == m_CameraDepthTarget.nameID || passColorAttachment.nameID == m_CameraDepthTarget.nameID) && m_FirstTimeCameraDepthTargetIsBound)
                {
                    m_FirstTimeCameraDepthTargetIsBound = false;

                    finalClearFlag |= (cameraClearFlag & ClearFlag.DepthStencil);

                    // finalClearFlag |= (cameraClearFlag & ClearFlag.Color);  // <- m_CameraDepthTarget is never a color-surface, so no need to add this here.
                }
                else
                    finalClearFlag |= (renderPass.clearFlag & ClearFlag.DepthStencil);

                // If scene filtering is enabled (prefab edit mode), the filtering is implemented compositing some builtin ImageEffect passes.
                // For the composition to work, we need to clear the color buffer alpha to 0
                // How filtering works:
                // - SRP frame is fully rendered as background
                // - builtin ImageEffect pass grey-out of the full scene previously rendered
                // - SRP frame rendering only the objects belonging to the prefab being edited (with clearColor.a = 0)
                // - builtin ImageEffect pass compositing the two previous passes
                // TODO: We should implement filtering fully in SRP to remove builtin dependencies
                if (IsSceneFilteringEnabled(camera))
                {
                    finalClearColor.a = 0;
                    finalClearFlag &= ~ClearFlag.Depth;
                }

                // If the debug-handler needs to clear the screen, update "finalClearColor" accordingly...
                if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(cameraData.isPreviewCamera))
                {
                    DebugHandler.TryGetScreenClearColor(ref finalClearColor);
                }
                // Disabling Native RenderPass if not using RTHandles as we will be relying on info inside handles object
                if (IsRenderPassEnabled(renderPass) && cameraData.isRenderPassSupportedCamera)
                {
                    SetNativeRenderPassAttachmentList(renderPass, cameraData, passColorAttachment, passDepthAttachment, finalClearFlag, finalClearColor);
                }
                else
                {
                    // As alternative we would need a way to check if rts are not going to be used as shader resource
                    bool colorAttachmentChanged = false;

                    // Special handling for the first attachment to support `renderPass.overrideCameraTarget`.
                    if (passColorAttachment.nameID != m_ActiveColorAttachments[0])
                        colorAttachmentChanged = true;
                    // Check the rest of attachments (1-8)
                    for (int i = 1; i < m_ActiveColorAttachments.Length; i++)
                    {
                        if (renderPass.colorAttachmentHandles[i] != m_ActiveColorAttachments[i])
                        {
                            colorAttachmentChanged = true;
                            break;
                        }
                    }

                    // Only setup render target if current render pass attachments are different from the active ones
                    if (colorAttachmentChanged || passDepthAttachment.nameID != m_ActiveDepthAttachment || finalClearFlag != ClearFlag.None ||
                        renderPass.colorStoreActions[0] != m_ActiveColorStoreActions[0] || renderPass.depthStoreAction != m_ActiveDepthStoreAction)
                    {
                        SetRenderTarget(cmd, passColorAttachment, passDepthAttachment, finalClearFlag, finalClearColor, renderPass.colorStoreActions[0], renderPass.depthStoreAction);

#if ENABLE_VR && ENABLE_XR_MODULE
                        if (cameraData.xr.enabled)
                        {
                            // SetRenderTarget might alter the internal device state(winding order).
                            // Non-stereo buffer is already updated internally when switching render target. We update stereo buffers here to keep the consistency.
                            bool renderIntoTexture = passColorAttachment.nameID != cameraData.xr.renderTarget;
                            cameraData.PushBuiltinShaderConstantsXR(CommandBufferHelpers.GetRasterCommandBuffer(cmd), renderIntoTexture);
                            XRSystemUniversal.MarkShaderProperties(CommandBufferHelpers.GetRasterCommandBuffer(cmd), cameraData.xrUniversal, renderIntoTexture);
                        }
#endif
                    }
                }
            }

#if ENABLE_SHADER_DEBUG_PRINT
            ShaderDebugPrintManager.instance.SetShaderDebugPrintInputConstants(cmd, ShaderDebugPrintInputProducer.Get());
            ShaderDebugPrintManager.instance.SetShaderDebugPrintBindings(cmd);
#endif
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        void BeginXRRendering(CommandBuffer cmd, ScriptableRenderContext context, ref CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                if (cameraData.xrUniversal.isLateLatchEnabled)
                    cameraData.xrUniversal.canMarkLateLatch = true;

                cameraData.xr.StartSinglePass(cmd);

                if (cameraData.xr.supportsFoveatedRendering)
                {
                    cmd.ConfigureFoveatedRendering(cameraData.xr.foveatedRenderingInfo);

                    if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster))
                        cmd.SetKeyword(ShaderGlobalKeywords.FoveatedRenderingNonUniformRaster, true);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
#endif
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        void EndXRRendering(CommandBuffer cmd, ScriptableRenderContext context, ref CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                cameraData.xr.StopSinglePass(cmd);


                if (XRSystem.foveatedRenderingCaps != FoveatedRenderingCaps.None)
                {
                    if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster))
                        cmd.SetKeyword(ShaderGlobalKeywords.FoveatedRenderingNonUniformRaster, false);

                    cmd.ConfigureFoveatedRendering(IntPtr.Zero);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
#endif
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal static void SetRenderTarget(CommandBuffer cmd, RTHandle colorAttachment, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = null;
            for (int i = 0; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachmentIDs[i] = m_ActiveColorAttachments[i]?.nameID ?? 0;

            m_ActiveColorStoreActions[0] = RenderBufferStoreAction.Store;
            m_ActiveDepthStoreAction = RenderBufferStoreAction.Store;
            for (int i = 1; i < m_ActiveColorStoreActions.Length; ++i)
                m_ActiveColorStoreActions[i] = RenderBufferStoreAction.Store;

            m_ActiveDepthAttachment = depthAttachment;

            RenderBufferLoadAction colorLoadAction = ((uint)clearFlag & (uint)ClearFlag.Color) != 0 ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            RenderBufferLoadAction depthLoadAction = ((uint)clearFlag & (uint)ClearFlag.Depth) != 0 || ((uint)clearFlag & (uint)ClearFlag.Stencil) != 0 ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            // Storing depth and color in the same RT should only be possible with alias RTHandles, those that create rendertargets with RTAlloc()
            if (colorAttachment.rt == null && depthAttachment.rt == null && depthAttachment.nameID == k_CameraTarget.nameID)
                SetRenderTarget(cmd, colorAttachment, colorLoadAction, RenderBufferStoreAction.Store,
                    colorAttachment, depthLoadAction, RenderBufferStoreAction.Store, clearFlag, clearColor);
            else
                SetRenderTarget(cmd, colorAttachment, colorLoadAction, RenderBufferStoreAction.Store,
                    depthAttachment, depthLoadAction, RenderBufferStoreAction.Store, clearFlag, clearColor);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        internal static void SetRenderTarget(CommandBuffer cmd, RTHandle colorAttachment, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor, RenderBufferStoreAction colorStoreAction, RenderBufferStoreAction depthStoreAction)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = null;
            for (int i = 0; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachmentIDs[i] = m_ActiveColorAttachments[i]?.nameID ?? 0;

            m_ActiveColorStoreActions[0] = colorStoreAction;
            m_ActiveDepthStoreAction = depthStoreAction;
            for (int i = 1; i < m_ActiveColorStoreActions.Length; ++i)
                m_ActiveColorStoreActions[i] = RenderBufferStoreAction.Store;

            m_ActiveDepthAttachment = depthAttachment;

            RenderBufferLoadAction colorLoadAction = ((uint)clearFlag & (uint)ClearFlag.Color) != 0 ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            RenderBufferLoadAction depthLoadAction = ((uint)clearFlag & (uint)ClearFlag.Depth) != 0 ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            // if we shouldn't use optimized store actions then fall back to the conservative safe (un-optimal!) route and just store everything
            if (!m_UseOptimizedStoreActions)
            {
                if (colorStoreAction != RenderBufferStoreAction.StoreAndResolve)
                    colorStoreAction = RenderBufferStoreAction.Store;
                if (depthStoreAction != RenderBufferStoreAction.StoreAndResolve)
                    depthStoreAction = RenderBufferStoreAction.Store;
            }


            SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                depthAttachment, depthLoadAction, depthStoreAction, clearFlag, clearColor);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        static void SetRenderTarget(CommandBuffer cmd,
            RTHandle colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RTHandle depthAttachment,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlags,
            Color clearColor)
        {
            // XRTODO: Revisit the logic. Why treat CameraTarget depth specially?
            if (depthAttachment.nameID == BuiltinRenderTextureType.CameraTarget)
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                    colorAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
            else
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                    depthAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        static void SetRenderTarget(CommandBuffer cmd, RTHandle[] colorAttachments, RenderTargetIdentifier[] colorAttachmentIDs, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments = colorAttachments;
            m_ActiveColorAttachmentIDs = colorAttachmentIDs;
            m_ActiveDepthAttachment = depthAttachment;

            CoreUtils.SetRenderTarget(cmd, m_ActiveColorAttachmentIDs, depthAttachment, clearFlag, clearColor);
        }

        internal virtual void SwapColorBuffer(CommandBuffer cmd) { }
        internal virtual void EnableSwapBufferMSAA(bool enable) { }

        [Conditional("UNITY_EDITOR")]
        void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (!Handles.ShouldRenderGizmos() || camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                return;

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, Profiling.drawGizmos))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawGizmos(camera, gizmoSubset);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
#endif
        }

        [Conditional("UNITY_EDITOR")]
        void DrawWireOverlay(ScriptableRenderContext context, Camera camera)
        {
            context.DrawWireOverlay(camera);
        }

        void InternalStartRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            using (new ProfilingScope(Profiling.internalStartRendering))
            {
                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                {
                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    m_ActiveRenderPassQueue[i].OnCameraSetup(renderingData.commandBuffer, ref renderingData);
                    #pragma warning restore CS0618
                }
            }

            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
        }

        // Common ScriptableRenderer.Execute and RenderGraph path
        void InternalFinishRenderingCommon(CommandBuffer cmd, bool resolveFinalTarget)
        {
            using (new ProfilingScope(Profiling.internalFinishRenderingCommon))
            {
                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                    m_ActiveRenderPassQueue[i].FrameCleanup(cmd);

                // Happens when rendering the last camera in the camera stack.
                if (resolveFinalTarget)
                {
                    for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                    {
                        // Disable obsolete warning for internal usage
                        #pragma warning disable CS0618
                        m_ActiveRenderPassQueue[i].OnFinishCameraStackRendering(cmd);
                        #pragma warning restore CS0618
                    }

                    FinishRendering(cmd);

                    // We finished camera stacking and released all intermediate pipeline textures.
                    m_IsPipelineExecuting = false;
                }
                m_ActiveRenderPassQueue.Clear();
            }
        }

        // ScriptableRenderer.Execute path
        void InternalFinishRenderingExecute(ScriptableRenderContext context, CommandBuffer cmd, bool resolveFinalTarget)
        {
            InternalFinishRenderingCommon(cmd, resolveFinalTarget);

            ResetNativeRenderPassFrameData();

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private protected int AdjustAndGetScreenMSAASamples(RenderGraph renderGraph, bool useIntermediateColorTarget)
        {
            // In the editor (ConfigureTargetTexture in PlayModeView.cs) and many platforms, the system render target is always allocated without MSAA    
            if (!SystemInfo.supportsMultisampledBackBuffer) return 1;

            // For mobile platforms, when URP main rendering is done to an intermediate target and NRP enabled
            // we disable multisampling for the system render target as a bandwidth optimization
            // doing so, we avoid storing costly MSAA samples back to system memory for nothing
            bool canOptimizeScreenMSAASamples = UniversalRenderPipeline.canOptimizeScreenMSAASamples
                                                && useIntermediateColorTarget
                                                && renderGraph.nativeRenderPassesEnabled
                                                && Screen.msaaSamples > 1;
            
            if (canOptimizeScreenMSAASamples)
            {
                Screen.SetMSAASamples(1);
            }

            // iOS and macOS corner case
            bool screenAPIHasOneFrameDelay = (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.IPhonePlayer);

            return screenAPIHasOneFrameDelay ? Mathf.Max(UniversalRenderPipeline.startFrameScreenMSAASamples, 1) : Mathf.Max(Screen.msaaSamples, 1);
        }

        internal static void SortStable(List<ScriptableRenderPass> list)
        {
            int j;
            for (int i = 1; i < list.Count; ++i)
            {
                ScriptableRenderPass curr = list[i];

                j = i - 1;
                for (; j >= 0 && curr < list[j]; --j)
                    list[j + 1] = list[j];

                list[j + 1] = curr;
            }
        }

        internal struct RenderBlocks : IDisposable
        {
            private NativeArray<RenderPassEvent> m_BlockEventLimits;
            private NativeArray<int> m_BlockRanges;
            private NativeArray<int> m_BlockRangeLengths;
            public RenderBlocks(List<ScriptableRenderPass> activeRenderPassQueue)
            {
                // Upper limits for each block. Each block will contains render passes with events below the limit.
                m_BlockEventLimits = new NativeArray<RenderPassEvent>(k_RenderPassBlockCount, Allocator.Temp);
                m_BlockRanges = new NativeArray<int>(m_BlockEventLimits.Length + 1, Allocator.Temp);
                m_BlockRangeLengths = new NativeArray<int>(m_BlockRanges.Length, Allocator.Temp);

                m_BlockEventLimits[RenderPassBlock.BeforeRendering] = RenderPassEvent.BeforeRenderingPrePasses;
                m_BlockEventLimits[RenderPassBlock.MainRenderingOpaque] = RenderPassEvent.AfterRenderingOpaques;
                m_BlockEventLimits[RenderPassBlock.MainRenderingTransparent] = RenderPassEvent.AfterRenderingPostProcessing;
                m_BlockEventLimits[RenderPassBlock.AfterRendering] = (RenderPassEvent)Int32.MaxValue;

                // blockRanges[0] is always 0
                // blockRanges[i] is the index of the first RenderPass found in m_ActiveRenderPassQueue that has a ScriptableRenderPass.renderPassEvent higher than blockEventLimits[i] (i.e, should be executed after blockEventLimits[i])
                // blockRanges[blockEventLimits.Length] is m_ActiveRenderPassQueue.Count
                FillBlockRanges(activeRenderPassQueue);
                m_BlockEventLimits.Dispose();

                for (int i = 0; i < m_BlockRanges.Length - 1; i++)
                {
                    m_BlockRangeLengths[i] = m_BlockRanges[i + 1] - m_BlockRanges[i];
                }
            }

            //  RAII like Dispose pattern implementation for 'using' keyword
            public void Dispose()
            {
                m_BlockRangeLengths.Dispose();
                m_BlockRanges.Dispose();
            }

            // Fill in render pass indices for each block. End index is startIndex + 1.
            void FillBlockRanges(List<ScriptableRenderPass> activeRenderPassQueue)
            {
                int currRangeIndex = 0;
                int currRenderPass = 0;
                m_BlockRanges[currRangeIndex++] = 0;

                // For each block, it finds the first render pass index that has an event
                // higher than the block limit.
                for (int i = 0; i < m_BlockEventLimits.Length - 1; ++i)
                {
                    while (currRenderPass < activeRenderPassQueue.Count &&
                           activeRenderPassQueue[currRenderPass].renderPassEvent < m_BlockEventLimits[i])
                        currRenderPass++;

                    m_BlockRanges[currRangeIndex++] = currRenderPass;
                }

                m_BlockRanges[currRangeIndex] = activeRenderPassQueue.Count;
            }

            public int GetLength(int index)
            {
                return m_BlockRangeLengths[index];
            }

            // Minimal foreach support
            public struct BlockRange : IDisposable
            {
                int m_Current;
                int m_End;
                public BlockRange(int begin, int end)
                {
                    Assertions.Assert.IsTrue(begin <= end);
                    m_Current = begin < end ? begin : end;
                    m_End = end >= begin ? end : begin;
                    m_Current -= 1;
                }

                public BlockRange GetEnumerator() { return this; }
                public bool MoveNext() { return ++m_Current < m_End; }
                public int Current { get => m_Current; }
                public void Dispose() { }
            }

            public BlockRange GetRange(int index)
            {
                return new BlockRange(m_BlockRanges[index], m_BlockRanges[index + 1]);
            }
        }

        internal virtual bool supportsNativeRenderPassRendergraphCompiler { get => false; }

        /// <summary>
        /// Used to determine if this renderer supports the use of GPU occlusion culling.
        /// </summary>
        public virtual bool supportsGPUOcclusion => false;
    }
}
