using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
    using UnityEditor;
#endif // UNITY_EDITOR

namespace UnityEngine.Rendering.HighDefinition
{
    // Struct storing per-camera data, to handle accumulation and dirtiness
    internal struct CameraData
    {
        public void ResetIteration()
        {
            accumulatedWeight = 0.0f;
            currentIteration = 0;
        }

        public uint width;
        public uint height;
        public bool skyEnabled;
        public bool fogEnabled;

        public float accumulatedWeight;
        public uint  currentIteration;
    }

    // Helper class to manage time-scale in Unity when recording multi-frame sequences where one final frame is an accumulation of multiple sub-frames
    internal class SubFrameManager
    {
        // Shutter settings
        float m_ShutterInterval = 0.0f;
        float m_ShutterFullyOpen = 0.0f;
        float m_ShutterBeginsClosing = 1.0f;

        AnimationCurve m_ShutterCurve;

        // Internal state
        float m_OriginalCaptureDeltaTime = 0;
        float m_OriginalFixedDeltaTime = 0;

        // Per-camera data cache
        Dictionary<int, CameraData> m_CameraCache = new Dictionary<int, CameraData>();

        internal CameraData GetCameraData(int camID)
        {
            CameraData camData;
            if (!m_CameraCache.TryGetValue(camID, out camData))
            {
                camData.ResetIteration();
                m_CameraCache.Add(camID, camData);
            }
            return camData;
        }

        internal void SetCameraData(int camID, CameraData camData)
        {
            m_CameraCache[camID] = camData;
        }

        // The number of sub-frames that will be used to reconstruct a converged frame
        public uint subFrameCount
        {
            get { return m_AccumulationSamples; }
            set { m_AccumulationSamples = value; }
        }
        uint m_AccumulationSamples = 0;

        // True when a recording session is in progress
        public bool isRecording
        {
            get { return m_IsRecording; }
        }
        bool m_IsRecording = false;

        // Resets the sub-frame sequence
        internal void Reset(int camID)
        {
            CameraData camData = GetCameraData(camID);
            camData.ResetIteration();
            SetCameraData(camID, camData);
        }
        internal void Reset()
        {
            foreach (int camID in m_CameraCache.Keys.ToList())
                Reset(camID);
        }
        internal void Clear()
        {
            m_CameraCache.Clear();
        }
        internal void SelectiveReset(uint maxSamples)
        {
            foreach (int camID in m_CameraCache.Keys.ToList())
            {
                CameraData camData = GetCameraData(camID);
                if (camData.currentIteration >= maxSamples)
                {
                    camData.ResetIteration();
                    SetCameraData(camID, camData);
                }
            }
        }

        void Init(int samples, float shutterInterval)
        {
            m_AccumulationSamples = (uint)samples;
            m_ShutterInterval = samples > 1 ? shutterInterval : 0;
            m_IsRecording = true;

            Clear();

            m_OriginalCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = m_OriginalCaptureDeltaTime / m_AccumulationSamples;

            // This is required for physics simulations
            m_OriginalFixedDeltaTime = Time.fixedDeltaTime;
            Time.fixedDeltaTime = m_OriginalFixedDeltaTime / m_AccumulationSamples;
        }

        internal void BeginRecording(int samples, float shutterInterval, float shutterFullyOpen = 0.0f, float shutterBeginsClosing = 1.0f)
        {
            Init(samples, shutterInterval);

            m_ShutterFullyOpen = shutterFullyOpen;
            m_ShutterBeginsClosing = shutterBeginsClosing;
        }

        internal void BeginRecording(int samples, float shutterInterval, AnimationCurve shutterProfile)
        {
            Init(samples, shutterInterval);

            m_ShutterCurve = shutterProfile;
        }

        internal void EndRecording()
        {
            m_IsRecording = false;
            Time.captureDeltaTime = m_OriginalCaptureDeltaTime;
            Time.fixedDeltaTime = m_OriginalFixedDeltaTime;
            m_ShutterCurve = null;
        }

