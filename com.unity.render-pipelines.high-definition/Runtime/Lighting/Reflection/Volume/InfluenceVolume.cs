using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// An influence volume.
    /// </summary>
    [Serializable]
    public partial class InfluenceVolume
    {
        // Serialized data
        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        InfluenceShape m_Shape = InfluenceShape.Box;

        // Box
        [SerializeField, FormerlySerializedAs("m_BoxBaseSize")]
        Vector3 m_BoxSize = Vector3.one * 10;
        [SerializeField, FormerlySerializedAs("m_BoxInfluencePositiveFade")]
        Vector3 m_BoxBlendDistancePositive;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNegativeFade")]
        Vector3 m_BoxBlendDistanceNegative;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalPositiveFade")]
        Vector3 m_BoxBlendNormalDistancePositive;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalNegativeFade")]
        Vector3 m_BoxBlendNormalDistanceNegative;
        [SerializeField, FormerlySerializedAs("m_BoxPositiveFaceFade")]
        Vector3 m_BoxSideFadePositive = Vector3.one;
        [SerializeField, FormerlySerializedAs("m_BoxNegativeFaceFade")]
        Vector3 m_BoxSideFadeNegative = Vector3.one;

        // Sphere
        [SerializeField, FormerlySerializedAs("m_SphereBaseRadius")]
        float m_SphereRadius = 3f;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceFade")]
        float m_SphereBlendDistance;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceNormalFade")]
        float m_SphereBlendNormalDistance;

        // Public API
        /// <summary>Shape of this InfluenceVolume.</summary>
        public InfluenceShape shape { get => m_Shape; set => m_Shape = value; }
        /// <summary>Get the extents of the influence.</summary>
        public Vector3 extents => GetExtents(shape);

        /// <summary>Size of the InfluenceVolume in Box Mode.</summary>
        public Vector3 boxSize { get => m_BoxSize; set => m_BoxSize = value; }

        /// <summary>Offset of sub volume defining fading.</summary>
        public Vector3 boxBlendOffset => (boxBlendDistanceNegative - boxBlendDistancePositive) * 0.5f;
        /// <summary>Size of sub volume defining fading.</summary>
        public Vector3 boxBlendSize => -(boxBlendDistancePositive + boxBlendDistanceNegative);
        /// <summary>
        /// Position of fade sub volume maxOffset point relative to InfluenceVolume max corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis.
        /// </summary>
        public Vector3 boxBlendDistancePositive { get => m_BoxBlendDistancePositive; set => m_BoxBlendDistancePositive = value; }
        /// <summary>
        /// Position of fade sub volume minOffset point relative to InfluenceVolume min corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis.
        /// </summary>
        public Vector3 boxBlendDistanceNegative { get => m_BoxBlendDistanceNegative; set => m_BoxBlendDistanceNegative = value; }

        /// <summary>Offset of sub volume defining fading relative to normal orientation.</summary>
        public Vector3 boxBlendNormalOffset => (boxBlendNormalDistanceNegative - boxBlendNormalDistancePositive) * 0.5f;
        /// <summary>Size of sub volume defining fading relative to normal orientation.</summary>
        public Vector3 boxBlendNormalSize => -(boxBlendNormalDistancePositive + boxBlendNormalDistanceNegative);
        /// <summary>
        /// Position of normal fade sub volume maxOffset point relative to InfluenceVolume max corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis (on origin for this axis).
        /// </summary>
        public Vector3 boxBlendNormalDistancePositive { get => m_BoxBlendNormalDistancePositive; set => m_BoxBlendNormalDistancePositive = value; }
        /// <summary>
        /// Position of normal fade sub volume minOffset point relative to InfluenceVolume min corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis (on origin for this axis).
        /// </summary>
        public Vector3 boxBlendNormalDistanceNegative { get => m_BoxBlendNormalDistanceNegative; set => m_BoxBlendNormalDistanceNegative = value; }

        /// <summary>Define fading percent of +X, +Y and +Z locally oriented face. (values from 0 to 1)</summary>
        public Vector3 boxSideFadePositive { get => m_BoxSideFadePositive; set => m_BoxSideFadePositive = value; }
        /// <summary>Define fading percent of -X, -Y and -Z locally oriented face. (values from 0 to 1)</summary>
        public Vector3 boxSideFadeNegative { get => m_BoxSideFadeNegative; set => m_BoxSideFadeNegative = value; }


        /// <summary>Radius of the InfluenceVolume in Sphere Mode.</summary>
        public float sphereRadius { get => m_SphereRadius; set => m_SphereRadius = value; }
        /// <summary>
        /// Offset of the fade sub volume from InfluenceVolume hull.
        /// Value between 0 (on InfluenceVolume hull) and sphereRadius (fade sub volume reduced to a point).
        /// </summary>
        public float sphereBlendDistance { get => m_SphereBlendDistance; set => m_SphereBlendDistance = value; }
        /// <summary>
        /// Offset of the normal fade sub volume from InfluenceVolume hull.
        /// Value between 0 (on InfluenceVolume hull) and sphereRadius (fade sub volume reduced to a point).
        /// </summary>
        public float sphereBlendNormalDistance { get => m_SphereBlendNormalDistance; set => m_SphereBlendNormalDistance = value; }

        /// <summary>Compute a hash of the influence properties.</summary>
        /// <returns></returns>
        public Hash128 ComputeHash()
        {
            var h = new Hash128();
            var h2 = new Hash128();
            HashUtilities.ComputeHash128(ref m_Shape, ref h);
            HashUtilities.ComputeHash128(ref m_ObsoleteOffset, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxBlendDistanceNegative, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxBlendDistancePositive, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxBlendNormalDistanceNegative, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxBlendNormalDistancePositive, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxSideFadeNegative, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxSideFadePositive, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_BoxSize, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_SphereBlendDistance, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_SphereBlendNormalDistance, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_SphereRadius, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            return h;
        }

        internal BoundingSphere GetBoundingSphereAt(Vector3 position)
        {
            switch (shape)
            {
                default:
                case InfluenceShape.Sphere:
                    return new BoundingSphere(position, sphereRadius);
                case InfluenceShape.Box:
                {
                    var radius = Mathf.Max(boxSize.x, Mathf.Max(boxSize.y, boxSize.z));
                    return new BoundingSphere(position, radius);
                }
            }
        }

        internal Bounds GetBoundsAt(Vector3 position)
        {
            switch (shape)
            {
                default:
                case InfluenceShape.Sphere:
                    return new Bounds(position, Vector3.one * sphereRadius);
                case InfluenceShape.Box:
                {
                    return new Bounds(position, boxSize);
                }
            }
        }

        internal Matrix4x4 GetInfluenceToWorld(Transform transform)
            => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        internal EnvShapeType envShape
        {
            get
            {
                switch (shape)
                {
                    default:
                    case InfluenceShape.Box:
                        return EnvShapeType.Box;
                    case InfluenceShape.Sphere:
                        return EnvShapeType.Sphere;
                }
            }
        }

        internal void CopyTo(InfluenceVolume data)
        {
            //keep the m_Probe as it is used to reset the probe

            data.m_Shape = m_Shape;
            data.m_ObsoleteOffset = m_ObsoleteOffset;
            data.m_BoxSize = m_BoxSize;
            data.m_BoxBlendDistancePositive = m_BoxBlendDistancePositive;
            data.m_BoxBlendDistanceNegative = m_BoxBlendDistanceNegative;
            data.m_BoxBlendNormalDistancePositive = m_BoxBlendNormalDistancePositive;
            data.m_BoxBlendNormalDistanceNegative = m_BoxBlendNormalDistanceNegative;
            data.m_BoxSideFadePositive = m_BoxSideFadePositive;
            data.m_BoxSideFadeNegative = m_BoxSideFadeNegative;
            data.m_SphereRadius = m_SphereRadius;
            data.m_SphereBlendDistance = m_SphereBlendDistance;
            data.m_SphereBlendNormalDistance = m_SphereBlendNormalDistance;

#if UNITY_EDITOR
            data.m_EditorAdvancedModeBlendDistancePositive = m_EditorAdvancedModeBlendDistancePositive;
            data.m_EditorAdvancedModeBlendDistanceNegative = m_EditorAdvancedModeBlendDistanceNegative;
            data.m_EditorSimplifiedModeBlendDistance = m_EditorSimplifiedModeBlendDistance;
            data.m_EditorAdvancedModeBlendNormalDistancePositive = m_EditorAdvancedModeBlendNormalDistancePositive;
            data.m_EditorAdvancedModeBlendNormalDistanceNegative = m_EditorAdvancedModeBlendNormalDistanceNegative;
            data.m_EditorSimplifiedModeBlendNormalDistance = m_EditorSimplifiedModeBlendNormalDistance;
            data.m_EditorAdvancedModeEnabled = m_EditorAdvancedModeEnabled;
            data.m_EditorAdvancedModeFaceFadePositive = m_EditorAdvancedModeFaceFadePositive;
            data.m_EditorAdvancedModeFaceFadeNegative = m_EditorAdvancedModeFaceFadeNegative;
#endif
        }

        Vector3 GetExtents(InfluenceShape shape)
        {
            switch (shape)
            {
                default:
                case InfluenceShape.Box:
                    return Vector3.Max(Vector3.one * 0.0001f, boxSize * 0.5f);
                case InfluenceShape.Sphere:
                    return Mathf.Max(0.0001f, sphereRadius) * Vector3.one;
            }
        }

        /// <summary>
        /// Compute the minimal FOV required to see the full influence volume from <paramref name="viewerPositionWS"/>
        ///     while looking at <paramref name="lookAtPositionWS"/>.
        /// </summary>
        /// <param name="viewerPositionWS">The viewer position in world space.</param>
        /// <param name="lookAtPositionWS">The look at position in world space.</param>
        /// <param name="influenceToWorld">The influence to world matrix.</param>
        /// <returns></returns>
        public float ComputeFOVAt(Vector3 viewerPositionWS, Vector3 lookAtPositionWS, Matrix4x4 influenceToWorld)
        {
            void GrowFOVToInclude(ref float fieldOfView, Vector3 positionWS)
            {
                var halfFOV = Vector3.Angle(lookAtPositionWS - viewerPositionWS, positionWS - viewerPositionWS);
                fieldOfView = Mathf.Max(halfFOV * 2, fieldOfView);
            }

            float fov = 0;


            switch (envShape)
            {
                case EnvShapeType.Box:
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(+boxSize.x, -boxSize.y, -boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(+boxSize.x, -boxSize.y, +boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(+boxSize.x, +boxSize.y, -boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(+boxSize.x, +boxSize.y, +boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(-boxSize.x, -boxSize.y, -boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(-boxSize.x, -boxSize.y, +boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(-boxSize.x, +boxSize.y, -boxSize.z)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(-boxSize.x, +boxSize.y, +boxSize.z)));
                    break;
                case EnvShapeType.Sphere:
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(+sphereRadius * 2, 0, 0)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(-sphereRadius * 2, 0, 0)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(0, +sphereRadius * 2, 0)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(0, -sphereRadius * 2, 0)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(0, 0, +sphereRadius * 2)));
                    GrowFOVToInclude(ref fov, influenceToWorld.MultiplyPoint(new Vector3(0, 0, -sphereRadius * 2)));
                    break;
                default:
                    fov = 90;
                    break;
            }

            return fov;
        }
    }
}
