using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ProjectionVolume
    {
        [SerializeField]
        ShapeType m_ShapeType;

        // Box
        [SerializeField]
        Vector3 m_BoxSize;
        [SerializeField]
        Vector3 m_BoxOffset;
        [SerializeField]
        bool m_BoxInfiniteProjection;

        // Sphere
        [SerializeField]
        float m_SphereRadius;
        [SerializeField]
        Vector3 m_SphereOffset;
        [SerializeField]
        bool m_SphereInfiniteProjection;


        public ShapeType shapeType { get { return m_ShapeType; } }

        public Vector3 boxSize { get { return m_BoxSize; } }
        public Vector3 boxOffset { get { return m_BoxOffset; } }
        public bool boxInfiniteProjection { get { return m_BoxInfiniteProjection; } }

        public float sphereRadius { get { return m_SphereRadius; } }
        public Vector3 sphereOffset { get { return m_SphereOffset; } }
        public bool sphereInfiniteProjection { get { return m_SphereInfiniteProjection; } }
    }
}
