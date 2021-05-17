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

        public bool IsEquivalent(ProbeVolumeArtistParameters other)
        {
            return other.size == size &&
                other.maxSubdivisionMultiplier == maxSubdivisionMultiplier &&
                other.minSubdivisionMultiplier == minSubdivisionMultiplier;
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

        [SerializeField] internal bool mightNeedRebaking = false;

        [SerializeField] internal Matrix4x4 cachedTransform;
        [SerializeField] internal ProbeVolumeArtistParameters cachedParameters;

        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return parameters.size;
        }

#if UNITY_EDITOR
        internal void OnLightingDataAssetCleared()
        {
            mightNeedRebaking = true;
        }

        internal void OnBakeCompleted()
        {
            // We cache the data of last bake completed.
            cachedTransform = gameObject.transform.worldToLocalMatrix;
            cachedParameters = parameters;
            mightNeedRebaking = false;
        }

#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
