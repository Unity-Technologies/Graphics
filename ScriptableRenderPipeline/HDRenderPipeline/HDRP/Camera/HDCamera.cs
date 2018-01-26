using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This holds all the matrix data we need for rendering, including data from the previous frame
    // (which is the main reason why we need to keep them around for a minimum of one frame).
    // HDCameras are automatically created & updated from a source camera and will be destroyed if
    // not used during a frame.
    public class HDCamera
    {
        public Matrix4x4 viewMatrix;
        public Matrix4x4 projMatrix;
        public Matrix4x4 nonJitteredProjMatrix;
        public Vector4 screenSize;
        public Plane[] frustumPlanes;
        public Vector4[] frustumPlaneEquations;
        public Camera camera;
        public uint taaFrameIndex;
        public Vector2 taaFrameRotation;
        public Vector4 viewParam;
        public PostProcessRenderContext postprocessRenderContext;

        // Non oblique projection matrix (RHS)
        public Matrix4x4 nonObliqueProjMatrix
        {
            get
            {
                return m_AdditionalCameraData != null
                    ? m_AdditionalCameraData.GetNonObliqueProjection(camera)
                    : GeometryUtils.CalculateProjectionMatrix(camera);
            }
        }

        public Matrix4x4 viewProjMatrix
        {
            get { return projMatrix * viewMatrix; }
        }

        public Matrix4x4 nonJitteredViewProjMatrix
        {
            get { return nonJitteredProjMatrix * viewMatrix; }
        }

        public RenderTextureDescriptor renderTextureDesc { get; private set; }

        // Always true for cameras that just got added to the pool - needed for previous matrices to
        // avoid one-frame jumps/hiccups with temporal effects (motion blur, TAA...)
        public bool isFirstFrame { get; private set; }

        public Vector4 invProjParam
        {
            // Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
            get
            {
                var p = projMatrix;
                return new Vector4(
                    p.m20 / (p.m00 * p.m23),
                    p.m21 / (p.m11 * p.m23),
                    -1f / p.m23,
                    (-p.m22 + p.m20 * p.m02 / p.m00 + p.m21 * p.m12 / p.m11) / p.m23
                );
            }
        }

        // View-projection matrix from the previous frame (non-jittered).
        public Matrix4x4 prevViewProjMatrix;

        // We need to keep track of these when camera relative rendering is enabled so we can take
        // camera translation into account when generating camera motion vectors
        public Vector3 cameraPos;
        public Vector3 prevCameraPos;

        // The only way to reliably keep track of a frame change right now is to compare the frame
        // count Unity gives us. We need this as a single camera could be rendered several times per
        // frame and some matrices only have to be computed once. Realistically this shouldn't
        // happen, but you never know...
        int m_LastFrameActive;

        static Dictionary<Camera, HDCamera> s_Cameras = new Dictionary<Camera, HDCamera>();
        static List<Camera> s_Cleanup = new List<Camera>(); // Recycled to reduce GC pressure

        HDAdditionalCameraData m_AdditionalCameraData;

        public HDCamera(Camera cam)
        {
            camera = cam;
            frustumPlanes = new Plane[6];
            frustumPlaneEquations = new Vector4[6];
            postprocessRenderContext = new PostProcessRenderContext();
            m_AdditionalCameraData = cam.GetComponent<HDAdditionalCameraData>();
            Reset();
        }

        public void Update(PostProcessLayer postProcessLayer, FrameSettings frameSettings)
        {
            // If TAA is enabled projMatrix will hold a jittered projection matrix. The original,
            // non-jittered projection matrix can be accessed via nonJitteredProjMatrix.
            bool taaEnabled = Application.isPlaying && camera.cameraType == CameraType.Game &&
                CoreUtils.IsTemporalAntialiasingActive(postProcessLayer);

            var nonJitteredCameraProj = camera.projectionMatrix;
            var cameraProj = taaEnabled
                ? postProcessLayer.temporalAntialiasing.GetJitteredProjectionMatrix(camera)
                : nonJitteredCameraProj;

            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(cameraProj, true); // Had to change this from 'false'
            var gpuView = camera.worldToCameraMatrix;
            var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(nonJitteredCameraProj, true);

            var pos = camera.transform.position;
            var relPos = pos; // World-origin-relative

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                // Zero out the translation component.
                gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
                relPos = Vector3.zero; // Camera-relative
            }

            var gpuVP = gpuNonJitteredProj * gpuView;

            // A camera could be rendered multiple times per frame, only updates the previous view proj & pos if needed
            if (m_LastFrameActive != Time.frameCount)
            {
                if (isFirstFrame)
                {
                    prevCameraPos = pos;
                    prevViewProjMatrix = gpuVP;
                }
                else
                {
                    prevCameraPos = cameraPos;
                    prevViewProjMatrix = nonJitteredViewProjMatrix;
                }

                isFirstFrame = false;
            }

            const uint taaFrameCount = 8;
            taaFrameIndex = taaEnabled ? (uint)Time.renderedFrameCount % taaFrameCount : 0;
            taaFrameRotation = new Vector2(Mathf.Sin(taaFrameIndex * (0.5f * Mathf.PI)),
                                           Mathf.Cos(taaFrameIndex * (0.5f * Mathf.PI)));

            viewMatrix = gpuView;
            projMatrix = gpuProj;
            nonJitteredProjMatrix = gpuNonJitteredProj;
            cameraPos = pos;
            viewParam = new Vector4(viewMatrix.determinant, 0.0f, 0.0f, 0.0f);

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                Matrix4x4 cameraDisplacement = Matrix4x4.Translate(cameraPos - prevCameraPos); // Non-camera-relative positions
                prevViewProjMatrix *= cameraDisplacement; // Now prevViewProjMatrix correctly transforms this frame's camera-relative positionWS
            }

            // Warning: near and far planes appear to be broken (or rather far plane seems broken)
            GeometryUtility.CalculateFrustumPlanes(viewProjMatrix, frustumPlanes);

            for (int i = 0; i < 4; i++)
            {
                // Left, right, top, bottom.
                frustumPlaneEquations[i] = new Vector4(frustumPlanes[i].normal.x, frustumPlanes[i].normal.y, frustumPlanes[i].normal.z, frustumPlanes[i].distance);
            }

            // Near, far.
            Vector4 forward = (camera.cameraType == CameraType.Reflection) ? camera.worldToCameraMatrix.GetRow(2) : new Vector4(camera.transform.forward.x, camera.transform.forward.y, camera.transform.forward.z, 0.0f);
            // We need to switch forward direction based on handness (Reminder: Regular camera have a negative determinant in Unity and reflection probe follow DX convention and have a positive determinant)
            forward = viewParam.x < 0.0f ? forward : -forward;
            frustumPlaneEquations[4] = new Vector4( forward.x,  forward.y,  forward.z, -Vector3.Dot(forward, relPos) - camera.nearClipPlane);
            frustumPlaneEquations[5] = new Vector4(-forward.x, -forward.y, -forward.z,  Vector3.Dot(forward, relPos) + camera.farClipPlane);

            m_LastFrameActive = Time.frameCount;

            RenderTextureDescriptor tempDesc;
            if (frameSettings.enableStereo)
            {
                screenSize = new Vector4(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight, 1.0f / XRSettings.eyeTextureWidth, 1.0f / XRSettings.eyeTextureHeight);
                tempDesc = XRSettings.eyeTextureDesc;
            }
            else
            {
                screenSize = new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight);
                tempDesc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            }

            tempDesc.msaaSamples = 1; // will be updated later, deferred will always set to 1
            tempDesc.depthBufferBits = 0;
            tempDesc.autoGenerateMips = false;
            tempDesc.useMipMap = false;
            tempDesc.enableRandomWrite = false;
            tempDesc.memoryless = RenderTextureMemoryless.None;

            renderTextureDesc = tempDesc;
        }

        // Warning: different views can use the same camera!
        public int GetViewID()
        {
            if (camera.cameraType == CameraType.Game)
            {
                int viewID = camera.GetInstanceID();
                Debug.Assert(viewID > 0);
                return viewID;
            }
            else
            {
                return 0;
            }
        }

        public void Reset()
        {
            m_LastFrameActive = -1;
            isFirstFrame = true;
        }

        // Grab the HDCamera tied to a given Camera and update it.
        public static HDCamera Get(Camera camera, PostProcessLayer postProcessLayer, FrameSettings frameSettings)
        {
            HDCamera hdcam;

            if (!s_Cameras.TryGetValue(camera, out hdcam))
            {
                hdcam = new HDCamera(camera);
                s_Cameras.Add(camera, hdcam);
            }

            hdcam.Update(postProcessLayer, frameSettings);
            return hdcam;
        }

        // Look for any camera that hasn't been used in the last frame and remove them for the pool.
        public static void CleanUnused()
        {
            int frameCheck = Time.frameCount - 1;

            foreach (var kvp in s_Cameras)
            {
                if (kvp.Value.m_LastFrameActive != frameCheck)
                    s_Cleanup.Add(kvp.Key);
            }

            foreach (var cam in s_Cleanup)
                s_Cameras.Remove(cam);

            s_Cleanup.Clear();
        }

        public void SetupGlobalParams(CommandBuffer cmd)
        {
            cmd.SetGlobalMatrix(HDShaderIDs._ViewMatrix, viewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewMatrix, viewMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._ProjMatrix, projMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvProjMatrix, projMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._NonJitteredViewProjMatrix, nonJitteredViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._ViewProjMatrix, viewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewProjMatrix, viewProjMatrix.inverse);
            cmd.SetGlobalVector(HDShaderIDs._ViewParam, viewParam);
            cmd.SetGlobalVector(HDShaderIDs._InvProjParam, invProjParam);
            cmd.SetGlobalVector(HDShaderIDs._ScreenSize, screenSize);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevViewProjMatrix, prevViewProjMatrix);
            cmd.SetGlobalVectorArray(HDShaderIDs._FrustumPlanes, frustumPlaneEquations);
            cmd.SetGlobalInt(HDShaderIDs._TaaFrameIndex, (int)taaFrameIndex);
            cmd.SetGlobalVector(HDShaderIDs._TaaFrameRotation, taaFrameRotation);
        }

        // TODO: We should set all the value below globally and not let it under the control of Unity,
        // Need to test that because we are not sure in which order these value are setup, but we need to have control on them, or rename them in our shader.
        // For now, apply it for all our compute shader to make it work
        public void SetupComputeShader(ComputeShader cs, CommandBuffer cmd)
        {
            // Copy values set by Unity which are not configured in scripts.
            cmd.SetComputeVectorParam(cs, HDShaderIDs.unity_OrthoParams, Shader.GetGlobalVector(HDShaderIDs.unity_OrthoParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProjectionParams, Shader.GetGlobalVector(HDShaderIDs._ProjectionParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ScreenParams, Shader.GetGlobalVector(HDShaderIDs._ScreenParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ZBufferParams, Shader.GetGlobalVector(HDShaderIDs._ZBufferParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._WorldSpaceCameraPos, Shader.GetGlobalVector(HDShaderIDs._WorldSpaceCameraPos));
        }
    }
}
