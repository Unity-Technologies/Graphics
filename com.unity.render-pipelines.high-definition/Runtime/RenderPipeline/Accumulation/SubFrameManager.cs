using System;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
    using UnityEditor;
#endif // UNITY_EDITOR

namespace UnityEngine.Rendering.HighDefinition
{
    // Helper class to manage time-scale in Unity when recording multi-frame sequences where one final frame is an accumulation of multiple sub-frames
    internal class SubFrameManager
    {
        // Shutter settings
        float m_ShutterInterval = 0.0f;
        bool m_Centered = true;
        float m_ShutterFullyOpen = 0.0f;
        float m_ShutterBeginsClosing = 1.0f;

        AnimationCurve m_ShutterCurve;

        // Internal state
        float m_OriginalTimeScale = 0;
        float m_OriginalFixedDeltaTime = 0;
        bool m_IsRenderingTheFirstFrame = true;
        float m_AccumulatedWeight = 0;

        // The number of sub-frames that will be used to reconstruct a converged frame
        public uint subFrameCount
        {
            get { return m_AccumulationSamples; }
            set { m_AccumulationSamples = value; }
        }
        uint m_AccumulationSamples = 0;

        // The sequence number of the current sub-frame
        public uint iteration
        {
            get { return m_CurrentIteration; }
        }
        uint m_CurrentIteration = 0;

        // True when a recording session is in progress
        public bool isRecording
        {
            get { return m_IsRecording; }
        }
        bool m_IsRecording = false;

        // Resets the sub-frame sequence
        internal void Reset()
        {
            m_CurrentIteration = 0;
            m_AccumulatedWeight = 0;
        }

        // Advances the sub-frame sequence
        internal void Advance()
        {
            // Increment the iteration counter, if we haven't converged yet
            if (m_CurrentIteration < m_AccumulationSamples)
                m_CurrentIteration++;
        }

        void Init(int samples, float shutterInterval)
        {
            m_AccumulationSamples = (uint)samples;
            m_ShutterInterval = samples > 1 ? shutterInterval : 0;
            m_IsRecording = true;
            m_IsRenderingTheFirstFrame = true;
            Reset();

            m_OriginalTimeScale = Time.timeScale;

            Time.timeScale = m_OriginalTimeScale * m_ShutterInterval / m_AccumulationSamples;

            if (m_Centered && m_IsRenderingTheFirstFrame)
            {
                Time.timeScale *= 0.5f;
            }

            m_OriginalFixedDeltaTime = Time.fixedDeltaTime;
            Time.fixedDeltaTime = Time.captureDeltaTime * Time.timeScale;
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
            Time.timeScale = m_OriginalTimeScale;
            Time.fixedDeltaTime = m_OriginalFixedDeltaTime;
            m_ShutterCurve = null;
        }

        // Should be called before rendering a new frame in a sequence (when accumulation is desired)
        internal void PrepareNewSubFrame()
        {
            if (m_CurrentIteration == m_AccumulationSamples)
            {
                Reset();
            }

            if (m_CurrentIteration == m_AccumulationSamples - 1)
            {
                Time.timeScale = m_OriginalTimeScale * (1.0f - m_ShutterInterval);
                m_IsRenderingTheFirstFrame = false;
            }
            else
            {
                Time.timeScale = m_OriginalTimeScale * m_ShutterInterval / m_AccumulationSamples;
            }

            if (m_Centered && m_IsRenderingTheFirstFrame)
            {
                Time.timeScale *= 0.5f;
            }
            Time.fixedDeltaTime = Time.captureDeltaTime * Time.timeScale;
        }

        // Helper function to compute the weight of a frame for a specific point in time
        float ShutterProfile(float time)
        {
            // for the first frame we are missing the first half when doing centered mb
            if (m_IsRenderingTheFirstFrame && m_Centered)
            {
                time = time * 0.5f + 0.5f;
            }

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
        internal Vector4 GetFrameWeights()
        {
            float totalWeight = m_AccumulatedWeight;
            float time = m_AccumulationSamples > 0 ? (float) m_CurrentIteration / m_AccumulationSamples : 0.0f;

            float weight = isRecording ? ShutterProfile(time) : 1.0f;

            if (m_CurrentIteration < m_AccumulationSamples)
                m_AccumulatedWeight += weight;

            if (m_AccumulatedWeight > 0)
            {
                return new Vector4(weight, totalWeight, 1.0f / m_AccumulatedWeight, 0.0f);
            }
            else
            {
                return new Vector4(weight, totalWeight, 0.0f, 0.0f);
            }
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

        void RenderAccumulation(HDCamera hdCamera, CommandBuffer cmd, RTHandle inputTexture, RTHandle outputTexture, bool needsExposure = false)
        {
            ComputeShader accumulationShader = m_Asset.renderPipelineResources.shaders.accumulationCS;

            // Grab the history buffer (hijack the reflections one)
            RTHandle history = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.PathTracing)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.PathTracing, PathTracingHistoryBufferAllocatorFunction, 1);

            // Check the validity of the state before moving on with the computation
            if (!accumulationShader)
                return;

            uint currentIteration = m_SubFrameManager.iteration;

            // Accumulate the path tracing results
            int kernel = accumulationShader.FindKernel("KMain");
            cmd.SetGlobalInt(HDShaderIDs._AccumulationFrameIndex, (int)currentIteration);
            cmd.SetGlobalInt(HDShaderIDs._AccumulationNumSamples, (int)m_SubFrameManager.subFrameCount);
            cmd.SetComputeTextureParam(accumulationShader, kernel, HDShaderIDs._AccumulatedFrameTexture, history);
            cmd.SetComputeTextureParam(accumulationShader, kernel, HDShaderIDs._CameraColorTextureRW, outputTexture);
            cmd.SetComputeTextureParam(accumulationShader, kernel, HDShaderIDs._RadianceTexture, inputTexture);
            cmd.SetComputeVectorParam(accumulationShader, HDShaderIDs._AccumulationWeights, m_SubFrameManager.GetFrameWeights());
            cmd.SetComputeIntParam(accumulationShader, HDShaderIDs._AccumulationNeedsExposure, needsExposure ? 1 : 0);
            cmd.DispatchCompute(accumulationShader, kernel, (hdCamera.actualWidth + 7) / 8, (hdCamera.actualHeight + 7) / 8, hdCamera.viewCount);

            m_SubFrameManager.Advance();
        }
    }

}
