#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEditor;
using static UnityEngine.Rendering.HighDefinition.VolumeGlobalUniqueIDUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A manager to enqueue extra probe rendering outside of probe volumes.
    /// </summary>
    public class AdditionalGIBakeRequestsManager
    {
        // The baking ID for the extra requests
        private static int s_AdditionalGIBakeRequestsBakingID = 0;
        private static readonly int s_LightmapperBakeIDStart = 1;

        private static AdditionalGIBakeRequestsManager s_Instance = new AdditionalGIBakeRequestsManager();
        /// <summary>
        /// Get the manager that governs the additional light probe rendering requests.
        /// </summary>
        public static AdditionalGIBakeRequestsManager instance { get { return s_Instance; } }

        private AdditionalGIBakeRequestsManager()
        {
            if (!Application.isPlaying)
            {
                lightmapperBakeIDFromBakeID.Clear();
                lightmapperBakeIDNext = 0;

                SubscribeOnBakeStarted();
            }
        }

        ~AdditionalGIBakeRequestsManager()
        {
            if (!Application.isPlaying)
            {
                UnsubscribeOnBakeStarted();

                lightmapperBakeIDFromBakeID.Clear();
                lightmapperBakeIDNext = 0;
            }
        }

        private static List<SphericalHarmonicsL2> m_SHCoefficients = new List<SphericalHarmonicsL2>();
        private static List<Vector3> m_RequestPositions = new List<Vector3>();
        private static Vector3[] m_RequestPositionsSanitized;
        private static int m_FreelistHead = -1;
        private static bool m_RequestToLightmapperIsSet = false;

        private static readonly Vector2 s_FreelistSentinel = new Vector2(float.MaxValue, float.MaxValue);

        // Lightmapper API uses ints as keys, but we want to use full, stable, GlobalObjectIds as keys.
        // Rather than hashing and hoping we don't collide, lets handle this robustly by
        // keeping a dictionary of VolumeGlobalUniqueID->int bit keys.
        private Dictionary<VolumeGlobalUniqueID, int> lightmapperBakeIDFromBakeID = new Dictionary<VolumeGlobalUniqueID, int>(32);
        private int lightmapperBakeIDNext = s_LightmapperBakeIDStart;

        internal void SetAdditionalBakedProbes(VolumeGlobalUniqueID bakeID, Vector3[] positions)
        {
            if (TryGetLightmapperBakeIDFromBakeID(bakeID, out int lightmapperBakeID))
            {
                UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(lightmapperBakeID, null);

                // When baking, the lightmapper hashes its state (i.e: the list of all AdditionalBakedProbes requests)
                // and only bakes data if this hash is not changed.
                // By storing a generation ID inside of our lightmapperBakeID, we ensure that Sets will always look like a completely new bake request to the lightmapper.
                // The lightmapper will always bake it.
                // Without storing this generation index, if we clear our bake request by setting positions to NULL, then set our bake request with valid data,
                // then bake, the lightmapper will treat the new bake request as an already completed old one, and skip doing any work.
                // In the future, after proving out this generation based approach, it would be a good idea to move this generation tracking code into the lightmapper,
                // so that users dont need to do this bookkeeping for the lightmapper - they can simply set and clear requests and always get the correct, fresh results.
                IncrementLightmapperBakeIDGeneration(bakeID, out lightmapperBakeID);

                if (positions != null)
                {
                    UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(lightmapperBakeID, positions);
                }                
            }
        }

        internal bool GetAdditionalBakedProbes(VolumeGlobalUniqueID bakeID, NativeArray<SphericalHarmonicsL2> outBakedProbeSH, NativeArray<float> outBakedProbeValidity, NativeArray<float> outBakedProbeOctahedralDepth)
        {
            bool success = false;
            if (TryGetLightmapperBakeIDFromBakeID(bakeID, out int lightmapperBakeID))
            {
                success = UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(lightmapperBakeID, outBakedProbeSH, outBakedProbeValidity, outBakedProbeOctahedralDepth);
            }
            return success;
        }

        private bool TryGetLightmapperBakeIDFromBakeID(VolumeGlobalUniqueID bakeID, out int lightmapperBakeID)
        {
            bool success = false;
            if (lightmapperBakeIDFromBakeID.TryGetValue(bakeID, out lightmapperBakeID))
            {
                success = true;
            }
            // Leave the whole top bit free. We shouldn't encounter it in practice, avoiding it allows us to not worry about handling the signed case.
            else if (lightmapperBakeIDNext == ((1 << 23) - 1))
            {
                success = false;
                lightmapperBakeID = -1;
                Debug.LogWarningFormat("Error: Used up all lightmapper bake IDs. This should never happen. Somehow all {0} ids have been used up. This must be the result of a bug. Unlikely that you created and baked {0} unique bake requests. Quit and reopen unity to flush all IDs.", (1 << 23) - 1);
            }
            else
            {
                success = true;
                lightmapperBakeID = lightmapperBakeIDNext << 8;
                ++lightmapperBakeIDNext;
                lightmapperBakeIDFromBakeID.Add(bakeID, lightmapperBakeID);
            }

            return success;
        }

        private void IncrementLightmapperBakeIDGeneration(VolumeGlobalUniqueID bakeID, out int lightmapperBakeID)
        {
            lightmapperBakeID = -1;
            if (lightmapperBakeIDFromBakeID.TryGetValue(bakeID, out lightmapperBakeID))
            {
                IncrementLightmapperBakeIDGeneration(ref lightmapperBakeID);
                lightmapperBakeIDFromBakeID[bakeID] = lightmapperBakeID;
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private static void IncrementLightmapperBakeIDGeneration(ref int lightmapperBakeID)
        {
            const int MASK = 255;
            int generationIndex = lightmapperBakeID & MASK;
            generationIndex = (generationIndex == MASK) ? 0 : (generationIndex + 1);

            lightmapperBakeID &= ~MASK;
            lightmapperBakeID |= generationIndex;
        }

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

            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_AdditionalGIBakeRequestsBakingID, null);
            IncrementLightmapperBakeIDGeneration(ref s_AdditionalGIBakeRequestsBakingID);
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_AdditionalGIBakeRequestsBakingID, m_RequestPositionsSanitized);
            m_RequestToLightmapperIsSet = true;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesBakeCompleted;
        }

        private void RemoveRequestsFromLightmapper()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_AdditionalGIBakeRequestsBakingID, null);
            m_RequestToLightmapperIsSet = false;
        }

        private void OnAdditionalProbesBakeCompleted()
        {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesBakeCompleted;

            if (!m_RequestToLightmapperIsSet) { return; }

            var sh = new NativeArray<SphericalHarmonicsL2>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bakedProbeOctahedralDepth = new NativeArray<float>(m_RequestPositions.Count * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(s_AdditionalGIBakeRequestsBakingID, sh, validity, bakedProbeOctahedralDepth))
            {
                SetSHCoefficients(sh);
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
