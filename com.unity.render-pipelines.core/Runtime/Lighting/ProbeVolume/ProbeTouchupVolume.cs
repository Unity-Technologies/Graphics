using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Volume Touchup (Experimental)")]
    public class ProbeTouchupVolume : MonoBehaviour
    {
        internal float intensityScale = 1.0f;
        /// <summary>
        /// Whether to invalidate all probes falling within this volume.
        /// </summary>
        public bool invalidateProbes = false;
        /// <summary>
        /// Whether to use a custom threshold for dilation for probes falling withing this volume.
        /// </summary>
        public bool overrideDilationThreshold = false;

        /// <summary>
        /// The overridden dilation threshold.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float overriddenDilationThreshold = 0.75f;

        /// <summary>
        /// The size.
        /// </summary>
        public Vector3 size = new Vector3(1, 1, 1);

#if UNITY_EDITOR
        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

        internal Bounds GetBounds()
        {
            ProbeReferenceVolume.Volume volume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(transform.position, transform.rotation, GetExtents()), 0, 0);
            return volume.CalculateAABB();
        }
#endif
    }
} // UnityEngine.Rendering.HDPipeline
