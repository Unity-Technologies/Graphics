using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class InfluenceVolume
    {
        [SerializeField]
        ShapeType m_ShapeType;

        // Box
        [SerializeField]
        Vector3 m_BoxBaseSize = Vector3.one;
        [SerializeField]
        Vector3 m_BoxBaseOffset;
        [SerializeField]
        Vector3 m_BoxInfluencePositiveFade;
        [SerializeField]
        Vector3 m_BoxInfluenceNegativeFade;
        [SerializeField]
        Vector3 m_BoxInfluenceNormalPositiveFade;
        [SerializeField]
        Vector3 m_BoxInfluenceNormalNegativeFade;
        [SerializeField]
        Vector3 m_BoxPositiveFaceFade = Vector3.one;
        [SerializeField]
        Vector3 m_BoxNegativeFaceFade = Vector3.one;

        // Sphere
        [SerializeField]
        float m_SphereBaseRadius = 1;
        [SerializeField]
        Vector3 m_SphereBaseOffset;
        [SerializeField]
        float m_SphereInfluenceFade;
        [SerializeField]
        float m_SphereInfluenceNormalFade;

        public ShapeType shapeType { get { return m_ShapeType; } }

        public Vector3 boxBaseSize { get { return m_BoxBaseSize; } set { m_BoxBaseSize = value; } }
        public Vector3 boxBaseOffset { get { return m_BoxBaseOffset; } set { m_BoxBaseOffset = value; } }
        public Vector3 boxInfluencePositiveFade { get { return m_BoxInfluencePositiveFade; } set { m_BoxInfluencePositiveFade = value; } }
        public Vector3 boxInfluenceNegativeFade { get { return m_BoxInfluenceNegativeFade; } set { m_BoxInfluenceNegativeFade = value; } }
        public Vector3 boxInfluenceNormalPositiveFade { get { return m_BoxInfluenceNormalPositiveFade; } set { m_BoxInfluenceNormalPositiveFade = value; } }
        public Vector3 boxInfluenceNormalNegativeFade { get { return m_BoxInfluenceNormalNegativeFade; } set { m_BoxInfluenceNormalNegativeFade = value; } }
        public Vector3 boxPositiveFaceFade { get { return m_BoxPositiveFaceFade; } set { m_BoxPositiveFaceFade = value; } }
        public Vector3 boxNegativeFaceFade { get { return m_BoxNegativeFaceFade; } set { m_BoxNegativeFaceFade = value; } }

        public Vector3 boxInfluenceOffset { get { return (boxInfluenceNegativeFade - boxInfluencePositiveFade) * 0.5f; } }
        public Vector3 boxInfluenceSizeOffset { get { return -(boxInfluencePositiveFade + boxInfluenceNegativeFade); } }
        public Vector3 boxInfluenceNormalOffset { get { return (boxInfluenceNormalNegativeFade - boxInfluenceNormalPositiveFade) * 0.5f; } }
        public Vector3 boxInfluenceNormalSizeOffset { get { return -(boxInfluenceNormalPositiveFade + boxInfluenceNormalNegativeFade); } }



        public float sphereBaseRadius { get { return m_SphereBaseRadius; } set { m_SphereBaseRadius = value; } }
        public Vector3 sphereBaseOffset { get { return m_SphereBaseOffset; } set { m_SphereBaseOffset = value; } }
        public float sphereInfluenceFade { get { return m_SphereInfluenceFade; } set { m_SphereInfluenceFade = value; } }
        public float sphereInfluenceNormalFade { get { return m_SphereInfluenceNormalFade; } set { m_SphereInfluenceNormalFade = value; } }

        public float sphereInfluenceRadiusOffset { get { return -sphereInfluenceFade; } }
        public float sphereInfluenceNormalRadiusOffset { get { return -sphereInfluenceNormalFade; } }

        public BoundingSphere GetBoundingSphereAt(Transform transform)
        {
            switch (shapeType)
            {
                default:
                case ShapeType.Sphere:
                    return new BoundingSphere(transform.TransformPoint(sphereBaseOffset), sphereBaseRadius);
                case ShapeType.Box:
                {
                    var position = transform.TransformPoint(boxBaseOffset);
                    var radius = Mathf.Max(boxBaseSize.x, Mathf.Max(boxBaseSize.y, boxBaseSize.z));
                    return new BoundingSphere(position, radius);
                }
            }
        }

        public Bounds GetBoundsAt(Transform transform)
        {
            switch (shapeType)
            {
                default:
                case ShapeType.Sphere:
                    return new Bounds(transform.position, Vector3.one * sphereBaseRadius);
                case ShapeType.Box:
                {
                    var position = transform.TransformPoint(boxBaseOffset);
                    // TODO: Return a proper AABB based on influence box volume
                    return new Bounds(position, boxBaseSize);
                }
            }
        }

        public Vector3 GetWorldPosition(Transform transform)
        {
            switch (shapeType)
            {
                default:
                case ShapeType.Sphere:
                    return transform.TransformPoint(sphereBaseOffset);
                case ShapeType.Box:
                    return transform.TransformPoint(boxBaseOffset);
            }
        }
    }
}
