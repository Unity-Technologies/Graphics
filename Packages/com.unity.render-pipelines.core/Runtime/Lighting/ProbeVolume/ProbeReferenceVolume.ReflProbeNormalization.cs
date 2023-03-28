using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
#if UNITY_EDITOR

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

        internal void Init()
        {
            SubscribeOnBakeStarted();
        }

        internal void Cleanup()
        {
            UnsubscribeOnBakeStarted();
        }

        const float kInvalidSH = 1f;
        const float kValidSHThresh = 0.33f;

        private static Dictionary<int, SphericalHarmonicsL2> m_SHCoefficients = new Dictionary<int, SphericalHarmonicsL2>();
        private static Dictionary<int, float> m_SHValidity = new Dictionary<int, float>();
        private static Dictionary<int, Vector3> m_RequestPositions = new Dictionary<int, Vector3>();

        private static readonly Vector2 s_FreelistSentinel = new Vector2(float.MaxValue, float.MaxValue);

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name ="capturePosition"> The position at which a probe is baked.</param>
        /// <param name ="probeInstanceID"> The instance ID of the probe doing the request.</param>
        public void EnqueueRequest(Vector3 capturePosition, int probeInstanceID)
        {
            m_SHCoefficients[probeInstanceID] = new SphericalHarmonicsL2();
            m_SHValidity[probeInstanceID] = kInvalidSH;
            m_RequestPositions[probeInstanceID] = capturePosition;
        }

        /// <summary>
        /// Dequeue a request for probe rendering.
        /// </summary>
        /// <param name ="probeInstanceID">The instance ID of the probe for which we want to dequeue a request. </param>
        public void DequeueRequest(int probeInstanceID)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                m_SHCoefficients.Remove(probeInstanceID);
                m_SHValidity.Remove(probeInstanceID);
                m_RequestPositions.Remove(probeInstanceID);
            }
        }

        /// <summary>
        /// Retrieve the result of a capture request, it will return false if the request has not been fulfilled yet or the request ID is invalid.
        /// </summary>
        /// <param name ="probeInstanceID"> The instance ID of the probe doing the request.</param>
        /// <param name ="sh"> The output SH coefficients that have been computed.</param>
        /// <param name ="pos"> The position for which the computed SH coefficients are valid.</param>
        /// <returns>Whether the request for light probe rendering has been fulfilled and sh is valid.</returns>
        public bool RetrieveProbeSH(int probeInstanceID, out SphericalHarmonicsL2 sh, out Vector3 pos)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                sh = m_SHCoefficients[probeInstanceID];
                pos = m_RequestPositions[probeInstanceID];
                return m_SHValidity[probeInstanceID] < kValidSHThresh;
            }

            sh = new SphericalHarmonicsL2();
            pos = Vector3.negativeInfinity;
            return false;
        }

        /// <summary>
        /// Update the capture location for the probe request.
        /// </summary>
        /// <param name ="probeInstanceID"> The instance ID of the probe doing the request and that wants the capture position updated.</param>
        /// <param name ="newPositionnewPosition"> The position at which a probe is baked.</param>
        public void UpdatePositionForRequest(int probeInstanceID, Vector3 newPosition)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                m_RequestPositions[probeInstanceID] = newPosition;
                m_SHCoefficients[probeInstanceID] = new SphericalHarmonicsL2();
                m_SHValidity[probeInstanceID] = kInvalidSH;
            }
            else
            {
                EnqueueRequest(newPosition, probeInstanceID);
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
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_BakingID, (new List<Vector3>(m_RequestPositions.Values)).ToArray());

            Lightmapping.bakeCompleted -= OnAdditionalProbesBakeCompleted;
            Lightmapping.bakeCompleted += OnAdditionalProbesBakeCompleted;
        }

        private void RemoveRequestsFromLightmapper()
        {
            UnityEditor.Experimental.Lightmapping.SetAdditionalBakedProbes(s_BakingID, null);
        }

        private void OnAdditionalProbesBakeCompleted()
        {
            Lightmapping.bakeCompleted -= OnAdditionalProbesBakeCompleted;

            if (m_RequestPositions.Count == 0) return;

            var sh = new NativeArray<SphericalHarmonicsL2>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var validity = new NativeArray<float>(m_RequestPositions.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bakedProbeOctahedralDepth = new NativeArray<float>(m_RequestPositions.Count * 64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            if (UnityEditor.Experimental.Lightmapping.GetAdditionalBakedProbes(s_BakingID, sh, validity, bakedProbeOctahedralDepth))
            {
                SetSHCoefficients(sh, validity);
            }
            else
            {
                Debug.LogWarning($"Failed to collect results for additional probes. (Bake Id {s_BakingID})");
                ClearSHCoefficients();
            }

            ProbeReferenceVolume.instance.retrieveExtraDataAction?.Invoke(new ProbeReferenceVolume.ExtraDataActionInput());

            sh.Dispose();
            validity.Dispose();
            bakedProbeOctahedralDepth.Dispose();
        }

        private void SetSHCoefficients(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            Debug.Assert(sh.Length == m_SHCoefficients.Count);
            Debug.Assert(sh.Length == validity.Length);

            List<int> requestsInstanceIDs = new List<int>(m_SHCoefficients.Keys);

            for (int i = 0; i < sh.Length; ++i)
            {
                var v = validity[i];
                var s = sh[i];

                if (v < kValidSHThresh)
                {
                    var hasNonZeroValue = false;
                    for (var r = 0; r < 3; ++r)
                    {
                        for (var c = 0; c < 9; ++c)
                        {
                            if (s[r, c] != 0f)
                            {
                                hasNonZeroValue = true;
                                goto doubleBreak;
                            }
                        }
                    }
                    doubleBreak:

                    if (!hasNonZeroValue)
                    {
                        // Use max value as a sentinel to explicitly pass coefficients to light loop that cancel out reflection probe contribution
                        const float k = float.MaxValue;
                        s.AddAmbientLight(new Color(k, k, k));
                    }
                }

                m_SHCoefficients[requestsInstanceIDs[i]] = s;
                m_SHValidity[requestsInstanceIDs[i]] = v;
            }
        }

        private void ClearSHCoefficients()
        {
            foreach (var key in m_SHCoefficients.Keys)
            {
                m_SHCoefficients[key] = default;
                m_SHValidity[key] = kInvalidSH;
            }
        }
    }
#endif
}