        // Should be called before rendering a new frame in a sequence (when accumulation is desired)
        internal void PrepareNewSubFrame()
        {
            uint maxIteration = 0;
            foreach (int camID in m_CameraCache.Keys.ToList())
                maxIteration = Math.Max(maxIteration, GetCameraData(camID).currentIteration);

            if (maxIteration == m_AccumulationSamples)
            {
                Reset();
            }
        }

        // Helper function to compute the weight of a frame for a specific point in time
        float ShutterProfile(float time)
        {
            if (time > m_ShutterInterval)
            {
                return 0;
            }

            // Scale the subframe time so the m_ShutterInterval spans between 0 and 1
            time = time / m_ShutterInterval;

            // In case we have a curve profile, use this and return
            if (m_ShutterCurve != null)
            {
                return m_ShutterCurve.Evaluate(time);
            }

            // Otherwise use linear open and closing times
            if (time < m_ShutterFullyOpen)
            {
                float openingSlope = 1.0f / m_ShutterFullyOpen;
                return openingSlope * time;
            }
            else if (time > m_ShutterBeginsClosing)
            {
                float closingSlope = 1.0f / (1.0f - m_ShutterBeginsClosing);
                return 1.0f - closingSlope * (time - m_ShutterBeginsClosing);
            }
            else
            {
                return 1.0f;
            }
        }

        // returns the accumulation weights for the current sub-frame
        // x: weight for the current frame
        // y: sum of weights until now, without the current frame
        // z: one over the sum of weights until now, including the current frame
        // w: unused
        internal Vector4 ComputeFrameWeights(int camID)
        {
            CameraData camData = GetCameraData(camID);

            float totalWeight = camData.accumulatedWeight;
            float time = m_AccumulationSamples > 0 ? (float) camData.currentIteration / m_AccumulationSamples : 0.0f;

            float weight = isRecording ? ShutterProfile(time) : 1.0f;

            if (camData.currentIteration < m_AccumulationSamples)
                camData.accumulatedWeight += weight;

            SetCameraData(camID, camData);

            return (camData.accumulatedWeight > 0) ?
                new Vector4(weight, totalWeight, 1.0f / camData.accumulatedWeight, 0.0f) :
                new Vector4(weight, totalWeight, 0.0f, 0.0f);
        }
    }


    public partial class HDRenderPipeline
    {
        SubFrameManager m_SubFrameManager = new SubFrameManager();

        // Public API for multi-frame recording

        /// <summary>
        /// Should be called to start a multi-frame recording session. Each final frame will be an accumulation of multiple sub-frames.
        /// </summary>
        /// <param name="samples">The number of subframes. Each recorded frame will be an accumulation of this number of framesIn case path tracing is enabled, this value will override the settign in the volume.</param>
        /// <param name="shutterInterval">The duration the shutter of the virtual camera is open (for motion blur). Between 0 and 1.</param>
        /// <param name="shutterFullyOpen">The time it takes for the shutter to fully open. Between 0 and 1.</param>
        /// <param name="shutterBeginsClosing">The time when the shutter starts closing. Between 0 and 1.</param>
        public void BeginRecording(int samples, float shutterInterval, float shutterFullyOpen = 0.0f, float shutterBeginsClosing = 1.0f)
        {
            m_SubFrameManager.BeginRecording(samples, shutterInterval, shutterFullyOpen, shutterBeginsClosing);
        }

        /// <summary>
        /// Should be called to start a multi-frame recording session. Each final frame will be an accumulation of multiple sub-frames.
        /// </summary>
        /// <param name="samples">The number of subframes. Each recorded frame will be an accumulation of this number of frames. In case path tracing is enabled, this value will override the settign in the volume.</param>
        /// <param name="shutterInterval">The duration the shutter of the virtual camera is open (for motion blur). Between 0 and 1.</param>
        /// <param name="shutterProfile">An animation curve (between 0 and 1) denoting the motion of the camera shutter.</param>
        public void BeginRecording(int samples, float shutterInterval, AnimationCurve shutterProfile)
        {
            m_SubFrameManager.BeginRecording(samples, shutterInterval, shutterProfile);
        }

        /// <summary>
        /// Should be called to finish a multi-frame recording session
        /// </summary>
        public void EndRecording()
        {
            m_SubFrameManager.EndRecording();
        }

