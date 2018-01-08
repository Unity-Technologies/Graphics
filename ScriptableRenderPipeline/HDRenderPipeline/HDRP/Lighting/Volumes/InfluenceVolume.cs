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

        // Sphere
        [SerializeField]
        float m_SphereBaseRadius = 1;
        [SerializeField]
        Vector3 m_SphereBaseOffset;
        [SerializeField]
        float m_SphereInfluenceRadius = 1;
        [SerializeField]
        float m_SphereInfluenceNormalRadius = 1;

        public ShapeType shapeType { get { return m_ShapeType; } }

        public Vector3 boxBaseSize { get { return m_BoxBaseSize; } }
        public Vector3 boxBaseOffset { get { return m_BoxBaseOffset; } }
        public Vector3 boxInfluencePositiveFade { get { return m_BoxInfluencePositiveFade; } }
        public Vector3 boxInfluenceNegativeFade { get { return m_BoxInfluenceNegativeFade; } }
        public Vector3 boxInfluenceNormalPositiveFade { get { return m_BoxInfluenceNormalPositiveFade; } }
        public Vector3 boxInfluenceNormalNegativeFade { get { return m_BoxInfluenceNormalNegativeFade; } }

        public float sphereBaseRadius { get { return m_SphereBaseRadius; } }
        public Vector3 sphereBaseOffset { get { return m_SphereBaseOffset; } }
        public float sphereInfluenceRadius { get { return m_SphereInfluenceRadius; } }
        public float sphereInfluenceNormalRadius { get { return m_SphereInfluenceNormalRadius; } }
    }
}
