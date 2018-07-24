using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ProxyVolume
    {
        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        ShapeOrInfinite m_Shape = ShapeOrInfinite.Box;

        // Box
        [SerializeField]
        Vector3 m_BoxSize = Vector3.one;
        [SerializeField, Obsolete("Kept only for compatibility. Use m_Shape instead")]
        bool m_BoxInfiniteProjection = false;

        // Sphere
        [SerializeField]
        float m_SphereRadius = 1;
        [SerializeField, Obsolete("Kept only for compatibility. Use m_Shape instead")]
        bool m_SphereInfiniteProjection = false;

        public ShapeOrInfinite shape { get { return m_Shape; } private set { m_Shape = value; } }

        public Vector3 boxSize { get { return m_BoxSize; } set { m_BoxSize = value; } }

        public float sphereRadius { get { return m_SphereRadius; } set { m_SphereRadius = value; } }


        internal Vector3 extents
        {
            get
            {
                switch (shape)
                {
                    case ShapeOrInfinite.Box: return m_BoxSize * 0.5f;
                    case ShapeOrInfinite.Sphere: return Vector3.one * m_SphereRadius;
                    default: return Vector3.one;
                }
            }
        }

        internal void MigrateInfiniteProhjectionInShape()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (shape == ShapeOrInfinite.Sphere && m_SphereInfiniteProjection
                || shape == ShapeOrInfinite.Box && m_BoxInfiniteProjection)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                shape = ShapeOrInfinite.Infinite;
            }
        }
    }
}
