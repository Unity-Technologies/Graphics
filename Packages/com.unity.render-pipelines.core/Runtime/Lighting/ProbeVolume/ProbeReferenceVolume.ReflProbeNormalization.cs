using System;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using Unity.Scripting.LifecycleManagement;
#endif

namespace UnityEngine.Rendering
{
#if UNITY_EDITOR

    /// <summary>
    /// A manager to enqueue extra probe rendering outside of probe volumes.
    /// </summary>
    public partial class AdditionalGIBakeRequestsManager
    {
        // The baking ID for the extra requests
        // TODO: Need to ensure this never conflicts with bake IDs from others interacting with the API.
        // In our project, this is ProbeVolumes.
        internal static readonly int k_BakingID = 912345678;

        [AutoStaticsCleanup]
        static AdditionalGIBakeRequestsManager s_Instance = new AdditionalGIBakeRequestsManager();
        /// <summary>
        /// Get the manager that governs the additional light probe rendering requests.
        /// </summary>
        public static AdditionalGIBakeRequestsManager instance { get { return s_Instance; } }

        const float k_InvalidSH = 1f;
        const float k_InvalidValidity = 1f;
        const float k_ValidSHThresh = 0.33f;

        [AutoStaticsCleanup]
        static Dictionary<EntityId, SphericalHarmonicsL2> s_SHCoefficients = new Dictionary<EntityId, SphericalHarmonicsL2>();
        [AutoStaticsCleanup]
        static Dictionary<EntityId, float> s_SHValidity = new Dictionary<EntityId, float>();
        [AutoStaticsCleanup]
        static Dictionary<EntityId, Vector3> s_RequestPositions = new Dictionary<EntityId, Vector3>();

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name ="capturePosition"> The position at which a probe is baked.</param>
        /// <param name ="probeEntityId"> The entityId of the probe doing the request.</param>
        public void EnqueueRequest(Vector3 capturePosition, EntityId probeEntityId)
        {
            s_SHCoefficients[probeEntityId] = new SphericalHarmonicsL2();
            s_SHValidity[probeEntityId] = k_InvalidSH;
            s_RequestPositions[probeEntityId] = capturePosition;
        }

        /// <summary>
        /// Dequeue a request for probe rendering.
        /// </summary>
        /// <param name ="probeInstanceID">The instance ID of the probe for which we want to dequeue a request. </param>
        public void DequeueRequest(EntityId probeInstanceID)
        {
            if (s_SHCoefficients.ContainsKey(probeInstanceID))
            {
                s_SHCoefficients.Remove(probeInstanceID);
                s_SHValidity.Remove(probeInstanceID);
                s_RequestPositions.Remove(probeInstanceID);
            }
        }

        /// <summary>
        /// Retrieve the result of a capture request, it will return false if the request has not been fulfilled yet or the request ID is invalid.
        /// </summary>
        /// <param name ="probeInstanceID"> The instance ID of the probe doing the request.</param>
        /// <param name ="sh"> The output SH coefficients that have been computed.</param>
        /// <param name ="pos"> The position for which the computed SH coefficients are valid.</param>
        /// <returns>Whether the request for light probe rendering has been fulfilled and sh is valid.</returns>
        [Obsolete("Use RetrieveProbe instead. #from(6000.2)")]
        public bool RetrieveProbeSH(int probeInstanceID, out SphericalHarmonicsL2 sh, out Vector3 pos)
        {
            if (s_SHCoefficients.ContainsKey(probeInstanceID))
            {
                sh = s_SHCoefficients[probeInstanceID];
                pos = s_RequestPositions[probeInstanceID];
                return s_SHValidity[probeInstanceID] < k_ValidSHThresh;
            }

            sh = new SphericalHarmonicsL2();
            pos = Vector3.negativeInfinity;
            return false;
        }

