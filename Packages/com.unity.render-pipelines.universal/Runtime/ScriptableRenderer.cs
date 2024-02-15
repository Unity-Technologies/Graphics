using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    ///  Class <c>ScriptableRenderer</c> implements a rendering strategy. It describes how culling and lighting works and
    /// the effects supported.
    ///
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
            public static readonly ProfilingSampler setupLights = new ProfilingSampler($"{k_Name}.{nameof(SetupLights)}");
            public static readonly ProfilingSampler setupCamera = new ProfilingSampler($"Setup Camera Parameters");
            public static readonly ProfilingSampler vfxProcessCamera = new ProfilingSampler($"VFX Process Camera");
            public static readonly ProfilingSampler addRenderPasses = new ProfilingSampler($"{k_Name}.{nameof(AddRenderPasses)}");
            public static readonly ProfilingSampler setupRenderPasses = new ProfilingSampler($"{k_Name}.{nameof(SetupRenderPasses)}");
            public static readonly ProfilingSampler clearRenderingState = new ProfilingSampler($"{k_Name}.{nameof(ClearRenderingState)}");
            public static readonly ProfilingSampler internalStartRendering = new ProfilingSampler($"{k_Name}.{nameof(InternalStartRendering)}");
            public static readonly ProfilingSampler internalFinishRendering = new ProfilingSampler($"{k_Name}.{nameof(InternalFinishRendering)}");
            public static readonly ProfilingSampler drawGizmos = new ProfilingSampler($"{nameof(DrawGizmos)}");
            internal static readonly ProfilingSampler beginXRRendering = new ProfilingSampler($"Begin XR Rendering");
            internal static readonly ProfilingSampler endXRRendering = new ProfilingSampler($"End XR Rendering");

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
                public static readonly ProfilingSampler configure = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderPass.Configure)}");

                public static readonly ProfilingSampler setRenderPassAttachments = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderer.SetRenderPassAttachments)}");
            }
        }

        /// <summary>
        /// This setting controls if the camera editor should display the camera stack category.
        /// If your renderer is not supporting stacking this one should return 0.
        /// For the UI to show the Camera Stack widget this must support CameraRenderType.Base.
        /// <see cref="CameraRenderType"/>
        /// Returns the bitmask of the supported camera render types in the renderer's current state.
        /// </summary>
        /// <returns>The bitmask of the supported camera render types in the renderer's current state.</returns>
        public virtual int SupportedCameraStackingTypes()
        {
            return 0;
        }

        /// <summary>
        /// Returns true if the given camera render type is supported in the renderer's current state.
        /// </summary>
        /// <param name="cameraRenderType">The camera render type that is checked if supported.</param>
        /// <returns>True if the given camera render type is supported in the renderer's current state.</returns>
        public bool SupportsCameraStackingType(CameraRenderType cameraRenderType)
        {
            return (SupportedCameraStackingTypes() & 1 << (int)cameraRenderType) != 0;
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
            [Obsolete("cameraStacking has been deprecated use SupportedCameraRenderTypes() in ScriptableRenderer instead.", false)]
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
            SetCameraMatrices(cmd, ref cameraData, setInverseMatrices, cameraData.IsCameraProjectionMatrixFlipped());
        }

        internal static void SetCameraMatrices(CommandBuffer cmd, ref CameraData cameraData, bool setInverseMatrices, bool isTargetFlipped)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                cameraData.PushBuiltinShaderConstantsXR(cmd, false);
                XRSystemUniversal.MarkShaderProperties(cmd, cameraData.xrUniversal, false);
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
                Matrix4x4 gpuProjectionMatrix = cameraData.GetGPUProjectionMatrix(isTargetFlipped);
                Matrix4x4 viewAndProjectionMatrix = gpuProjectionMatrix * viewMatrix;
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
        void SetPerCameraShaderVariables(CommandBuffer cmd, ref CameraData cameraData)
        {
            SetPerCameraShaderVariables(cmd, ref cameraData, cameraData.IsCameraProjectionMatrixFlipped());
        }

        void SetPerCameraShaderVariables(CommandBuffer cmd, ref CameraData cameraData, bool isTargetFlipped)
        {
            using var profScope = new ProfilingScope(null, Profiling.setPerCameraShaderVariables);

            Camera camera = cameraData.camera;

            float scaledCameraWidth = (float)cameraData.cameraTargetDescriptor.width;
            float scaledCameraHeight = (float)cameraData.cameraTargetDescriptor.height;
            float cameraWidth = (float)camera.pixelWidth;
            float cameraHeight = (float)camera.pixelHeight;

            // Use eye texture's width and height as screen params when XR is enabled
            if (cameraData.xr.enabled)
            {
                cameraWidth = (float)cameraData.cameraTargetDescriptor.width;
                cameraHeight = (float)cameraData.cameraTargetDescriptor.height;

                useRenderPassEnabled = false;
            }

            if (camera.allowDynamicResolution)
            {
                scaledCameraWidth *= ScalableBufferManager.widthScaleFactor;
                scaledCameraHeight *= ScalableBufferManager.heightScaleFactor;
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
            cmd.SetGlobalVector(ShaderPropertyId.scaledScreenParams, new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));
            cmd.SetGlobalVector(ShaderPropertyId.zBufferParams, zBufferParams);
            cmd.SetGlobalVector(ShaderPropertyId.orthoParams, orthoParams);

            cmd.SetGlobalVector(ShaderPropertyId.screenSize, new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f / scaledCameraWidth, 1.0f / scaledCameraHeight));
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SCREEN_COORD_OVERRIDE, cameraData.useScreenCoordOverride);
            cmd.SetGlobalVector(ShaderPropertyId.screenSizeOverride, cameraData.screenSizeOverride);
            cmd.SetGlobalVector(ShaderPropertyId.screenCoordScaleBias, cameraData.screenCoordScaleBias);

            // Calculate a bias value which corrects the mip lod selection logic when image scaling is active.
            // We clamp this value to 0.0 or less to make sure we don't end up reducing image detail in the downsampling case.
            float mipBias = Math.Min((float)-Math.Log(cameraWidth / scaledCameraWidth, 2.0f), 0.0f);
            // Temporal Anti-aliasing can use negative mip bias to increase texture sharpness and new information for the jitter.
            float taaMipBias = Math.Min(cameraData.taaSettings.mipBias, 0.0f);
            mipBias = Math.Min(mipBias, taaMipBias);
            cmd.SetGlobalVector(ShaderPropertyId.globalMipBias, new Vector2(mipBias, Mathf.Pow(2.0f, mipBias)));

            //Set per camera matrices.
            SetCameraMatrices(cmd, ref cameraData, true, isTargetFlipped);
        }

        /// <summary>
        /// Set the Camera billboard properties.
        /// </summary>
        /// <param name="cmd">CommandBuffer to submit data to GPU.</param>
        /// <param name="cameraData">CameraData containing camera matrices information.</param>
        void SetPerCameraBillboardProperties(CommandBuffer cmd, ref CameraData cameraData)
        {
            Matrix4x4 worldToCameraMatrix = cameraData.GetViewMatrix();
            Vector3 cameraPos = cameraData.worldSpaceCameraPos;

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.BillboardFaceCameraPos, QualitySettings.billboardsFaceCameraPosition);

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

        private void SetPerCameraClippingPlaneProperties(CommandBuffer cmd, ref CameraData cameraData)
        {
            SetPerCameraClippingPlaneProperties(cmd, in cameraData, cameraData.IsCameraProjectionMatrixFlipped());
        }

        private void SetPerCameraClippingPlaneProperties(CommandBuffer cmd, in CameraData cameraData, bool isTargetFlipped)
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
        static void SetShaderTimeValues(CommandBuffer cmd, float time, float deltaTime, float smoothDeltaTime)
        {
            float timeEights = time / 8f;
            float timeFourth = time / 4f;
            float timeHalf = time / 2f;

            // Time values
            Vector4 timeVector = time * new Vector4(1f / 20f, 1f, 2f, 3f);
            Vector4 sinTimeVector = new Vector4(Mathf.Sin(timeEights), Mathf.Sin(timeFourth), Mathf.Sin(timeHalf), Mathf.Sin(time));
            Vector4 cosTimeVector = new Vector4(Mathf.Cos(timeEights), Mathf.Cos(timeFourth), Mathf.Cos(timeHalf), Mathf.Cos(time));
            Vector4 deltaTimeVector = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
            Vector4 timeParametersVector = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);

            cmd.SetGlobalVector(ShaderPropertyId.time, timeVector);
            cmd.SetGlobalVector(ShaderPropertyId.sinTime, sinTimeVector);
            cmd.SetGlobalVector(ShaderPropertyId.cosTime, cosTimeVector);
            cmd.SetGlobalVector(ShaderPropertyId.deltaTime, deltaTimeVector);
            cmd.SetGlobalVector(ShaderPropertyId.timeParameters, timeParametersVector);
        }

        /// <summary>
        /// Returns the camera color target for this renderer.
        /// It's only valid to call cameraColorTarget in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete("Use cameraColorTargetHandle")]
        public RenderTargetIdentifier cameraColorTarget
        {
            get
            {
                if (!m_IsPipelineExecuting)
                {
                    Debug.LogError("You can only call cameraColorTarget inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
                    return BuiltinRenderTextureType.None;
                }

                return m_CameraColorTarget.nameID;
            }
        }

        /// <summary>
        /// Returns the camera color target for this renderer.
        /// It's only valid to call cameraColorTarget in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        public RTHandle cameraColorTargetHandle
        {
            get
            {
                if (!m_IsPipelineExecuting)
                {
                    Debug.LogError("You can only call cameraColorTarget inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
                    return null;
                }

                return m_CameraColorTarget.handle;
            }
        }


        /// <summary>
        /// Returns the frontbuffer color target. Returns null if not implemented by the renderer.
        /// It's only valid to call GetCameraColorFrontBuffer in the scope of <c>ScriptableRenderPass</c>.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
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
        virtual internal RTHandle GetCameraColorBackBuffer(CommandBuffer cmd)
        {
            return null;
        }

        /// <summary>
        /// Returns the camera depth target for this renderer.
        /// It's only valid to call cameraDepthTarget in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        [Obsolete("Use cameraDepthTargetHandle")]
        public RenderTargetIdentifier cameraDepthTarget
        {
            get
            {
                if (!m_IsPipelineExecuting)
                {
                    Debug.LogError("You can only call cameraDepthTarget inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
                    return BuiltinRenderTextureType.None;
                }

                return m_CameraDepthTarget.nameID;
            }
        }

        /// <summary>
        /// Returns the camera depth target for this renderer.
        /// It's only valid to call cameraDepthTarget in the scope of <c>ScriptableRenderPass</c>.
        /// <seealso cref="ScriptableRenderPass"/>.
        /// </summary>
        public RTHandle cameraDepthTargetHandle
        {
            get
            {
                if (!m_IsPipelineExecuting)
                {
                    Debug.LogError("You can only call cameraDepthTarget inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
                    return null;
                }

                return m_CameraDepthTarget.handle;
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

        // Compatibility to support setting targets with RenderTargetIdentifier or RTHandle
        // to be removed once setting RenderTargetIdentifier as target is removed
        internal struct RTHandleRenderTargetIdentifierCompat
        {
            public RTHandle handle;
            public RenderTargetIdentifier fallback;
            public bool useRTHandle => handle != null;
            public RenderTargetIdentifier nameID => useRTHandle ? new RenderTargetIdentifier(handle.nameID, 0, CubemapFace.Unknown, -1) : fallback;
        }

        RTHandleRenderTargetIdentifierCompat m_CameraColorTarget;
        RTHandleRenderTargetIdentifierCompat m_CameraDepthTarget;
        RTHandleRenderTargetIdentifierCompat m_CameraResolveTarget;

        bool m_FirstTimeCameraColorTargetIsBound = true; // flag used to track when m_CameraColorTarget should be cleared (if necessary), as well as other special actions only performed the first time m_CameraColorTarget is bound as a render target
        bool m_FirstTimeCameraDepthTargetIsBound = true; // flag used to track when m_CameraDepthTarget should be cleared (if necessary), the first time m_CameraDepthTarget is bound as a render target

        // The pipeline can only guarantee the camera target texture are valid when the pipeline is executing.
        // Trying to access the camera target before or after might be that the pipeline texture have already been disposed.
        bool m_IsPipelineExecuting = false;

        // Temporary variable to disable custom passes using render pass ( due to it potentially breaking projects with custom render features )
        // To enable it - override SupportsNativeRenderPass method in the feature and return true
        internal bool disableNativeRenderPassInFeatures = false;

        internal bool useRenderPassEnabled = false;
        static RenderTargetIdentifier[] m_ActiveColorAttachments = new RenderTargetIdentifier[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        static RenderTargetIdentifier m_ActiveDepthAttachment;

        private static RenderBufferStoreAction[] m_ActiveColorStoreActions = new RenderBufferStoreAction[]
        {
            RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store,
            RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store
        };

        private static RenderBufferStoreAction m_ActiveDepthStoreAction = RenderBufferStoreAction.Store;

        // CommandBuffer.SetRenderTarget(RenderTargetIdentifier[] colors, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice);
        // called from CoreUtils.SetRenderTarget will issue a warning assert from native c++ side if "colors" array contains some invalid RTIDs.
        // To avoid that warning assert we trim the RenderTargetIdentifier[] arrays we pass to CoreUtils.SetRenderTarget.
        // To avoid re-allocating a new array every time we do that, we re-use one of these arrays:
        static RenderTargetIdentifier[][] m_TrimmedColorAttachmentCopies = new RenderTargetIdentifier[][]
        {
            new RenderTargetIdentifier[0],                          // m_TrimmedColorAttachmentCopies[0] is an array of 0 RenderTargetIdentifier - only used to make indexing code easier to read
            new RenderTargetIdentifier[] {0},                        // m_TrimmedColorAttachmentCopies[1] is an array of 1 RenderTargetIdentifier
            new RenderTargetIdentifier[] {0, 0},                     // m_TrimmedColorAttachmentCopies[2] is an array of 2 RenderTargetIdentifiers
            new RenderTargetIdentifier[] {0, 0, 0},                  // m_TrimmedColorAttachmentCopies[3] is an array of 3 RenderTargetIdentifiers
            new RenderTargetIdentifier[] {0, 0, 0, 0},               // m_TrimmedColorAttachmentCopies[4] is an array of 4 RenderTargetIdentifiers
            new RenderTargetIdentifier[] {0, 0, 0, 0, 0},            // m_TrimmedColorAttachmentCopies[5] is an array of 5 RenderTargetIdentifiers
            new RenderTargetIdentifier[] {0, 0, 0, 0, 0, 0},         // m_TrimmedColorAttachmentCopies[6] is an array of 6 RenderTargetIdentifiers
            new RenderTargetIdentifier[] {0, 0, 0, 0, 0, 0, 0},      // m_TrimmedColorAttachmentCopies[7] is an array of 7 RenderTargetIdentifiers
            new RenderTargetIdentifier[] {0, 0, 0, 0, 0, 0, 0, 0 },  // m_TrimmedColorAttachmentCopies[8] is an array of 8 RenderTargetIdentifiers
        };

        private static Plane[] s_Planes = new Plane[6];
        private static Vector4[] s_VectorPlanes = new Vector4[6];

        internal static void ConfigureActiveTarget(RenderTargetIdentifier colorAttachment,
            RenderTargetIdentifier depthAttachment)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

            m_ActiveDepthAttachment = depthAttachment;
        }

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
            DebugHandler = new DebugHandler(data);
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
            useRenderPassEnabled = data.useNativeRenderPass && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12;
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
        [Obsolete("Use RTHandles for colorTarget and depthTarget")]
        public void ConfigureCameraTarget(RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
        {
            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { fallback = colorTarget };
            m_CameraDepthTarget = new RTHandleRenderTargetIdentifierCompat { fallback = depthTarget };
        }

        /// <summary>
        /// Configures the camera target.
        /// </summary>
        /// <param name="colorTarget">Camera color target. Pass k_CameraTarget if rendering to backbuffer.</param>
        /// <param name="depthTarget">Camera depth target. Pass k_CameraTarget if color has depth or rendering to backbuffer.</param>
        public void ConfigureCameraTarget(RTHandle colorTarget, RTHandle depthTarget)
        {
            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { handle = colorTarget };
            m_CameraDepthTarget = new RTHandleRenderTargetIdentifierCompat { handle = depthTarget };
        }

        [Obsolete("Use RTHandles for colorTarget, depthTarget and resolveTarget")]
        internal void ConfigureCameraTarget(RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget, RenderTargetIdentifier resolveTarget)
        {
            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { fallback = colorTarget };
            m_CameraDepthTarget = new RTHandleRenderTargetIdentifierCompat { fallback = depthTarget };
            m_CameraResolveTarget = new RTHandleRenderTargetIdentifierCompat { fallback = resolveTarget };
        }

        internal void ConfigureCameraTarget(RTHandle colorTarget, RTHandle depthTarget, RTHandle resolveTarget)
        {
            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { handle = colorTarget };
            m_CameraDepthTarget = new RTHandleRenderTargetIdentifierCompat { handle = depthTarget };
            m_CameraResolveTarget = new RTHandleRenderTargetIdentifierCompat { handle = resolveTarget };
        }

        // This should be removed when early camera color target assignment is removed.
        [Obsolete("Use RTHandle for colorTarget")]
        internal void ConfigureCameraColorTarget(RenderTargetIdentifier colorTarget)
        {
            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { fallback = colorTarget };
        }

        // This should be removed when early camera color target assignment is removed.
        internal void ConfigureCameraColorTarget(RTHandle colorTarget)
        {
            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { handle = colorTarget };
        }

        /// <summary>
        /// Configures the render passes that will execute for this renderer.
        /// This method is called per-camera every frame.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        /// <seealso cref="ScriptableRenderPass"/>
        /// <seealso cref="ScriptableRendererFeature"/>
        public abstract void Setup(ScriptableRenderContext context, ref RenderingData renderingData);

        /// <summary>
        /// Override this method to implement the lighting setup for the renderer. You can use this to
        /// compute and upload light CBUFFER for example.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
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
        /// Override this method to record the RenderGraph passes to be used by the RenderGraph render path.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        internal virtual void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        private void InitRenderGraphFrame(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("InitFrame", out var passData,
                Profiling.setupFrameData)) //TODO rendergraph maybe add a new profiling scope?
            {
                passData.renderingData = renderingData;
                passData.renderer = this;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    CommandBuffer cmd = rgContext.cmd;
#if UNITY_EDITOR
                    float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
                    float time = Time.time;
#endif
                    float deltaTime = Time.deltaTime;
                    float smoothDeltaTime = Time.smoothDeltaTime;

                    ClearRenderingState(cmd);
                    SetShaderTimeValues(cmd, time, deltaTime, smoothDeltaTime);

                    data.renderer.SetupLights(rgContext.renderContext, ref data.renderingData);
                });
            }
        }

        private class VFXProcessCameraPassData
        {
            internal CullingResults cullResults;
            internal Camera camera;
        };

        internal void ProcessVFXCameraCommand(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<VFXProcessCameraPassData>("ProcessVFXCameraCommand", out var passData,
                       Profiling.vfxProcessCamera))
            {
                passData.camera = renderingData.cameraData.camera;
                passData.cullResults = renderingData.cullResults;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((VFXProcessCameraPassData data, RenderGraphContext context) =>
                {
                    //Triggers dispatch per camera, all global parameters should have been setup at this stage.
                    VFX.VFXManager.ProcessCameraCommand(data.camera, context.cmd, new VFX.VFXCameraXRSettings(), data.cullResults);
                });

            }
        }
        internal void SetupRenderGraphCameraProperties(RenderGraph renderGraph, ref RenderingData renderingData, bool isTargetBackbuffer)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("SetupCameraProperties", out var passData,
                Profiling.setupCamera))
            {
                passData.renderer = this;
                passData.renderingData = renderingData;
                passData.cameraData = renderingData.cameraData;
                passData.isTargetBackbuffer = isTargetBackbuffer;

                builder.AllowPassCulling(false);


                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    // TODO RENDERGRAPH: implement both branches

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
                        context.renderContext.SetupCameraProperties(data.cameraData.camera);
                        data.renderer.SetPerCameraShaderVariables(context.cmd, ref data.cameraData, !data.isTargetBackbuffer);
                    }
                    else
                    {
                        // Set new properties
                        data.renderer.SetPerCameraShaderVariables(context.cmd, ref data.cameraData, !data.isTargetBackbuffer);
                        data.renderer.SetPerCameraClippingPlaneProperties(context.cmd, in data.cameraData, !data.isTargetBackbuffer);
                        data.renderer.SetPerCameraBillboardProperties(context.cmd, ref data.cameraData);
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

                    // Setup XR camera properties
                    // XRBuiltinShaderConstants.Update(cameraData.xr, cmd, true);
                });
            }
        }


        private class DrawGizmosPassData
        {
            public RenderingData renderingData;
            public ScriptableRenderer renderer;
            public GizmoSubset gizmoSubset;
        };

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="color"></param>
        /// <param name="depth"></param>
        /// <param name="gizmoSubset"></param>
        /// <param name="renderingData"></param>
        internal void DrawRenderGraphGizmos(RenderGraph renderGraph, TextureHandle color, TextureHandle depth, GizmoSubset gizmoSubset, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<DrawGizmosPassData>("Draw Gizmos Pass", out var passData,
                Profiling.drawGizmos))
            {
                builder.UseColorBuffer(color, 0);
                builder.UseDepthBuffer(depth, DepthAccess.Read);

                passData.renderingData = renderingData;
                passData.renderer = this;
                passData.gizmoSubset = gizmoSubset;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((DrawGizmosPassData data, RenderGraphContext rgContext) =>
                {
                    Camera camera = data.renderingData.cameraData.camera;
                    data.renderer.DrawGizmos(rgContext.renderContext, camera, data.gizmoSubset, ref data.renderingData);
                });
            }
        }

        private class BeginXRPassData
        {
            public RenderingData renderingData;
            public CameraData cameraData;
        };

        private void BeginRenderGraphXRRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            using (var builder = renderGraph.AddRenderPass<BeginXRPassData>("BeginXRRendering", out var passData,
                Profiling.beginXRRendering))
            {
                passData.renderingData = renderingData;
                passData.cameraData = renderingData.cameraData;

                builder.AllowPassCulling(false);


                builder.SetRenderFunc((BeginXRPassData data, RenderGraphContext context) =>
                {
                    var cameraData = data.cameraData;
                    var cmd = data.renderingData.commandBuffer;
                    if (cameraData.xr.enabled)
                    {
                        if (cameraData.xrUniversal.isLateLatchEnabled)
                            cameraData.xrUniversal.canMarkLateLatch = true;

                        cameraData.xr.StartSinglePass(cmd);
                        context.renderContext.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }
                });
            }