        /// <summary>
        /// Should be called during a recording session when preparing to render a new sub-frame of a multi-frame sequence where each final frame is an accumulation of multiple sub-frames.
        /// </summary>
        public void PrepareNewSubFrame()
        {
            m_SubFrameManager.PrepareNewSubFrame();
        }

        struct RenderAccumulationParameters
        {
            public ComputeShader    accumulationCS;
            public int              accumulationKernel;
            public SubFrameManager  subFrameManager;
            public bool             needExposure;
            public HDCamera         hdCamera;
        }

        RenderAccumulationParameters PrepareRenderAccumulationParameters(HDCamera hdCamera, bool needExposure, bool inputFromRadianceTexture)
        {
            var parameters = new RenderAccumulationParameters();

            parameters.accumulationCS = m_Asset.renderPipelineResources.shaders.accumulationCS;
            parameters.accumulationKernel = parameters.accumulationCS.FindKernel("KMain");
            parameters.subFrameManager = m_SubFrameManager;
            parameters.needExposure = needExposure;
            parameters.hdCamera = hdCamera;

            parameters.accumulationCS.shaderKeywords = null;
            if (inputFromRadianceTexture)
            {
                parameters.accumulationCS.EnableKeyword("INPUT_FROM_RADIANCE_TEXTURE");
            }
            return parameters;
        }

        void RenderAccumulation(HDCamera hdCamera, RTHandle inputTexture, RTHandle outputTexture, bool needExposure, CommandBuffer cmd)
        {
            // Grab the history buffer
            RTHandle history = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.PathTracing, PathTracingHistoryBufferAllocatorFunction, 1);

            bool inputFromRadianceTexture = !inputTexture.Equals(outputTexture);
            var parameters = PrepareRenderAccumulationParameters(hdCamera, needExposure, inputFromRadianceTexture);
            RenderAccumulation(parameters, inputTexture, outputTexture, history, cmd);
        }

        static void RenderAccumulation(in RenderAccumulationParameters parameters, RTHandle inputTexture, RTHandle outputTexture, RTHandle historyTexture, CommandBuffer cmd)
        {
            ComputeShader accumulationShader = parameters.accumulationCS;

            // Check the validity of the state before moving on with the computation
            if (!accumulationShader)
                return;

            // Get the per-camera data
            int camID = parameters.hdCamera.camera.GetInstanceID();
            Vector4 frameWeights = parameters.subFrameManager.ComputeFrameWeights(camID);
            CameraData camData = parameters.subFrameManager.GetCameraData(camID);

            // Accumulate the path tracing results
            cmd.SetComputeIntParam(accumulationShader, HDShaderIDs._AccumulationFrameIndex, (int)camData.currentIteration);
            cmd.SetComputeIntParam(accumulationShader, HDShaderIDs._AccumulationNumSamples, (int)parameters.subFrameManager.subFrameCount);
            cmd.SetComputeTextureParam(accumulationShader, parameters.accumulationKernel, HDShaderIDs._AccumulatedFrameTexture, historyTexture);
            cmd.SetComputeTextureParam(accumulationShader, parameters.accumulationKernel, HDShaderIDs._CameraColorTextureRW, outputTexture);
            if (!inputTexture.Equals(outputTexture))
            {
                cmd.SetComputeTextureParam(accumulationShader, parameters.accumulationKernel, HDShaderIDs._RadianceTexture, inputTexture);
            }
            cmd.SetComputeVectorParam(accumulationShader, HDShaderIDs._AccumulationWeights, frameWeights);
            cmd.SetComputeIntParam(accumulationShader, HDShaderIDs._AccumulationNeedsExposure, parameters.needExposure ? 1 : 0);
            cmd.DispatchCompute(accumulationShader, parameters.accumulationKernel, (parameters.hdCamera.actualWidth + 7) / 8, (parameters.hdCamera.actualHeight + 7) / 8, parameters.hdCamera.viewCount);

            // Increment the iteration counter, if we haven't converged yet
            if (camData.currentIteration < parameters.subFrameManager.subFrameCount)
            {
                camData.currentIteration++;
                parameters.subFrameManager.SetCameraData(camID, camData);
            }
        }
    }

}
