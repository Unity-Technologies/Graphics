using System;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [Serializable]
    internal struct ProbeVolumeArtistParameters
    {
        public Vector3 size;

        public ProbeVolumeArtistParameters(Color debugColor)
        {
            this.size = Vector3.one;
        }
    } // class ProbeVolumeArtistParameters

    /// <summary>
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Volume")]
    public class ProbeVolume : MonoBehaviour
    {
        [SerializeField] internal ProbeVolumeArtistParameters parameters = new ProbeVolumeArtistParameters(Color.white);

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
