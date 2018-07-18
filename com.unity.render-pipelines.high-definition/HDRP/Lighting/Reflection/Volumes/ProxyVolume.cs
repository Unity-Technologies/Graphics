using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ProxyVolume
    {
        [SerializeField]
        ShapeType m_ShapeType = ShapeType.Box;

        // Box
        [SerializeField]
        Vector3 m_BoxSize = Vector3.one;
        [SerializeField]
        Vector3 m_BoxOffset;
        [SerializeField]
        bool m_BoxInfiniteProjection = false;

        // Sphere
        [SerializeField]
        float m_SphereRadius = 1;
        [SerializeField]
        Vector3 m_SphereOffset;
        [SerializeField]
        bool m_SphereInfiniteProjection = false;


        public ShapeType shapeType { get { return m_ShapeType; } }

        public Vector3 boxSize { get { return m_BoxSize; } set { m_BoxSize = value; } }
        public Vector3 boxOffset { get { return m_BoxOffset; } set { m_BoxOffset = value; } }
        public bool boxInfiniteProjection { get { return m_BoxInfiniteProjection; } }

        public float sphereRadius { get { return m_SphereRadius; } set { m_SphereRadius = value; } }
        public Vector3 sphereOffset { get { return m_SphereOffset; } set { m_SphereOffset = value; } }
        public bool sphereInfiniteProjection { get { return m_SphereInfiniteProjection; } }

        public Vector3 extents
        {
            get
            {
                switch (shapeType)
                {
                    case ShapeType.Box: return m_BoxSize * 0.5f;
                    case ShapeType.Sphere: return Vector3.one * m_SphereRadius;
                    default: return Vector3.one;
                }
            }
        }

        public bool infiniteProjection
        {
            get
            {
                return shapeType == ShapeType.Box && boxInfiniteProjection
                    || shapeType == ShapeType.Sphere && sphereInfiniteProjection;
            }
        }
    }
}