        /// <summary>
        /// Retrieve the result of a capture request, it will return false if the request ID is invalid.
        /// </summary>
        /// <param name ="probeInstanceID"> The instance ID of the probe doing the request.</param>
        /// <param name ="pos"> The position for which the computed SH coefficients are valid.</param>
        /// <param name ="sh"> The output SH coefficients that have been computed.</param>
        /// <param name ="validity"> The output validity that has been computed.</param>
        /// <returns>True if the request ID is valid.</returns>
        public bool RetrieveProbe(EntityId probeInstanceID, out Vector3 pos, out SphericalHarmonicsL2 sh, out float validity)
        {
            if (s_SHCoefficients.ContainsKey(probeInstanceID))
            {
                sh = s_SHCoefficients[probeInstanceID];
                pos = s_RequestPositions[probeInstanceID];
                validity = s_SHValidity[probeInstanceID];

                return true;
            }

            sh = new SphericalHarmonicsL2();
            pos = Vector3.negativeInfinity;
            validity = float.NegativeInfinity;

            return false;
        }

        static internal bool GetPositionForRequest(EntityId probeInstanceID, out Vector3 pos)
        {
            if (s_SHCoefficients.ContainsKey(probeInstanceID))
            {
                pos = s_RequestPositions[probeInstanceID];
                return true;
            }

            pos = Vector3.negativeInfinity;
            return false;
        }

        /// <summary>
        /// Update the capture location for the probe request.
        /// </summary>
        /// <param name ="probeInstanceID"> The instance ID of the probe doing the request and that wants the capture position updated.</param>
        /// <param name ="newPosition"> The position at which a probe is baked.</param>
        public void UpdatePositionForRequest(EntityId probeInstanceID, Vector3 newPosition)
        {
            if (s_SHCoefficients.ContainsKey(probeInstanceID))
            {
                s_RequestPositions[probeInstanceID] = newPosition;
                s_SHCoefficients[probeInstanceID] = new SphericalHarmonicsL2();
                s_SHValidity[probeInstanceID] = k_InvalidSH;
            }
            else
            {
                EnqueueRequest(newPosition, probeInstanceID);
            }
        }

        static internal List<Vector3> GetProbeNormalizationRequests() => new List<Vector3>(s_RequestPositions.Values);

        static internal void OnAdditionalProbesBakeCompleted(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            SetSHCoefficients(sh, validity);

            ProbeReferenceVolume.instance.retrieveExtraDataAction?.Invoke(new ProbeReferenceVolume.ExtraDataActionInput());
        }

        static bool IsZero(in SphericalHarmonicsL2 s)
        {
            for (var r = 0; r < 3; ++r)
            {
                for (var c = 0; c < 9; ++c)
                {
                    if (s[r, c] != 0f)
                        return false;
                }
            }
            return true;
        }

        static internal void SetSHCoefficients(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            Debug.Assert(sh.Length == s_SHCoefficients.Count);
            Debug.Assert(sh.Length == validity.Length);

            var requestsInstanceIDs = new List<EntityId>(s_SHCoefficients.Keys);

            for (int i = 0; i < sh.Length; ++i)
            {
                SetSHCoefficients(requestsInstanceIDs[i], sh[i], validity[i]);
            }
        }

        static internal void SetSHCoefficients(EntityId instanceID, SphericalHarmonicsL2 sh, float validity)
        {
            if (validity < k_ValidSHThresh)
            {
                if (IsZero(in sh))
                {
                    // Use max value as a sentinel to explicitly pass coefficients to light loop that cancel out reflection probe contribution.
                    // Written directly to the L0 coefficients rather than via AddAmbientLight, so the sentinel stays finite regardless of
                    // the SupportedRenderingFeatures.divideBakedOutputByPI convention scaling.
                    const float k = float.MaxValue;
                    sh[0, 0] = k;
                    sh[1, 0] = k;
                    sh[2, 0] = k;
                }
            }

            s_SHCoefficients[instanceID] = sh;
            s_SHValidity[instanceID] = validity;
        }
    }
#endif
}
