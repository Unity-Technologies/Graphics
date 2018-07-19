using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class InfluenceVolume
    {
        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        Shape m_Shape = Shape.Box;
        [SerializeField, FormerlySerializedAs("m_BoxBaseOffset")]
        Vector3 m_Offset;
        [SerializeField, Obsolete("Kept only for compatibility. Use m_Offset instead")]
        Vector3 m_SphereBaseOffset;

        // Box
        [SerializeField]
        Vector3 m_BoxBaseSize = Vector3.one;
        [SerializeField, FormerlySerializedAs("m_BoxInfluencePositiveFade")]
        Vector3 m_BoxPositiveFade;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNegativeFade")]
        Vector3 m_BoxNegativeFade;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalPositiveFade")]
        Vector3 m_BoxNormalPositiveFade;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalNegativeFade")]
        Vector3 m_BoxNormalNegativeFade;
        [SerializeField, FormerlySerializedAs("m_BoxPositiveFaceFade")]
        Vector3 m_BoxFacePositiveFade = Vector3.one;
        [SerializeField, FormerlySerializedAs("m_BoxNegativeFaceFade")]
        Vector3 m_BoxFaceNegativeFade = Vector3.one;

        //editor value that need to be saved for easy passing from simplified to advanced and vice et versa
        // /!\ must not be used outside editor code
        [SerializeField] private Vector3 editorAdvancedModeBlendDistancePositive;
        [SerializeField] private Vector3 editorAdvancedModeBlendDistanceNegative;
        [SerializeField] private float editorSimplifiedModeBlendDistance;
        [SerializeField] private Vector3 editorAdvancedModeBlendNormalDistancePositive;
        [SerializeField] private Vector3 editorAdvancedModeBlendNormalDistanceNegative;
        [SerializeField] private float editorSimplifiedModeBlendNormalDistance;
        [SerializeField] private bool editorAdvancedModeEnabled;

        // Sphere
        [SerializeField, FormerlySerializedAs("m_SphereBaseRadius")]
        float m_SphereRadius = 1;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceFade")]
        float m_SphereBlendDistance;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceNormalFade")]
        float m_SphereBlendNormalDistance;

        /// <summary>Shape of this InfluenceVolume.</summary>
        public Shape shape { get { return m_Shape; } set { m_Shape = value; } }

        /// <summary>Offset of this influence volume to the component handling him.</summary>
        public Vector3 offset { get { return m_Offset; } set { m_Offset = value; } }

        public Vector3 boxSize { get { return m_BoxBaseSize; } set { m_BoxBaseSize = value; } }

        public Vector3 boxBlendOffset { get { return (boxBlendDistanceNegative - boxBlendDistancePositive) * 0.5f; } }
        public Vector3 boxBlendSize { get { return -(boxBlendDistancePositive + boxBlendDistanceNegative); } }
        public Vector3 boxBlendDistancePositive { get { return m_BoxPositiveFade; } set { m_BoxPositiveFade = value; } }
        public Vector3 boxBlendDistanceNegative { get { return m_BoxNegativeFade; } set { m_BoxNegativeFade = value; } }

        public Vector3 boxBlendNormalOffset { get { return (boxBlendNormalDistanceNegative - boxBlendNormalDistancePositive) * 0.5f; } }
        public Vector3 boxBlendNormalSize { get { return -(boxBlendNormalDistancePositive + boxBlendNormalDistanceNegative); } }
        public Vector3 boxBlendNormalDistancePositive { get { return m_BoxNormalPositiveFade; } set { m_BoxNormalPositiveFade = value; } }
        public Vector3 boxBlendNormalDistanceNegative { get { return m_BoxNormalNegativeFade; } set { m_BoxNormalNegativeFade = value; } }

        public Vector3 boxSideFadePositive { get { return m_BoxFacePositiveFade; } set { m_BoxFacePositiveFade = value; } }
        public Vector3 boxSideFadeNegative { get { return m_BoxFaceNegativeFade; } set { m_BoxFaceNegativeFade = value; } }


        public float sphereRadius { get { return m_SphereRadius; } set { m_SphereRadius = value; } }
        public float sphereBlendDistance { get { return m_SphereBlendDistance; } set { m_SphereBlendDistance = value; } }
        public float sphereBlendNormalDistance { get { return m_SphereBlendNormalDistance; } set { m_SphereBlendNormalDistance = value; } }

        public BoundingSphere GetBoundingSphereAt(Transform transform)
        {
            switch (shape)
            {
                default:
                case Shape.Sphere:
                    return new BoundingSphere(transform.TransformPoint(offset), sphereRadius);
                case Shape.Box:
                {
                    var position = transform.TransformPoint(offset);
                    var radius = Mathf.Max(boxSize.x, Mathf.Max(boxSize.y, boxSize.z));
                    return new BoundingSphere(position, radius);
                }
            }
        }

        public Bounds GetBoundsAt(Transform transform)
        {
            switch (shape)
            {
                default:
                case Shape.Sphere:
                    return new Bounds(transform.position, Vector3.one * sphereRadius);
                case Shape.Box:
                {
                    var position = transform.TransformPoint(offset);
                    // TODO: Return a proper AABB based on influence box volume
                    return new Bounds(position, boxSize);
                }
            }
        }

        public Vector3 GetWorldPosition(Transform transform)
        {
            return transform.TransformPoint(offset);
        }

        internal void MigrateOffsetSphere()
        {
            if (shape == Shape.Sphere)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                m_Offset = m_SphereBaseOffset;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }
}
