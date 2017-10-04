using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

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
        public Vector4[] frustumPlaneEquations;
        public Camera camera;

        public Matrix4x4 viewProjMatrix
        {
            get { return projMatrix * viewMatrix; }
        }

        public Matrix4x4 nonJitteredViewProjMatrix
        {
            get { return nonJitteredProjMatrix * viewMatrix; }
        }

        public bool isFirstFrame
        {
            get { return m_FirstFrame; }
        }

        public Vector4 invProjParam
        {
            // Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
            get { var p = projMatrix; return new Vector4(p.m20 / (p.m00 * p.m23), p.m21 / (p.m11 * p.m23), -1.0f / p.m23, (-p.m22 + p.m20 * p.m02 / p.m00 + p.m21 * p.m12 / p.m11) / p.m23); }
        }

        // View-projection matrix from the previous frame.
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

        // Always true for cameras that just got added to the pool - needed for previous matrices to
        // avoid one-frame jumps/hiccups with temporal effects (motion blur, TAA...)
        bool m_FirstFrame;

        public HDCamera(Camera cam)
        {
            camera = cam;
            frustumPlaneEquations = new Vector4[6];
            Reset();
        }

        public void Update(PostProcessLayer postProcessLayer)
        {
            // If TAA is enabled projMatrix will hold a jittered projection matrix. The original,
            // non-jittered projection matrix can be accessed via nonJitteredProjMatrix.
            bool taaEnabled = camera.cameraType == CameraType.Game
                && CoreUtils.IsTemporalAntialiasingActive(postProcessLayer);

            Matrix4x4 nonJitteredCameraProj = camera.projectionMatrix;
            Matrix4x4 cameraProj = taaEnabled
                ? postProcessLayer.temporalAntialiasing.GetJitteredProjectionMatrix(camera)
                : nonJitteredCameraProj;

            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(cameraProj, true); // Had to change this from 'false'
            Matrix4x4 gpuView = camera.worldToCameraMatrix;
            Matrix4x4 gpuNonJitteredProj = GL.GetGPUProjectionMatrix(nonJitteredCameraProj, true);

            Vector3 pos = camera.transform.position;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                // Zero out the translation component.
                gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
            }

            Matrix4x4 gpuVP = gpuNonJitteredProj * gpuView;

            // A camera could be rendered multiple times per frame, only updates the previous view proj & pos if needed
            if (m_LastFrameActive != Time.frameCount)
            {
                if (m_FirstFrame)
                {
                    prevCameraPos = pos;
                    prevViewProjMatrix = gpuVP;
                }
                else
                {
                    prevCameraPos = cameraPos;
                    prevViewProjMatrix = nonJitteredViewProjMatrix;
                }

                m_FirstFrame = false;
            }

            viewMatrix = gpuView;
            projMatrix = gpuProj;
            nonJitteredProjMatrix = gpuNonJitteredProj;
            cameraPos = pos;
            screenSize = new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight);

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(viewProjMatrix);

            for (int i = 0; i < 6; i++)
            {
                frustumPlaneEquations[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
            }

            m_LastFrameActive = Time.frameCount;
        }

        public void Reset()
        {
            m_LastFrameActive = -1;
            m_FirstFrame = true;
        }

        static Dictionary<Camera, HDCamera> m_Cameras = new Dictionary<Camera, HDCamera>();
        static List<Camera> m_Cleanup = new List<Camera>(); // Recycled to reduce GC pressure

        // Grab the HDCamera tied to a given Camera and update it.
        public static HDCamera Get(Camera camera, PostProcessLayer postProcessLayer)
        {
            HDCamera hdcam;

            if (!m_Cameras.TryGetValue(camera, out hdcam))
            {
                hdcam = new HDCamera(camera);
                m_Cameras.Add(camera, hdcam);
            }

            hdcam.Update(postProcessLayer);
            return hdcam;
        }

        // Look for any camera that hasn't been used in the last frame and remove them for the pool.
        public static void CleanUnused()
        {
            int frameCheck = Time.frameCount - 1;

            foreach (var kvp in m_Cameras)
            {
                if (kvp.Value.m_LastFrameActive != frameCheck)
                    m_Cleanup.Add(kvp.Key);
            }

            foreach (var cam in m_Cleanup)
                m_Cameras.Remove(cam);

            m_Cleanup.Clear();
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
            cmd.SetGlobalVector(HDShaderIDs._InvProjParam, invProjParam);
            cmd.SetGlobalVector(HDShaderIDs._ScreenSize, screenSize);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevViewProjMatrix, prevViewProjMatrix);
            cmd.SetGlobalVectorArray(HDShaderIDs._FrustumPlanes, frustumPlaneEquations);
        }

        // Does not modify global settings. Used for shadows, low res. rendering, etc.
        public void OverrideGlobalParams(Material material)
        {
            material.SetMatrix(HDShaderIDs._ViewMatrix, viewMatrix);
            material.SetMatrix(HDShaderIDs._InvViewMatrix, viewMatrix.inverse);
            material.SetMatrix(HDShaderIDs._ProjMatrix, projMatrix);
            material.SetMatrix(HDShaderIDs._InvProjMatrix, projMatrix.inverse);
            material.SetMatrix(HDShaderIDs._NonJitteredViewProjMatrix, nonJitteredViewProjMatrix);
            material.SetMatrix(HDShaderIDs._ViewProjMatrix, viewProjMatrix);
            material.SetMatrix(HDShaderIDs._InvViewProjMatrix, viewProjMatrix.inverse);
            material.SetVector(HDShaderIDs._InvProjParam, invProjParam);
            material.SetVector(HDShaderIDs._ScreenSize, screenSize);
            material.SetMatrix(HDShaderIDs._PrevViewProjMatrix, prevViewProjMatrix);
            material.SetVectorArray(HDShaderIDs._FrustumPlanes, frustumPlaneEquations);
        }

        public void SetupComputeShader(ComputeShader cs, CommandBuffer cmd)
        {
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._ViewMatrix, viewMatrix);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._InvViewMatrix, viewMatrix.inverse);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._ProjMatrix, projMatrix);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._InvProjMatrix, projMatrix.inverse);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._NonJitteredViewProjMatrix, nonJitteredViewProjMatrix);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._ViewProjMatrix, viewProjMatrix);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._InvViewProjMatrix, viewProjMatrix.inverse);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._InvProjParam, invProjParam);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ScreenSize, screenSize);
            cmd.SetComputeMatrixParam(cs, HDShaderIDs._PrevViewProjMatrix, prevViewProjMatrix);
            cmd.SetComputeVectorArrayParam(cs, HDShaderIDs._FrustumPlanes, frustumPlaneEquations);
            // Copy values set by Unity which are not configured in scripts.
            cmd.SetComputeVectorParam(cs, HDShaderIDs.unity_OrthoParams, Shader.GetGlobalVector(HDShaderIDs.unity_OrthoParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProjectionParams, Shader.GetGlobalVector(HDShaderIDs._ProjectionParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ScreenParams, Shader.GetGlobalVector(HDShaderIDs._ScreenParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ZBufferParams, Shader.GetGlobalVector(HDShaderIDs._ZBufferParams));
            cmd.SetComputeVectorParam(cs, HDShaderIDs._WorldSpaceCameraPos, Shader.GetGlobalVector(HDShaderIDs._WorldSpaceCameraPos));
        }
    }
}
