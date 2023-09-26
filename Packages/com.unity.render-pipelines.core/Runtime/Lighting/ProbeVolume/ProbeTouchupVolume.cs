using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A marker to adjust probes in an area of the scene.
    /// </summary>
    [CoreRPHelpURL("probevolumes-settings#probe-adjustment-volume", "com.unity.render-pipelines.high-definition")]
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Probe Adjustment Volume")]
    public class ProbeTouchupVolume : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>The type of shape that an adjustment volume can take. </summary>
        public enum Shape
        {
            /// <summary>A Box shape.</summary>
            Box,
            /// <summary>A Sphere shape.</summary>
            Sphere,
        };

        /// <summary>The shape of the adjustment volume</summary>
        [Tooltip("Select the shape used for this Probe Adjustment Volume.")]
        public Shape shape = Shape.Box;

        /// <summary>
        /// The size for box shape.
        /// </summary>
        [Min(0.0f), Tooltip("Modify the size of this Probe Adjustment Volume. This is unaffected by the GameObject's Transform's Scale property.")]
        public Vector3 size = new Vector3(1, 1, 1);

        /// <summary>
        /// The size for sphere shape.
        /// </summary>
        [Min(0.0f), Tooltip("Modify the radius of this Probe Adjustment Volume. This is unaffected by the GameObject's Transform's Scale property.")]
        public float radius = 1.0f;


        /// <summary>The mode that adjustment volume will operate in. It determines what probes falling within the volume will do. </summary>
        public enum Mode
        {
            /// <summary>Invalidate the probes within the adjustment volume.</summary>
            InvalidateProbes,
            /// <summary>Override the dilation validity threshold for the probes within the adjustment volume.</summary>
            OverrideValidityThreshold,
            /// <summary>Apply an explicit virtual offset to the probes within the adjustment volume.</summary>
            ApplyVirtualOffset,
            /// <summary>Override the virtual offset settings for the probes within the adjustment volume.</summary>
            OverrideVirtualOffsetSettings,
            /// <summary>Scale probe intensity.</summary>
            IntensityScale = 99, // make sure this appears last
        };

        /// <summary>Choose what to do with probes falling inside this volume</summary>
        public Mode mode = Mode.InvalidateProbes;

        /// <summary>
        /// A scale to apply to probes falling within the invalidation volume. It is really important to use this with caution as it can lead to inconsistent lighting.
        /// </summary>
        [Range(0.0001f, 2.0f), Tooltip("A multiplier applied to the intensity of probes covered by this Probe Adjustment Volume.")]
        public float intensityScale = 1.0f;

        /// <summary>
        /// The overridden dilation threshold.
        /// </summary>
        [Range(0.0f, 0.95f)]
        public float overriddenDilationThreshold = 0.75f;

        /// <summary>The rotation angles for the virtual offset direction.</summary>
        public Vector3 virtualOffsetRotation = Vector3.zero;

        /// <summary>Determines how far probes are pushed along the specified virtual offset direction.</summary>
        [Min(0.0f)]
        public float virtualOffsetDistance = 1.0f;

        /// <summary>Determines how far Unity pushes a probe out of geometry after a ray hit.</summary>
        [Range(0f, 1f), Tooltip("Determines how far Unity pushes a probe out of geometry after a ray hit.")]
        public float geometryBias = 0.01f;

        /// <summary>Distance from the probe position used to determine the origin of the sampling ray.</summary>
        [Range(-0.05f, 0f), Tooltip("Distance from the probe position used to determine the origin of the sampling ray.")]
        public float rayOriginBias = -0.001f;

#if UNITY_EDITOR
        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

        internal void GetOBBandAABB(out ProbeReferenceVolume.Volume volume, out Bounds bounds)
        {
            if (shape == Shape.Box)
            {
                volume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(transform.position, transform.rotation, GetExtents()), 0, 0);
                bounds = volume.CalculateAABB();
            }
            else
            {
                volume = default;
                bounds = new Bounds(transform.position, radius * Vector3.up);
            }
        }

        internal bool IntersectsVolume(in ProbeReferenceVolume.Volume touchupOBB, in Bounds touchupBounds, Bounds volumeBounds)
        {
            if (shape == Shape.Box)
                return ProbeVolumePositioning.OBBAABBIntersect(touchupOBB, volumeBounds, touchupBounds);
            else
                return volumeBounds.SqrDistance(touchupBounds.center) < radius * radius;
        }

        internal bool ContainsPoint(in ProbeReferenceVolume.Volume touchupOBB, in Vector3 touchupCenter, in Vector3 position)
        {
            if (shape == Shape.Box)
                return ProbeVolumePositioning.OBBContains(touchupOBB, position);
            else
                return (touchupCenter - position).sqrMagnitude < radius * radius;
        }

        internal Vector3 GetVirtualOffset()
        {
            if (mode != Mode.ApplyVirtualOffset)
                return Vector3.zero;
            return (transform.rotation * Quaternion.Euler(virtualOffsetRotation) * Vector3.forward) * virtualOffsetDistance;
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, ProbeVolume.s_gizmosLocationPath + "ProbeTouchupVolume.png", true);
        }
#endif


        // Migration related stuff

        enum Version
        {
            Initial,
            Mode,

            Count
        }

        [SerializeField]
        Version version = Version.Count;

        /// <summary>Whether to invalidate all probes falling within this volume.</summary>
        [Obsolete("Use mode")]
        public bool invalidateProbes = false;
        /// <summary>Whether to use a custom threshold for dilation for probes falling withing this volume.</summary>
        [Obsolete("Use mode")]
        public bool overrideDilationThreshold = false;

        void Awake()
        {
            if (version == Version.Count)
                return;

            if (version == Version.Initial)
            {
#pragma warning disable 618 // Type or member is obsolete
                if (invalidateProbes)
                    mode = Mode.InvalidateProbes;
                else if (overrideDilationThreshold)
                    mode = Mode.OverrideValidityThreshold;
#pragma warning restore 618

                version++;
            }
        }

        // This piece of code is needed because some objects could have been created before existence of Version enum
        /// <summary>OnBeforeSerialize needed to handle migration before the versioning system was in place.</summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (version == Version.Count) // serializing a newly created object
                version = Version.Count - 1; // mark as up to date
        }

        /// <summary>OnAfterDeserialize needed to handle migration before the versioning system was in place.</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (version == Version.Count) // deserializing and object without version
                version = Version.Initial; // reset to run the migration
        }
    }
}