#endif
        }

        private class EndXRPassData
        {
            public RenderingData renderingData;
            public CameraData cameraData;
        };

        private void EndRenderGraphXRRendering(RenderGraph renderGraph, ref RenderingData renderingData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            using (var builder = renderGraph.AddRenderPass<EndXRPassData>("EndXRRendering", out var passData,
                Profiling.endXRRendering))
            {
                passData.renderingData = renderingData;
                passData.cameraData = renderingData.cameraData;

                builder.AllowPassCulling(false);


                builder.SetRenderFunc((EndXRPassData data, RenderGraphContext context) =>
                {
                    var cameraData = data.cameraData;
                    var cmd = data.renderingData.commandBuffer;
                    if (cameraData.xr.enabled)
                    {
                        cameraData.xr.StopSinglePass(cmd);
                        context.renderContext.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }
                });
            }
#endif
        }

        private class PassData
        {
            internal RenderingData renderingData;
            internal ScriptableRenderer renderer;
            internal CameraData cameraData;
            internal bool isTargetBackbuffer;
        };


        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        internal void RecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            using (new ProfilingScope(null, Profiling.sortRenderPasses))
            {
                // Sort the render pass queue
                SortStable(m_ActiveRenderPassQueue);
            }

            InitRenderGraphFrame(renderGraph, ref renderingData);

            BeginRenderGraphXRRendering(renderGraph, ref renderingData);

            OnRecordRenderGraph(renderGraph, context, ref renderingData);

            EndRenderGraphXRRendering(renderGraph, ref renderingData);
        }

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        internal void FinishRenderGraphRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            OnFinishRenderGraphRendering(context, ref renderingData);
            InternalFinishRendering(renderingData.cameraData.resolveFinalTarget, renderingData);
        }

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        internal virtual void OnFinishRenderGraphRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        /// <summary>
        /// TODO RENDERGRAPH
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        /// <param name="injectionPoint"></param>
        internal void RecordCustomRenderGraphPasses(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData, RenderPassEvent injectionPoint)
        {
            int range = ScriptableRenderPass.GetRenderPassEventRange(injectionPoint);
            int nextValue = (int) injectionPoint + range;

            foreach (ScriptableRenderPass pass in m_ActiveRenderPassQueue)
            {
                if (pass.renderPassEvent >= injectionPoint && (int) pass.renderPassEvent < nextValue)
                    pass.RecordRenderGraph(renderGraph, ref renderingData);
            }
        }

        internal void SetPerCameraProperties(ScriptableRenderContext context, ref CameraData cameraData, Camera camera,
            CommandBuffer cmd)
        {
            if (cameraData.renderType == CameraRenderType.Base)
            {
                context.SetupCameraProperties(camera);
                SetPerCameraShaderVariables(cmd, ref cameraData);
            }
            else
            {
                // Set new properties
                SetPerCameraShaderVariables(cmd, ref cameraData);
                SetPerCameraClippingPlaneProperties(cmd, ref cameraData);
                SetPerCameraBillboardProperties(cmd, ref cameraData);
            }
        }

        /// <summary>
        /// Execute the enqueued render passes. This automatically handles editor and stereo rendering.
        /// </summary>
        /// <param name="context">Use this render context to issue any draw commands during execution.</param>
        /// <param name="renderingData">Current render state information.</param>
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Disable Gizmos when using scene overrides. Gizmos break some effects like Overdraw debug.
            bool drawGizmos = UniversalRenderPipelineDebugDisplaySettings.Instance.renderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None;

            hasReleasedRTs = false;
            m_IsPipelineExecuting = true;
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // Let renderer features call their own setup functions when targets are valid
            if (rendererFeatures.Count != 0 && !renderingData.cameraData.isPreviewCamera)
                SetupRenderPasses(in renderingData);

            CommandBuffer cmd = CommandBufferPool.Get();

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
                ClearRenderingState(cmd);
                SetShaderTimeValues(cmd, time, deltaTime, smoothDeltaTime);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                using (new ProfilingScope(null, Profiling.sortRenderPasses))
                {
                    // Sort the render pass queue
                    SortStable(m_ActiveRenderPassQueue);

                }

                using (new ProfilingScope(null, Profiling.RenderPass.configure))
                {
                    foreach (var pass in activeRenderPassQueue)
                        pass.Configure(cmd, cameraData.cameraTargetDescriptor);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                SetupNativeRenderPassFrameData(ref cameraData, useRenderPassEnabled);

                using var renderBlocks = new RenderBlocks(m_ActiveRenderPassQueue);

                using (new ProfilingScope(null, Profiling.setupLights))
                {
                    SetupLights(context, ref renderingData);
                }

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                using (new ProfilingScope(null, Profiling.setupCamera))
                {
                    //Camera variables need to be setup for the VFXManager.ProcessCameraCommand to work properly.
                    //VFXManager.ProcessCameraCommand needs to be called before any rendering (incl. shadows)
                    SetPerCameraProperties(context, ref cameraData, camera, cmd);

                    //Triggers dispatch per camera, all global parameters should have been setup at this stage.
                    VFX.VFXManager.ProcessCameraCommand(camera, cmd, new VFX.VFXCameraXRSettings(), renderingData.cullResults);

                    // Force execution of the command buffer, to ensure that it is run before "BeforeRendering", (which renders shadow maps)
                    // This is needed in 2022.2 because they are using different command buffers, but not in latest versions.
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
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
                    using var profScope = new ProfilingScope(null, Profiling.RenderBlock.beforeRendering);
                    ExecuteBlock(RenderPassBlock.BeforeRendering, in renderBlocks, context, ref renderingData);
                }

                using (new ProfilingScope(null, Profiling.setupCamera))
                {
                    // This is still required because of the following reasons:
                    // - Camera billboard properties.
                    // - Camera frustum planes: unity_CameraWorldClipPlanes[6]
                    // - _ProjectionParams.x logic is deep inside GfxDevice
                    // NOTE: The only reason we have to call this here and not at the beginning (before shadows)
                    // is because this need to be called for each eye in multi pass VR.
                    // The side effect is that this will override some shader properties we already setup and we will have to
                    // reset them.
                    SetPerCameraProperties(context, ref cameraData, camera, cmd);

                    // Reset shader time variables as they were overridden in SetupCameraProperties. If we don't do it we might have a mismatch between shadows and main rendering
                    SetShaderTimeValues(cmd, time, deltaTime, smoothDeltaTime);
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
                    using var profScope = new ProfilingScope(null, Profiling.RenderBlock.mainRenderingOpaque);
                    ExecuteBlock(RenderPassBlock.MainRenderingOpaque, in renderBlocks, context, ref renderingData);
                }

                // Transparent blocks...
                if (renderBlocks.GetLength(RenderPassBlock.MainRenderingTransparent) > 0)
                {
                    using var profScope = new ProfilingScope(null, Profiling.RenderBlock.mainRenderingTransparent);
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
                    using var profScope = new ProfilingScope(null, Profiling.RenderBlock.afterRendering);
                    ExecuteBlock(RenderPassBlock.AfterRendering, in renderBlocks, context, ref renderingData);
                }

                EndXRRendering(cmd, context, ref renderingData.cameraData);

                DrawWireOverlay(context, camera);

                if (drawGizmos)
                {
                    DrawGizmos(context, camera, GizmoSubset.PostImageEffects, ref renderingData);
                }

                InternalFinishRendering(context, cameraData.resolveFinalTarget, renderingData);

                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                {
                    m_ActiveRenderPassQueue[i].m_ColorAttachmentIndices.Dispose();
                    m_ActiveRenderPassQueue[i].m_InputAttachmentIndices.Dispose();
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
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
            if (debugHandler != null && debugHandler.IsActiveForCamera(ref cameraData) && debugHandler.IsScreenClearNeeded)
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
            using var profScope = new ProfilingScope(null, Profiling.addRenderPasses);

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
        protected void SetupRenderPasses(in RenderingData renderingData)
        {
            using var profScope = new ProfilingScope(null, Profiling.setupRenderPasses);

            // Add render passes from custom renderer features
            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                if (!rendererFeatures[i].isActive)
                    continue;

                rendererFeatures[i].SetupRenderPasses(this, in renderingData);
            }
        }

        static void ClearRenderingState(CommandBuffer cmd)
        {
            using var profScope = new ProfilingScope(null, Profiling.clearRenderingState);

            // Reset per-camera shader keywords. They are enabled depending on which render passes are executed.
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.ForwardPlus);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.ReflectionProbeBlending);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.ReflectionProbeBoxProjection);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadowsLow);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadowsMedium);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadowsHigh);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive); // Backward compatibility
            cmd.DisableShaderKeyword(ShaderKeywordStrings.LightmapShadowMixing);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.ShadowsShadowMask);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.LightLayers);
            cmd.SetGlobalVector(ScreenSpaceAmbientOcclusion.ScreenSpaceAmbientOcclusionPass.s_AmbientOcclusionParamID, Vector4.zero);
        }

        internal void Clear(CameraRenderType cameraType)
        {
            m_ActiveColorAttachments[0] = BuiltinRenderTextureType.CameraTarget;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

            m_ActiveDepthAttachment = BuiltinRenderTextureType.CameraTarget;

            m_FirstTimeCameraColorTargetIsBound = cameraType == CameraRenderType.Base;
            m_FirstTimeCameraDepthTargetIsBound = true;

            m_CameraColorTarget = new RTHandleRenderTargetIdentifierCompat { fallback = BuiltinRenderTextureType.CameraTarget };
            m_CameraDepthTarget = new RTHandleRenderTargetIdentifierCompat { fallback = BuiltinRenderTextureType.CameraTarget };
        }

        void ExecuteBlock(int blockIndex, in RenderBlocks renderBlocks,
            ScriptableRenderContext context, ref RenderingData renderingData, bool submit = false)
        {
            foreach (int currIndex in renderBlocks.GetRange(blockIndex))
            {
                var renderPass = m_ActiveRenderPassQueue[currIndex];
                ExecuteRenderPass(context, renderPass, ref renderingData);
            }

            if (submit)
                context.Submit();
        }

        private bool IsRenderPassEnabled(ScriptableRenderPass renderPass)
        {
            return renderPass.useNativeRenderPass && renderPass.m_UsesRTHandles && useRenderPassEnabled;
        }

        void ExecuteRenderPass(ScriptableRenderContext context, ScriptableRenderPass renderPass,
            ref RenderingData renderingData)
        {
            // TODO: Separate command buffers per pass break the profiling scope order/hierarchy.
            // If a single buffer is used (passed as a param) and passed to renderPass.Execute, put the scope into command buffer (i.e. null -> cmd)
            using var profScope = new ProfilingScope(null, renderPass.profilingSampler);

            ref CameraData cameraData = ref renderingData.cameraData;

            var cmd = renderingData.commandBuffer;

            // Track CPU only as GPU markers for this scope were "too noisy".
            using (new ProfilingScope(null, Profiling.RenderPass.setRenderPassAttachments))
                SetRenderPassAttachments(cmd, renderPass, ref cameraData);

            // Selectively enable foveated rendering
            if (cameraData.xr.supportsFoveatedRendering)
            {
                if (renderPass.renderPassEvent >= RenderPassEvent.BeforeRenderingPrePasses && renderPass.renderPassEvent < RenderPassEvent.BeforeRenderingPostProcessing)
                {
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
                }
            }

            // Also, we execute the commands recorded at this point to ensure SetRenderTarget is called before RenderPass.Execute
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (IsRenderPassEnabled(renderPass) && cameraData.isRenderPassSupportedCamera)
                ExecuteNativeRenderPass(context, renderPass, ref cameraData, ref renderingData); // cmdBuffer is executed inside
            else
            {
                renderPass.Execute(context, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            if (cameraData.xr.enabled)
            {
                if (cameraData.xr.supportsFoveatedRendering)
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);

                // Inform the late latching system for XR once we're done with a render pass
                XRSystemUniversal.UnmarkShaderProperties(cmd, cameraData.xrUniversal);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
        }

        void SetRenderPassAttachments(CommandBuffer cmd, ScriptableRenderPass renderPass, ref CameraData cameraData)
        {
#pragma warning disable 0618 // Obsolete usage: Using deprecated RenderTargetIdentifiers to ensure backwards compatibility with passes set with RenderTargetIdentifier
            Camera camera = cameraData.camera;
            ClearFlag cameraClearFlag = GetCameraClearFlag(ref cameraData);

            // Invalid configuration - use current attachment setup
            // Note: we only check color buffers. This is only technically correct because for shadowmaps and depth only passes
            // we bind depth as color and Unity handles it underneath. so we never have a situation that all color buffers are null and depth is bound.
            uint validColorBuffersCount = RenderingUtils.GetValidColorBufferCount(renderPass.colorAttachments);
            if (validColorBuffersCount == 0)
                return;

            // We use a different code path for MRT since it calls a different version of API SetRenderTarget
            if (RenderingUtils.IsMRT(renderPass.colorAttachments))
            {
                // In the MRT path we assume that all color attachments are REAL color attachments,
                // and that the depth attachment is a REAL depth attachment too.

                // Determine what attachments need to be cleared. ----------------

                bool needCustomCameraColorClear = false;
                bool needCustomCameraDepthClear = false;

                int cameraColorTargetIndex = RenderingUtils.IndexOf(renderPass.colorAttachments, m_CameraColorTarget.nameID);
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
                if (renderPass.depthAttachment == depthTargetID && m_FirstTimeCameraDepthTargetIsBound)
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
                        SetRenderTarget(cmd, renderPass.colorAttachments[cameraColorTargetIndex], renderPass.depthAttachment, ClearFlag.Color, cameraData.backgroundColor);

                    if ((renderPass.clearFlag & ClearFlag.Color) != 0)
                    {
                        uint otherTargetsCount = RenderingUtils.CountDistinct(renderPass.colorAttachments, m_CameraColorTarget.nameID);
                        var nonCameraAttachments = m_TrimmedColorAttachmentCopies[otherTargetsCount];
                        int writeIndex = 0;
                        for (int readIndex = 0; readIndex < renderPass.colorAttachments.Length; ++readIndex)
                        {
                            if (renderPass.colorAttachments[readIndex] != m_CameraColorTarget.nameID && renderPass.colorAttachments[readIndex] != 0)
                            {
                                nonCameraAttachments[writeIndex] = renderPass.colorAttachments[readIndex];
                                ++writeIndex;
                            }
                        }

                        if (writeIndex != otherTargetsCount)
                            Debug.LogError("writeIndex and otherTargetsCount values differed. writeIndex:" + writeIndex + " otherTargetsCount:" + otherTargetsCount);
                        if (!IsRenderPassEnabled(renderPass) || !cameraData.isRenderPassSupportedCamera)
                            SetRenderTarget(cmd, nonCameraAttachments, m_CameraDepthTarget.nameID, ClearFlag.Color, renderPass.clearColor);
                    }
                }

                // Bind all attachments, clear color only if there was no custom behaviour for cameraColorTarget, clear depth as needed.
                ClearFlag finalClearFlag = ClearFlag.None;
                finalClearFlag |= needCustomCameraDepthClear ? (cameraClearFlag & ClearFlag.DepthStencil) : (renderPass.clearFlag & ClearFlag.DepthStencil);
                finalClearFlag |= needCustomCameraColorClear ? (IsRenderPassEnabled(renderPass) ? (cameraClearFlag & ClearFlag.Color) : 0) : (renderPass.clearFlag & ClearFlag.Color);

                if (IsRenderPassEnabled(renderPass) && cameraData.isRenderPassSupportedCamera)
                    SetNativeRenderPassMRTAttachmentList(renderPass, ref cameraData, needCustomCameraColorClear, finalClearFlag);

                // Only setup render target if current render pass attachments are different from the active ones.
                if (!RenderingUtils.SequenceEqual(renderPass.colorAttachments, m_ActiveColorAttachments) || renderPass.depthAttachment != m_ActiveDepthAttachment || finalClearFlag != ClearFlag.None)
                {
                    int lastValidRTindex = RenderingUtils.LastValid(renderPass.colorAttachments);
                    if (lastValidRTindex >= 0)
                    {
                        int rtCount = lastValidRTindex + 1;
                        var trimmedAttachments = m_TrimmedColorAttachmentCopies[rtCount];
                        for (int i = 0; i < rtCount; ++i)
                            trimmedAttachments[i] = renderPass.colorAttachments[i];

                        if (!IsRenderPassEnabled(renderPass) || !cameraData.isRenderPassSupportedCamera)
                        {
                            if (renderPass.m_UsesRTHandles)
                            {
                                var depthAttachment = m_CameraDepthTarget.handle;

                                if (renderPass.overrideCameraTarget)
                                    depthAttachment = renderPass.depthAttachmentHandle;
                                else
                                    m_FirstTimeCameraDepthTargetIsBound = false;

                                // Only one RTHandle is necessary to set the viewport in dynamic scaling, use depth
                                SetRenderTarget(cmd, trimmedAttachments, depthAttachment, finalClearFlag, renderPass.clearColor);
                            }
                            else
                            {
                                var depthAttachment = m_CameraDepthTarget.nameID;

                                if (renderPass.overrideCameraTarget)
                                    depthAttachment = renderPass.depthAttachment;
                                else
                                    m_FirstTimeCameraDepthTargetIsBound = false;

                                SetRenderTarget(cmd, trimmedAttachments, depthAttachment, finalClearFlag, renderPass.clearColor);
                            }
                        }

#if ENABLE_VR && ENABLE_XR_MODULE
                        if (cameraData.xr.enabled)
                        {
                            // SetRenderTarget might alter the internal device state(winding order).
                            // Non-stereo buffer is already updated internally when switching render target. We update stereo buffers here to keep the consistency.
                            int xrTargetIndex = RenderingUtils.IndexOf(renderPass.colorAttachments, cameraData.xr.renderTarget);
                            bool renderIntoTexture = xrTargetIndex == -1;
                            cameraData.PushBuiltinShaderConstantsXR(cmd, renderIntoTexture);
                            XRSystemUniversal.MarkShaderProperties(cmd, cameraData.xrUniversal, renderIntoTexture);
                        }
#endif
                    }
                }
            }
            else
            {
                // Currently in non-MRT case, color attachment can actually be a depth attachment.

                var passColorAttachment = renderPass.m_UsesRTHandles ?
                    new RTHandleRenderTargetIdentifierCompat { handle = renderPass.colorAttachmentHandle } :
                new RTHandleRenderTargetIdentifierCompat { fallback = renderPass.colorAttachment };
                var passDepthAttachment = renderPass.m_UsesRTHandles ?
                    new RTHandleRenderTargetIdentifierCompat { handle = renderPass.depthAttachmentHandle } :
                new RTHandleRenderTargetIdentifierCompat { fallback = renderPass.depthAttachment };

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

#if UNITY_EDITOR
                if (CoreUtils.IsSceneFilteringEnabled() && camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                {
                    finalClearColor.a = 0;
                    finalClearFlag &= ~ClearFlag.Depth;
                }
#endif

                // If the debug-handler needs to clear the screen, update "finalClearColor" accordingly...
                if ((DebugHandler != null) && DebugHandler.IsActiveForCamera(ref cameraData))
                {
                    DebugHandler.TryGetScreenClearColor(ref finalClearColor);
                }
                // Disabling Native RenderPass if not using RTHandles as we will be relying on info inside handles object
                if (IsRenderPassEnabled(renderPass) && cameraData.isRenderPassSupportedCamera && renderPass.m_UsesRTHandles)
                {
                    SetNativeRenderPassAttachmentList(renderPass, ref cameraData, passColorAttachment.handle, passDepthAttachment.handle, finalClearFlag, finalClearColor);
                }
                else
                {
                    // As alternative we would need a way to check if rts are not going to be used as shader resource
                    bool colorAttachmentChanged = false;
                    for (int i = 0; i < m_ActiveColorAttachments.Length; i++)
                    {
                        if (renderPass.colorAttachments[i] != m_ActiveColorAttachments[i])
                        {
                            colorAttachmentChanged = true;
                            break;
                        }
                    }

                    // Only setup render target if current render pass attachments are different from the active ones
                    if (colorAttachmentChanged || passColorAttachment.nameID != m_ActiveColorAttachments[0] || passDepthAttachment.nameID != m_ActiveDepthAttachment || finalClearFlag != ClearFlag.None ||
                        renderPass.colorStoreActions[0] != m_ActiveColorStoreActions[0] || renderPass.depthStoreAction != m_ActiveDepthStoreAction)
                    {
                        if (passColorAttachment.useRTHandle && passDepthAttachment.useRTHandle)
                            SetRenderTarget(cmd, passColorAttachment.handle, passDepthAttachment.handle, finalClearFlag, finalClearColor, renderPass.colorStoreActions[0], renderPass.depthStoreAction);
                        else
                            SetRenderTarget(cmd, passColorAttachment.fallback, passDepthAttachment.fallback, finalClearFlag, finalClearColor, renderPass.colorStoreActions[0], renderPass.depthStoreAction);

#if ENABLE_VR && ENABLE_XR_MODULE
                        if (cameraData.xr.enabled)
                        {
                            // SetRenderTarget might alter the internal device state(winding order).
                            // Non-stereo buffer is already updated internally when switching render target. We update stereo buffers here to keep the consistency.
                            bool renderIntoTexture = passColorAttachment.nameID != cameraData.xr.renderTarget;
                            cameraData.PushBuiltinShaderConstantsXR(cmd, renderIntoTexture);
                            XRSystemUniversal.MarkShaderProperties(cmd, cameraData.xrUniversal, renderIntoTexture);
                        }
#endif
                    }
                }
            }

#if ENABLE_SHADER_DEBUG_PRINT
            ShaderDebugPrintManager.instance.SetShaderDebugPrintInputConstants(cmd, ShaderDebugPrintInputProducer.Get());
            ShaderDebugPrintManager.instance.SetShaderDebugPrintBindings(cmd);
#endif
#pragma warning restore 0618
        }

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
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.FoveatedRenderingNonUniformRaster);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
#endif
        }

        void EndXRRendering(CommandBuffer cmd, ScriptableRenderContext context, ref CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                cameraData.xr.StopSinglePass(cmd);


                if (XRSystem.foveatedRenderingCaps != FoveatedRenderingCaps.None)
                {
                    if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster))
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.FoveatedRenderingNonUniformRaster);

                    cmd.ConfigureFoveatedRendering(IntPtr.Zero);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
