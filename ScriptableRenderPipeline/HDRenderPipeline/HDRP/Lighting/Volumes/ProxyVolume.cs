using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ProxyVolume
    {
        [SerializeField]
        ShapeType m_ShapeType;

        // Box
        [SerializeField]
        Vector3 m_BoxSize = Vector3.one;
        [SerializeField]
        Vector3 m_BoxOffset;
        [SerializeField]
        bool m_BoxInfiniteProjection;

        // Sphere
        [SerializeField]
        float m_SphereRadius = 1;
        [SerializeField]
        Vector3 m_SphereOffset;
        [SerializeField]
        bool m_SphereInfiniteProjection;


        public ShapeType shapeType { get { return m_ShapeType; } }

        public Vector3 boxSize { get { return m_BoxSize; } set { m_BoxSize = value; } }
        public Vector3 boxOffset { get { return m_BoxOffset; } set { m_BoxOffset = value; } }
        public bool boxInfiniteProjection { get { return m_BoxInfiniteProjection; } }

        public float sphereRadius { get { return m_SphereRadius; } set { m_SphereRadius = value; } }
        public Vector3 sphereOffset { get { return m_SphereOffset; } set { m_SphereOffset = value; } }
        public bool sphereInfiniteProjection { get { return m_SphereInfiniteProjection; } }

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
