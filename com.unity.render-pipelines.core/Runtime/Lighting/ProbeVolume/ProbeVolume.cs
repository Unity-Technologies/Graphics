using System;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    internal struct ProbeVolumeArtistParameters
    {
        public Vector3  size;
        [HideInInspector]
        public float    maxSubdivisionMultiplier;
        [HideInInspector]
        public float    minSubdivisionMultiplier;

        public ProbeVolumeArtistParameters(Color debugColor, float maxSubdivision = 1, float minSubdivision = 0)
        {
            this.size = Vector3.one;
            this.maxSubdivisionMultiplier = maxSubdivision;
            this.minSubdivisionMultiplier = minSubdivision;
        }
    } // class ProbeVolumeArtistParameters

    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Volume (Experimental)")]
    public class ProbeVolume : MonoBehaviour
    {
        [SerializeField] internal ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return parameters.size;
        }

#if UNITY_EDITOR
        protected void Update()
        {
        }

        internal void OnLightingDataCleared()
        {
        }

        internal void OnLightingDataAssetCleared()
        {
        }

        internal void OnProbesBakeCompleted()
        {
        }

        internal void OnBakeCompleted()
        {
        }

        internal void ForceBakingDisabled()
        {
        }

        internal void ForceBakingEnabled()
        {
        }

#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
