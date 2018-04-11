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
        public Vector4   cameraPositionWS;
        public float     detViewMatrix;
        public Vector4   screenSize;
        public Frustum   frustum;
        public Vector4[] frustumPlaneEquations;
        public Camera    camera;
        public uint      taaFrameIndex;
        public Vector2   taaFrameRotation;
        public Vector4   depthBufferParam;

        public PostProcessRenderContext postprocessRenderContext;

        public Matrix4x4[] viewMatrixStereo;
        public Matrix4x4[] projMatrixStereo;
        public Vector4 centerEyeTranslationOffset;

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

        // This is the size actually used for this camera (as it can be altered by VR for example)
        int m_ActualWidth;
        int m_ActualHeight;
        // This is the scale and bias of the camera viewport compared to the reference size of our Render Targets (RHandle.maxSize)
        Vector2 m_CameraScaleBias;
        // Current mssa sample
        MSAASamples m_msaaSamples;

        public int actualWidth { get { return m_ActualWidth; } }
        public int actualHeight { get { return m_ActualHeight; } }
        public Vector2 scaleBias { get { return m_CameraScaleBias; } }
        public MSAASamples msaaSamples { get { return m_msaaSamples; } }

        public Matrix4x4 viewProjMatrix
        {
            get { return projMatrix * viewMatrix; }
        }

        public Matrix4x4 nonJitteredViewProjMatrix
        {
            get { return nonJitteredProjMatrix * viewMatrix; }
        }

        public Matrix4x4 GetViewProjMatrixStereo(uint eyeIndex)
        {
            return (projMatrixStereo[eyeIndex] * viewMatrixStereo[eyeIndex]);
        }

        // Always true for cameras that just got added to the pool - needed for previous matrices to
        // avoid one-frame jumps/hiccups with temporal effects (motion blur, TAA...)
        public bool isFirstFrame { get; private set; }

        // Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
        // TODO: pass this as "_DepthBufferParam" if the projection matrix is oblique.
        public Vector4 invProjParam
        {
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

        public bool clearDepth
        {
            get { return m_AdditionalCameraData != null ? m_AdditionalCameraData.clearDepth : camera.clearFlags != CameraClearFlags.Nothing; }
        }

        public HDAdditionalCameraData.ClearColorMode clearColorMode
        {
            get
            {
                if (m_AdditionalCameraData != null)
                {
                    return m_AdditionalCameraData.clearColorMode;
                }

                if (camera.clearFlags == CameraClearFlags.Skybox)
                    return HDAdditionalCameraData.ClearColorMode.Sky;
                else if (camera.clearFlags == CameraClearFlags.SolidColor)
                    return HDAdditionalCameraData.ClearColorMode.BackgroundColor;
                else // None
                    return HDAdditionalCameraData.ClearColorMode.None;
            }
        }

        public Color backgroundColorHDR
        {
            get
            {
                if (m_AdditionalCameraData != null)
                {
                    return m_AdditionalCameraData.backgroundColorHDR;
                }

                // The scene view has no additional data so this will correctly pick the editor preference backround color here.
                return camera.backgroundColor.linear;
            }
        }

        static Dictionary<Camera, HDCamera> s_Cameras = new Dictionary<Camera, HDCamera>();
        static List<Camera> s_Cleanup = new List<Camera>(); // Recycled to reduce GC pressure

        HDAdditionalCameraData m_AdditionalCameraData;

        public HDCamera(Camera cam)
        {
            camera = cam;
            frustum = new Frustum();
            frustumPlaneEquations = new Vector4[6];

            viewMatrixStereo = new Matrix4x4[2];
            projMatrixStereo = new Matrix4x4[2];

            postprocessRenderContext = new PostProcessRenderContext();
            m_AdditionalCameraData = cam.GetComponent<HDAdditionalCameraData>();
            Reset();
        }

        public void Update(PostProcessLayer postProcessLayer, FrameSettings frameSettings)
        {
            // If TAA is enabled projMatrix will hold a jittered projection matrix. The original,
            // non-jittered projection matrix can be accessed via nonJitteredProjMatrix.
            bool taaEnabled = camera.cameraType == CameraType.Game &&
                CoreUtils.IsTemporalAntialiasingActive(postProcessLayer) &&
                frameSettings.enablePostprocess;

            var nonJitteredCameraProj = camera.projectionMatrix;
            var cameraProj = taaEnabled
                ? postProcessLayer.temporalAntialiasing.GetJitteredProjectionMatrix(camera)
                : nonJitteredCameraProj;

            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(cameraProj, true); // Had to change this from 'false'
            var gpuView = camera.worldToCameraMatrix;
            var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(nonJitteredCameraProj, true);

            // In stereo, this corresponds to the center eye position
            var pos = camera.transform.position;
            cameraPositionWS = pos;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                // Zero out the translation component.
                gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
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

            taaFrameIndex = taaEnabled ? (uint)postProcessLayer.temporalAntialiasing.sampleIndex : 0;
            taaFrameRotation = new Vector2(Mathf.Sin(taaFrameIndex * (0.5f * Mathf.PI)),
                                           Mathf.Cos(taaFrameIndex * (0.5f * Mathf.PI)));

            viewMatrix = gpuView;
            projMatrix = gpuProj;
            nonJitteredProjMatrix = gpuNonJitteredProj;
            cameraPos = pos;
            detViewMatrix = viewMatrix.determinant;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                Matrix4x4 cameraDisplacement = Matrix4x4.Translate(cameraPos - prevCameraPos); // Non-camera-relative positions
                prevViewProjMatrix *= cameraDisplacement; // Now prevViewProjMatrix correctly transforms this frame's camera-relative positionWS
            }

            float n = camera.nearClipPlane;
            float f = camera.farClipPlane;

            // Analyze the projection matrix.
            // p[2][3] = (reverseZ ? 1 : -1) * (depth_0_1 ? 1 : 2) * (f * n) / (f - n)
            float scale     = projMatrix[2, 3] / (f * n) * (f - n);
            bool  depth_0_1 = Mathf.Abs(scale) < 1.5f;
            bool  reverseZ  = scale > 0;

            // http://www.humus.name/temp/Linearize%20depth.txt
            if (reverseZ)
            {
                depthBufferParam = new Vector4(-1 + f/n, 1, -1/f + 1/n, 1/f);
            }
            else
            {
                depthBufferParam = new Vector4(1 - f/n, f/n, 1/f - 1/n, 1/n);
            }

            frustum = Frustum.Create(viewProjMatrix, depth_0_1, reverseZ);

            // Left, right, top, bottom, near, far.
            for (int i = 0; i < 6; i++)
            {
                frustumPlaneEquations[i] = new Vector4(frustum.planes[i].normal.x, frustum.planes[i].normal.y, frustum.planes[i].normal.z, frustum.planes[i].distance);
            }

            m_LastFrameActive = Time.frameCount;

            m_ActualWidth = camera.pixelWidth;
            m_ActualHeight = camera.pixelHeight;
            var screenWidth = m_ActualWidth;
            var screenHeight = m_ActualHeight;
            if (frameSettings.enableStereo)
            {
                screenWidth = XRSettings.eyeTextureWidth;
                screenHeight = XRSettings.eyeTextureHeight;

                var xrDesc = XRSettings.eyeTextureDesc;
                m_ActualWidth = xrDesc.width;
                m_ActualHeight = xrDesc.height;

                ConfigureStereoMatrices();
            }

            // Unfortunately sometime (like in the HDCameraEditor) HDUtils.hdrpSettings can be null because of scripts that change the current pipeline...
            m_msaaSamples = HDUtils.hdrpSettings != null ? HDUtils.hdrpSettings.msaaSampleCount : MSAASamples.None;
            RTHandle.SetReferenceSize(m_ActualWidth, m_ActualHeight, frameSettings.enableMSAA, m_msaaSamples);

            int maxWidth = RTHandle.maxWidth;
            int maxHeight = RTHandle.maxHeight;
            m_CameraScaleBias.x = (float)m_ActualWidth / maxWidth;
            m_CameraScaleBias.y = (float)m_ActualHeight / maxHeight;

            screenSize = new Vector4(screenWidth, screenHeight, 1.0f / screenWidth, 1.0f / screenHeight);
        }

        // Stopgap method used to extract stereo combined matrix state.
        public void UpdateStereoDependentState(FrameSettings frameSettings, ref ScriptableCullingParameters cullingParams)
        {
            if (!frameSettings.enableStereo)
                return;

            // What constants in UnityPerPass need updating for stereo considerations?
            // _ViewProjMatrix - It is used directly for generating tesselation factors. This should be the same
            //                   across both eyes for consistency, and to keep shadow-generation eye-independent
            // _DetViewMatrix -  Used for isFrontFace determination, should be the same for both eyes. There is the scenario
            //                   where there might be multi-eye sets that are divergent enough where this assumption is not valid,
            //                   but that's a future problem
            // _InvProjParam -   Intention was for generating linear depths, but not currently used.  Will need to be stereo-ized if
            //                   actually needed.
            // _FrustumPlanes -  Also used for generating tesselation factors.  Should be fine to use the combined stereo VP
            //                   to calculate frustum planes.

            // TODO: Would it be worth calculating my own combined view/proj matrix in Update?
            // In engine, we modify the view and proj matrices accordingly in order to generate the single cull
            // * Get the center eye view matrix, and pull it back to cover both eyes
            // * Generated an expanded projection matrix (one method - max bound of left/right proj matrices)
            //   and move near/far planes to match near/far locations of proj matrices located at eyes.
            // I think using the cull matrices is valid, as long as I only use them for tess factors in shader.
            // Using them for other calculations (like light list generation) could be problematic.

            var stereoCombinedViewMatrix = cullingParams.cullStereoView;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                // This is pulled back from the center eye, so set the offset
                var translation = stereoCombinedViewMatrix.GetColumn(3);
                translation += centerEyeTranslationOffset;
                stereoCombinedViewMatrix.SetColumn(3, translation);
            }

            viewMatrix = stereoCombinedViewMatrix;
            var stereoCombinedProjMatrix = cullingParams.cullStereoProj;
            projMatrix = GL.GetGPUProjectionMatrix(stereoCombinedProjMatrix, true);

            detViewMatrix = viewMatrix.determinant;

            frustum = Frustum.Create(viewProjMatrix, true, true);

            // Left, right, top, bottom, near, far.
            for (int i = 0; i < 6; i++)
            {
                frustumPlaneEquations[i] = new Vector4(frustum.planes[i].normal.x, frustum.planes[i].normal.y, frustum.planes[i].normal.z, frustum.planes[i].distance);
            }
        }

        void ConfigureStereoMatrices()
        {
            for (uint eyeIndex = 0; eyeIndex < 2; eyeIndex++)
            {
                viewMatrixStereo[eyeIndex] = camera.GetStereoViewMatrix((Camera.StereoscopicEye)eyeIndex);

                projMatrixStereo[eyeIndex] = camera.GetStereoProjectionMatrix((Camera.StereoscopicEye)eyeIndex);
                projMatrixStereo[eyeIndex] = GL.GetGPUProjectionMatrix(projMatrixStereo[eyeIndex], true);
            }

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                var leftTranslation = viewMatrixStereo[0].GetColumn(3);
                var rightTranslation = viewMatrixStereo[1].GetColumn(3);
                var centerTranslation = (leftTranslation + rightTranslation) / 2;
                var centerOffset = -centerTranslation;
                centerOffset.w = 0;

                // TODO: Grabbing the CenterEye transform would be preferable, but XRNode.CenterEye
                // doesn't always seem to be valid.

                for (uint eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                {
                    var translation = viewMatrixStereo[eyeIndex].GetColumn(3);
                    translation += centerOffset;
                    viewMatrixStereo[eyeIndex].SetColumn(3, translation);
                }

                centerEyeTranslationOffset = centerOffset;
            }

            // TODO: Fetch the single cull matrix stuff
        }

        // Warning: different views can use the same camera!
        public long GetViewID()
        {
            if (camera.cameraType == CameraType.Game)
            {
                long viewID = camera.GetInstanceID();
                // Make it positive.
                viewID += (-(long)int.MinValue) + 1;
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

        // Set up UnityPerView CBuffer.
        public void SetupGlobalParams(CommandBuffer cmd)
        {
            cmd.SetGlobalMatrix(HDShaderIDs._ViewMatrix,                viewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewMatrix,             viewMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._ProjMatrix,                projMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvProjMatrix,             projMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._ViewProjMatrix,            viewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewProjMatrix,         viewProjMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._NonJitteredViewProjMatrix, nonJitteredViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevViewProjMatrix,        prevViewProjMatrix);
            cmd.SetGlobalVector(HDShaderIDs._CameraPositionWS,          cameraPositionWS);
            cmd.SetGlobalFloat( HDShaderIDs._DetViewMatrix,             detViewMatrix);
            cmd.SetGlobalVector(HDShaderIDs._ScreenSize,                screenSize);
            cmd.SetGlobalVector(HDShaderIDs._ScreenToTargetScale,       scaleBias);
            cmd.SetGlobalVector(HDShaderIDs._DepthBufferParam,          depthBufferParam);
            cmd.SetGlobalVector(HDShaderIDs._InvProjParam,              invProjParam);
            cmd.SetGlobalVector(HDShaderIDs._TaaFrameRotation,          taaFrameRotation);
            cmd.SetGlobalVectorArray(HDShaderIDs._FrustumPlanes,        frustumPlaneEquations);
        }

        public void SetupGlobalStereoParams(CommandBuffer cmd)
        {
            var viewProjStereo = new Matrix4x4[2];
            var invViewStereo = new Matrix4x4[2];
            var invProjStereo = new Matrix4x4[2];
            var invViewProjStereo = new Matrix4x4[2];

            for (uint eyeIndex = 0; eyeIndex < 2; eyeIndex++)
            {
                var proj = projMatrixStereo[eyeIndex];
                invProjStereo[eyeIndex] = proj.inverse;

                var view = viewMatrixStereo[eyeIndex];
                invViewStereo[eyeIndex] = view.inverse;

                viewProjStereo[eyeIndex] = proj * view;
                invViewProjStereo[eyeIndex] = viewProjStereo[eyeIndex].inverse;
            }

            // corresponds to UnityPerPassStereo
            // TODO: Migrate the other stereo matrices to HDRP-managed UnityPerPassStereo?
            cmd.SetGlobalMatrixArray(HDShaderIDs._ViewMatrixStereo, viewMatrixStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._ViewProjMatrixStereo, viewProjStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._InvViewMatrixStereo, invViewStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._InvProjMatrixStereo, invProjStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._InvViewProjMatrixStereo, invViewProjStereo);
        }
    }
}