#endif
        }

        [Obsolete("Use RTHandles for colorAttachment and depthAttachment")]
        internal static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

            m_ActiveColorStoreActions[0] = RenderBufferStoreAction.Store;
            m_ActiveDepthStoreAction = RenderBufferStoreAction.Store;
            for (int i = 1; i < m_ActiveColorStoreActions.Length; ++i)
                m_ActiveColorStoreActions[i] = RenderBufferStoreAction.Store;

            m_ActiveDepthAttachment = depthAttachment;

            RenderBufferLoadAction colorLoadAction = ((uint)clearFlag & (uint)ClearFlag.Color) != 0 ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            RenderBufferLoadAction depthLoadAction = ((uint)clearFlag & (uint)ClearFlag.Depth) != 0 || ((uint)clearFlag & (uint)ClearFlag.Stencil) != 0 ?
                RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load;

            SetRenderTarget(cmd, colorAttachment, colorLoadAction, RenderBufferStoreAction.Store,
                depthAttachment, depthLoadAction, RenderBufferStoreAction.Store, clearFlag, clearColor);
        }

        internal static void SetRenderTarget(CommandBuffer cmd, RTHandle colorAttachment, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

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

        [Obsolete("Use RTHandles for colorAttachment and depthAttachment")]
        internal static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor, RenderBufferStoreAction colorStoreAction, RenderBufferStoreAction depthStoreAction)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

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

        internal static void SetRenderTarget(CommandBuffer cmd, RTHandle colorAttachment, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor, RenderBufferStoreAction colorStoreAction, RenderBufferStoreAction depthStoreAction)
        {
            m_ActiveColorAttachments[0] = colorAttachment;
            for (int i = 1; i < m_ActiveColorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = 0;

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

        [Obsolete("Use RTHandle for colorAttachment")]
        static void SetRenderTarget(CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            ClearFlag clearFlags,
            Color clearColor)
        {
            CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
        }

        static void SetRenderTarget(CommandBuffer cmd,
            RTHandle colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            ClearFlag clearFlags,
            Color clearColor)
        {
            CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
        }

        [Obsolete("Use RTHandles for colorAttachment and depthAttachment")]
        static void SetRenderTarget(CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthAttachment,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlags,
            Color clearColor)
        {
            // XRTODO: Revisit the logic. Why treat CameraTarget depth specially?
            if (depthAttachment == BuiltinRenderTextureType.CameraTarget)
            {
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, depthLoadAction, depthStoreAction, clearFlags, clearColor);
            }
            else
            {
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                    depthAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
            }
        }

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

        [Obsolete("Use RTHandles for colorAttachments and depthAttachment")]
        static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments = colorAttachments;
            m_ActiveDepthAttachment = depthAttachment;

            CoreUtils.SetRenderTarget(cmd, colorAttachments, depthAttachment, clearFlag, clearColor);
        }

        [Obsolete("Use RTHandle for colorAttachments")]
        static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorAttachments, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            m_ActiveColorAttachments = colorAttachments;
            m_ActiveDepthAttachment = depthAttachment.nameID;

            CoreUtils.SetRenderTarget(cmd, m_ActiveColorAttachments, depthAttachment, clearFlag, clearColor);
        }

        static void SetRenderTarget(CommandBuffer cmd, RTHandle[] colorAttachments, RTHandle depthAttachment, ClearFlag clearFlag, Color clearColor)
        {
            if (m_ActiveColorAttachments.Length != colorAttachments.Length)
                m_ActiveColorAttachments = new RenderTargetIdentifier[colorAttachments.Length];

            for (int i = 0; i < colorAttachments.Length; ++i)
                m_ActiveColorAttachments[i] = colorAttachments[i].nameID;
            m_ActiveDepthAttachment = depthAttachment.nameID;

            CoreUtils.SetRenderTarget(cmd, m_ActiveColorAttachments, depthAttachment, clearFlag, clearColor);
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
            using (new ProfilingScope(null, Profiling.internalStartRendering))
            {
                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                {
                    m_ActiveRenderPassQueue[i].OnCameraSetup(renderingData.commandBuffer, ref renderingData);
                }
            }

            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
        }

        void InternalFinishRendering(bool resolveFinalTarget, RenderingData renderingData)
        {
            using (new ProfilingScope(null, Profiling.internalFinishRendering))
            {
                for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                    m_ActiveRenderPassQueue[i].FrameCleanup(renderingData.commandBuffer);

                // Happens when rendering the last camera in the camera stack.
                if (resolveFinalTarget)
                {
                    for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                        m_ActiveRenderPassQueue[i].OnFinishCameraStackRendering(renderingData.commandBuffer);

                    FinishRendering(renderingData.commandBuffer);

                    // We finished camera stacking and released all intermediate pipeline textures.
                    m_IsPipelineExecuting = false;
                }
                m_ActiveRenderPassQueue.Clear();
            }
        }

        void InternalFinishRendering(ScriptableRenderContext context, bool resolveFinalTarget, RenderingData renderingData)
        {
            InternalFinishRendering(resolveFinalTarget, renderingData);

            ResetNativeRenderPassFrameData();

            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
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
    }
}
