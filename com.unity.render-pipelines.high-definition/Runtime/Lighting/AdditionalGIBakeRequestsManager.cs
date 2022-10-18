#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A manager to enqueue extra probe rendering outside of probe volumes.
    /// </summary>
    public class AdditionalGIBakeRequestsManager
    {
        // The baking ID for the extra requests
        // TODO: Need to ensure this never conflicts with bake IDs from others interacting with the API.
        // In our project, this is ProbeVolumes.
        internal static readonly int s_BakingID = 912345678;

        private static AdditionalGIBakeRequestsManager s_Instance = new AdditionalGIBakeRequestsManager();
        /// <summary>
        /// Get the manager that governs the additional light probe rendering requests.
        /// </summary>
        public static AdditionalGIBakeRequestsManager instance { get { return s_Instance; } }

        private AdditionalGIBakeRequestsManager()
        {
            if (!Application.isPlaying)
            {
                SubscribeOnBakeStarted();
            }
        }

        ~AdditionalGIBakeRequestsManager()
        {
            if (!Application.isPlaying)
            {
                UnsubscribeOnBakeStarted();
            }
        }

        private static List<SphericalHarmonicsL2> m_SHCoefficients = new List<SphericalHarmonicsL2>();
        private static List<Vector3> m_RequestPositions = new List<Vector3>();
        private static Vector3[] m_RequestPositionsSanitized;
        private static int m_FreelistHead = -1;
        private static bool m_RequestToLightmapperIsSet = false;

        private static readonly Vector2 s_FreelistSentinel = new Vector2(float.MaxValue, float.MaxValue);

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name ="capturePosition"> The position at which a probe is baked.</param>
        /// <returns>An ID that can be used to retrieve the data once it has been computed</returns>
        public int EnqueueRequest(Vector3 capturePosition)
        {
            Debug.Assert(ComputeCapturePositionIsValid(capturePosition));

            if (m_FreelistHead >= 0)
            {
                int requestID = m_FreelistHead;
                Debug.Assert(requestID < m_RequestPositions.Count);
                m_FreelistHead = ComputeFreelistNext(m_RequestPositions[requestID]);
                m_RequestPositions[requestID] = capturePosition;
                m_SHCoefficients[requestID] = new SphericalHarmonicsL2();
                return requestID;
            }
            else
            {
                int requestID = m_RequestPositions.Count;
                m_RequestPositions.Add(capturePosition);
                m_SHCoefficients.Add(new SphericalHarmonicsL2());
                return requestID;
            }
        }

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name ="requestID"> An ID that can be used to retrieve the data once it has been computed</param>
        /// <returns>An ID that can be used to retrieve the data once it has been computed</returns>
        public void DequeueRequest(int requestID)
        {
            Debug.Assert(requestID >= 0 && requestID < m_RequestPositions.Count);

            m_RequestPositions[requestID] = new Vector3(s_FreelistSentinel.x, s_FreelistSentinel.y, m_FreelistHead);
            m_SHCoefficients[requestID] = new SphericalHarmonicsL2();
            m_FreelistHead = requestID;
        }

        private bool ComputeCapturePositionIsValid(Vector3 capturePosition)
        {
            return !((capturePosition.x == s_FreelistSentinel.x) && (capturePosition.y == s_FreelistSentinel.y));
        }

        private int ComputeFreelistNext(Vector3 capturePosition)
        {
            Debug.Assert(ComputeRequestIsFree(capturePosition));

            int freelistNext = (int)capturePosition.z;
            Debug.Assert(freelistNext >= -1 && freelistNext < m_RequestPositions.Count);
            return freelistNext;
        }

        private bool ComputeRequestIsFree(int requestID)
        {
            Debug.Assert(requestID >= 0 && requestID < m_RequestPositions.Count);
            Vector3 requestPosition = m_RequestPositions[requestID];
            return ComputeRequestIsFree(requestPosition);
        }

        private bool ComputeRequestIsFree(Vector3 capturePosition)
        {
            return (capturePosition.x == s_FreelistSentinel.x) && (capturePosition.y == s_FreelistSentinel.y);
        }

        /// <summary>
        /// Retrieve the result of a capture request, it will return false if the request has not been fulfilled yet or the request ID is invalid.
        /// </summary>
        /// <param name ="requestID"> The request ID that has been given by the manager through a previous EnqueueRequest.</param>
        /// <param name ="sh"> The output SH coefficients that have been computed.</param>
        /// <returns>Whether the request for light probe rendering has been fulfilled and sh is valid.</returns>
        public bool RetrieveProbeSH(int requestID, out SphericalHarmonicsL2 sh)
        {
            if (requestID >= 0 && requestID < m_SHCoefficients.Count
                && ComputeCapturePositionIsValid(m_RequestPositions[requestID]))
            {
                sh = m_SHCoefficients[requestID];
                return true;
            }
            else
            {
                sh = new SphericalHarmonicsL2();
                return false;
            }
        }

        /// <summary>
        /// Update the capture location for the probe request.
        /// </summary>
        /// <param name ="requestID"> The request ID that has been given by the manager through a previous EnqueueRequest.</param>
        /// <param name ="newPositionnewPosition"> The position at which a probe is baked.</param>
        public int UpdatePositionForRequest(int requestID, Vector3 newPosition)
        {
            if (requestID >= 0 && requestID < m_RequestPositions.Count)
            {
                Debug.Assert(ComputeCapturePositionIsValid(m_RequestPositions[requestID]));
                m_RequestPositions[requestID] = newPosition;
                m_SHCoefficients[requestID] = new SphericalHarmonicsL2();
                return requestID;
            }
            else
            {
                return EnqueueRequest(newPosition);
            }
        }

        private void SubscribeOnBakeStarted()
        {
            UnsubscribeOnBakeStarted();
            Lightmapping.bakeStarted += AddRequestsToLightmapper;
        }

        private void UnsubscribeOnBakeStarted()
        {
            Lightmapping.bakeStarted -= AddRequestsToLightmapper;
            RemoveRequestsFromLightmapper();
        }

        internal void AddRequestsToLightmapper()
        {
            RemoveRequestsFromLightmapper();

            int validRequestCount = ComputeValidRequestCount();
            if (validRequestCount == 0)
            {
                return;
            }

            EnsureRequestPositionsSanitized();

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_BakingID, m_RequestPositionsSanitized);
            m_RequestToLightmapperIsSet = true;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;
        }

        private void RemoveRequestsFromLightmapper()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_BakingID, null);
            m_RequestToLightmapperIsSet = false;
        }

        private void OnAdditionalProbesBakeCompleted()
        {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;

            if (!m_RequestToLightmapperIsSet) { return; }

            var sh = new NativeArray<SphericalHarmonicsL2>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bakedProbeOctahedralDepth = new NativeArray<float>(m_RequestPositions.Count * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(s_BakingID, sh, validity, bakedProbeOctahedralDepth))
            {
                SetSHCoefficients(sh);
                PushSHCoefficientsToReflectionProbes();
            }
            else
            {
                Debug.LogWarning("Warning: AdditionalGIBakeRequestsManager: Request to lightmapper was set, but bake completed with no result.");
            }

            sh.Dispose();
            validity.Dispose();
            bakedProbeOctahedralDepth.Dispose();
        }

        private void SetSHCoefficients(NativeArray<SphericalHarmonicsL2> sh)
        {
            Debug.Assert(sh.Length == m_SHCoefficients.Count);
            for (int i = 0; i < sh.Length; ++i)
            {
                m_SHCoefficients[i] = sh[i];
            }
        }

        private void PushSHCoefficientsToReflectionProbes()
        {
            List<HDProbe> hdProbes = HDProbe.GetInstances();
            foreach (var hdProbe in hdProbes)
            {
                hdProbe.TryUpdateLuminanceSHL2ForNormalization();
            }
        }

        private int ComputeValidRequestCount()
        {
            int count = m_RequestPositions.Count;
            int freelistIndex = m_FreelistHead;

            // count > 0 check not technically necessary.
            // Added here as a safe guard in case the freelist gets corrupted - we don't want to hang unity.
            while (freelistIndex != -1 && count > 0)
            {
                Debug.Assert(freelistIndex >= 0 && freelistIndex < m_RequestPositions.Count);

                --count;
                freelistIndex = ComputeFreelistNext(m_RequestPositions[freelistIndex]);   
            }
            return count;
        }

        private int FindFirstValidCapturePositionIndex()
        {
            for (int i = 0; i < m_RequestPositions.Count; ++i)
            {
                if (ComputeCapturePositionIsValid(m_RequestPositions[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private void EnsureRequestPositionsSanitized()
        {
            if (m_RequestPositionsSanitized == null || m_RequestPositionsSanitized.Length != m_RequestPositions.Count)
            {
                m_RequestPositionsSanitized = new Vector3[m_RequestPositions.Count];
            }

            int firstValidCapturePositionIndex = FindFirstValidCapturePositionIndex();
            Vector3 firstValidCapturePosition = (firstValidCapturePositionIndex == -1) ? Vector3.zero : m_RequestPositions[firstValidCapturePositionIndex];

            for (int i = 0; i < m_RequestPositions.Count; ++i)
            {
                m_RequestPositionsSanitized[i] = ComputeCapturePositionIsValid(m_RequestPositions[i]) ? m_RequestPositions[i] : firstValidCapturePosition;
            }
        }
    }
}
#endif