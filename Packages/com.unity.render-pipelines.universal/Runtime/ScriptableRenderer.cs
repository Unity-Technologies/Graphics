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
    /// Class <c>ScriptableRenderer</c> implements a rendering strategy. It describes how culling and lighting work and
    /// the effects supported. A custom scriptable renderer is the lowest level of extensibility of URP. It allows you
    /// to implement a fully new rendering strategy at the expense of a lot more complexity and work. However, It's still
    /// a lot less work and more maintainable than writing a full-fledged custom render pipeline.
    /// If you want to simply extend the existing URP renderers (2D and 3D), using <c>ScriptableRendererFeature</c> should
    /// always be considered first.
    /// </summary>
    /// <remarks>
    /// A renderer can be used for all cameras or be overridden on a per-camera basis. It will implement light culling and setup
    /// and describe a list of <c>ScriptableRenderPass</c> to execute in a frame. It will also define the RenderGraph to execute.
    /// External users can then again extend your scriptable renderer to support more effects with additional <c>ScriptableRendererFeatures</c>.
    ///
    /// The <c>ScriptableRenderer</c> is a run-time object. The resources and asset data for the renderer are serialized in
    /// <c>ScriptableRendererData</c> (more specifically a class derived from <c>ScriptableRendererData</c> which contains additional data for your renderer).
    ///
    /// The high-level steps needed to create and use your own scriptable renderer are:
    ///
    /// 1. Create subclasses of  <c>ScriptableRenderer</c> and <c>ScriptableRendererData</c> and implement the rendering logic. Key functions to implement here are:
    /// <c>ScriptableRenderer.OnRecordRenderGraph</c> which will define the rendergraph to execute when rendering a camera. And <c>ScriptableRendererData.Create</c> to create
    /// an instance of your new <c>ScriptableRenderer</c> subclass.
    /// 2. Create an asset of your new <c>ScriptableRendererData</c> subclass and assign it to the renderer asset field in the URP asset so it gets picked
    /// up at run time.
    /// </remarks>
    /// <example>
    /// You can find a code sample in the URP tests package in the "Graphics/Tests/SRPTests/Packages/com.unity.testing.urp/Scripts/Runtime/CustomRenderPipeline/" folder
    /// of the SRP repository.
    /// </example>
    public abstract partial class ScriptableRenderer : IDisposable
    {
        private static class Profiling
        {
            private const string k_Name = nameof(ScriptableRenderer);
            public static readonly ProfilingSampler setPerCameraShaderVariables = new ProfilingSampler($"{k_Name}.{nameof(SetPerCameraShaderVariables)}");
            public static readonly ProfilingSampler sortRenderPasses = new ProfilingSampler($"Sort Render Passes");
            public static readonly ProfilingSampler recordRenderGraph = new ProfilingSampler($"On Record Render Graph");
            public static readonly ProfilingSampler setupCamera = new ProfilingSampler($"Setup Camera Properties");
            public static readonly ProfilingSampler vfxProcessCamera = new ProfilingSampler($"VFX Process Camera");
            public static readonly ProfilingSampler addRenderPasses = new ProfilingSampler($"{k_Name}.{nameof(AddRenderPasses)}");
            public static readonly ProfilingSampler clearRenderingState = new ProfilingSampler($"{k_Name}.{nameof(ClearRenderingState)}");
            public static readonly ProfilingSampler internalFinishRenderingCommon = new ProfilingSampler($"{k_Name}.{nameof(InternalFinishRenderingCommon)}");
            public static readonly ProfilingSampler drawGizmos = new ProfilingSampler("DrawGizmos"); //Todo: update to nameof(method reference) once RG version name is cleaned up
            public static readonly ProfilingSampler drawWireOverlay = new ProfilingSampler("DrawWireOverlay"); //Todo: update to nameof(method reference) once RG version name is cleaned up
            internal static readonly ProfilingSampler beginXRRendering = new ProfilingSampler($"Begin XR Rendering");
            internal static readonly ProfilingSampler endXRRendering = new ProfilingSampler($"End XR Rendering");
            internal static readonly ProfilingSampler initRenderGraphFrame = new ProfilingSampler($"Initialize Frame");
            internal static readonly ProfilingSampler setEditorTarget = new ProfilingSampler($"Set Editor Target");
        }

        /// <summary>
        /// This setting controls if the camera editor should display the camera stack category.
        /// If your scriptable renderer is not supporting stacking this one should return 0.
        /// For the UI to show the Camera Stack widget this must at least support CameraRenderType.Base.
        /// </summary>
        /// <seealso cref="CameraRenderType"/>
        /// <returns>The bitmask of the supported camera render types in the renderer's current state.</returns>
        public virtual int SupportedCameraStackingTypes()
        {
            return 0;
        }

        /// <summary>
        /// Check if the given camera render type is supported in the renderer's current state. The default implementation
        /// simply checks if the camera type is part of the <see cref="SupportedCameraStackingTypes'"/> bitmask.
        /// </summary>
        /// <seealso cref="CameraRenderType"/>
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
        /// Check if the ScriptableRenderer implements a camera opaque pass.
        /// </summary>
        /// <returns>Returns true if the ScriptableRenderer implements a camera opaque pass. False otherwise.</returns>
        protected internal virtual bool SupportsCameraOpaque()
        {
            return false;
        }

        /// <summary>
        /// Check if the ScriptableRenderer implements a camera normal pass.
        /// </summary>
        /// <returns>Returns true if the ScriptableRenderer implements a camera normal pass. False otherwise.</returns>
        protected internal virtual bool SupportsCameraNormals()
        {
            return false;
        }

        /// <summary>
        /// Configures the supported features for this renderer. When creating custom renderers
        /// for Universal Render Pipeline you can choose to opt-in or out for specific features.
        /// </summary>
        public class RenderingFeatures
        {
            /// <summary>
            /// This setting controls if the camera editor should display the camera stack category.
            /// Renderers that don't support camera stacking will only render cameras of type CameraRenderType.Base
            /// </summary>
            /// <seealso cref="CameraRenderType"/>
            /// <seealso cref="UniversalAdditionalCameraData.cameraStack"/>
            [Obsolete("cameraStacking has been deprecated use SupportedCameraRenderTypes() in ScriptableRenderer instead. #from(2022.2) #breakingFrom(2023.1)", true)]
            public bool cameraStacking { get; set; } = false;

            /// <summary>
            /// This setting controls if the Universal Render Pipeline asset should expose the MSAA option.
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
            }

            if (camera.allowDynamicResolution)
            {
                scaledCameraTargetWidth *= ScalableBufferManager.widthScaleFactor;
                scaledCameraTargetHeight *= ScalableBufferManager.heightScaleFactor;
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
            if (cameraData.renderType == CameraRenderType.Overlay)
            {
                float projectionFlipSign = isTargetFlipped ? -1.0f : 1.0f;
                Vector4 projectionParams = new Vector4(projectionFlipSign, near, far, 1.0f * invFar);
                cmd.SetGlobalVector(ShaderPropertyId.projectionParams, projectionParams);
            }

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
        /// Returns a list of renderer features added to this renderer.
        /// </summary>
        /// <seealso cref="ScriptableRendererFeature"/>
        protected List<ScriptableRendererFeature> rendererFeatures
        {
            get => m_RendererFeatures;
        }

        /// <summary>
        /// Returns a list of render passes scheduled to be executed by this renderer.
        /// </summary>
        /// <seealso cref="ScriptableRenderPass"/>
        protected List<ScriptableRenderPass> activeRenderPassQueue
        {
            get => m_ActiveRenderPassQueue;
        }

        /// <summary>
        /// Supported rendering features by this renderer. The scriptable renderer framework will use the returned information
        /// to adjust things like inspectors, etc.
        /// </summary>
        /// <seealso cref="SupportedRenderingFeatures"/>
        public RenderingFeatures supportedRenderingFeatures { get; set; } = new RenderingFeatures();

        /// <summary>
        /// List of unsupported Graphics APIs for this renderer.The scriptable renderer framework will use the returned information
        /// to adjust things like inspectors, etc.
        /// </summary>
        /// <seealso cref="GraphicsDeviceType"/>
        public GraphicsDeviceType[] unsupportedGraphicsDeviceTypes { get; set; } = new GraphicsDeviceType[0];

        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>(32);
        List<ScriptableRendererFeature> m_RendererFeatures = new List<ScriptableRendererFeature>(10);

        // The pipeline can only guarantee the camera target texture are valid when the pipeline is executing.
        // Trying to access the camera target before or after might be that the pipeline texture have already been disposed.
        bool m_IsPipelineExecuting = false;

        ContextContainer m_frameData = new();
        internal ContextContainer frameData => m_frameData;

        private static Plane[] s_Planes = new Plane[6];
        private static Vector4[] s_VectorPlanes = new Vector4[6];

        /// <summary>
        /// In URP RenderGraph (likely not in Compatibility Mode), this returns if the pipeline will actually perform depth priming.
        /// Depth priming is done with a prepass to the activeCameraDepth.
        /// Even when the settings on the URP asset requests depth priming the pipeline can decide not to do it (or vice versa).
        /// </summary>
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
            foreach (var feature in data.rendererFeatures)
            {
                if (feature == null)
                    continue;

                feature.Create();
                m_RendererFeatures.Add(feature);
            }
            m_ActiveRenderPassQueue.Clear();
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

                try
                {
                    // Guard the renderer feature Dispose() call so if it raises any exception,
                    // it doesn't leave the renderer in a partially destructed state.
                    rendererFeatures[i].Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called by Dispose().
        /// Override this function to clean up resources in your renderer.
        /// Be sure to call this base dispose in your overridden function to free resources allocated by the base.
        /// </summary>
        /// <param name="disposing">See the definition of IDisposable.</param>
        protected virtual void Dispose(bool disposing)
        {
            DebugHandler?.Dispose();
        }

        internal virtual void ReleaseRenderTargets()
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
        /// <param name="cmd">The command buffer where any work should be recorded on..</param>
        public virtual void FinishRendering(CommandBuffer cmd)
        {
        }

        /// <summary>
        /// Override this method to initialize anything before starting the recording of the render graph, such as resources.
        /// This is the last point where it is ok to call <c>ScriptableRenderer.EnqueuePass</c> as after this function the
        /// queue will be sorted for the frame.
        /// </summary>
        public virtual void OnBeginRenderGraphFrame()
        {
        }

        /// <summary>
        /// Override this method to record the RenderGraph passes to be used by the RenderGraph render path.
        /// </summary>
        /// <param name="renderGraph">The rendergraph to schedule passes on.</param>
        /// <param name="context">The render context to use when creating rendering lists or performing culling operations. Ideally, graphics work should be executed through rendergraph so is is not recommended to use <c>ScriptableRenderContext.ExecuteCommandBuffer</c>. </param>
        internal virtual void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
        }

        /// <summary>
        /// Override this method to cleanup things after recording the render graph, such as resources.
        /// This executes after the render graph is recorded but before it is compiled and executed.
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

                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext rgContext) =>
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

                builder.SetRenderFunc(static (VFXProcessCameraPassData data, UnsafeGraphContext context) =>
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

        internal void SetupRenderGraphCameraProperties(RenderGraph renderGraph, in TextureHandle target)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(Profiling.setupCamera.name, out var passData,
                Profiling.setupCamera))
            {
                passData.renderer = this;
                passData.cameraData = frameData.Get<UniversalCameraData>();
                passData.cameraTargetSizeCopy = new Vector2Int(passData.cameraData.cameraTargetDescriptor.width, passData.cameraData.cameraTargetDescriptor.height);
                passData.target = target;

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    bool yFlipped = SystemInfo.graphicsUVStartsAtTop && RenderingUtils.IsHandleYFlipped(context, in data.target);

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
                        data.renderer.SetPerCameraShaderVariables(context.cmd, data.cameraData, data.cameraTargetSizeCopy, yFlipped);
                    }
                    else
                    {
                        // Set new properties
                        data.renderer.SetPerCameraShaderVariables(context.cmd, data.cameraData, data.cameraTargetSizeCopy, yFlipped);
                        data.renderer.SetPerCameraClippingPlaneProperties(context.cmd, in data.cameraData, yFlipped);
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
        internal void DrawRenderGraphGizmos(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle color, in TextureHandle depth, GizmoSubset gizmoSubset)
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

                builder.SetRenderFunc(static (DrawGizmosPassData data, UnsafeGraphContext rgContext) =>
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

        internal void DrawRenderGraphWireOverlay(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle color)
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

                builder.AllowGlobalStateModification(true);

                // Apply MultiviewRenderRegionsCompatible flag only for the first pass in multipass
                if (cameraData.xr.multipassId == 0)
                {
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                }

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

                builder.SetRenderFunc(static (DummyData data, UnsafeGraphContext context) =>
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
            internal TextureHandle target;

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
        
        /// <summary>
        /// Enqueues a render pass for execution.
        /// </summary>
        /// <param name="pass">Render pass to be enqueued.</param>
        public void EnqueuePass(ScriptableRenderPass pass)
        {
            m_ActiveRenderPassQueue.Add(pass);
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

                rendererFeatures[i].AddRenderPasses(this, ref renderingData);
            }

            // Remove any null render pass that might have been added by user by mistake
            int count = activeRenderPassQueue.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (activeRenderPassQueue[i] == null)
                    activeRenderPassQueue.RemoveAt(i);
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

        // Scene filtering is enabled when in prefab editing mode
        internal bool IsSceneFilteringEnabled(Camera camera)
        {
#if UNITY_EDITOR
            if (CoreUtils.IsSceneFilteringEnabled() && camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                return true;
#endif
            return false;
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

                    FinishRendering(cmd);

                    // We finished camera stacking and released all intermediate pipeline textures.
                    m_IsPipelineExecuting = false;
                }
                m_ActiveRenderPassQueue.Clear();
            }
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

        /// <summary>
        /// Used to determine if this renderer supports the use of GPU occlusion culling.
        /// </summary>
        public virtual bool supportsGPUOcclusion => false;
    }
}
