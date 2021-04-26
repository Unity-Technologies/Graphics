using System;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Volume")]
    public class ProbeVolume : MonoBehaviour
    {
        public Vector3      size = Vector3.one;
        [HideInInspector]
        public float        maxSubdivisionMultiplier = 1;
        [HideInInspector]
        public float        minSubdivisionMultiplier = 0;

        public LayerMask    objectLayerMask = -1;

        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
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
