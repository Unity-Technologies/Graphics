using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A proxy volume.
    ///
    /// This volume approximate the scene geometry with simple mathematical shapes.
    /// </summary>
    [Serializable]
    public partial class ProxyVolume
    {
        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        ProxyShape m_Shape = ProxyShape.Box;
        [SerializeField]
        Vector3 m_BoxSize = Vector3.one;
        [SerializeField]
        float m_SphereRadius = 1;

        /// <summary>The shape of the proxy</summary>
        public ProxyShape shape { get => m_Shape; private set => m_Shape = value; }
        /// <summary>The size of the proxy if it as a shape Box</summary>
        public Vector3 boxSize { get => m_BoxSize; set => m_BoxSize = value; }
        /// <summary>The radius of the proxy if it as a shape Sphere</summary>
        public float sphereRadius { get => m_SphereRadius; set => m_SphereRadius = value; }

        internal Vector3 extents => GetExtents(shape);

        internal Hash128 ComputeHash()
        {
            var h = new Hash128();
            var h2 = new Hash128();

            HashUtilities.ComputeHash128(ref m_Shape, ref h);
            HashUtilities.ComputeHash128(ref m_BoxSize, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            HashUtilities.ComputeHash128(ref m_SphereRadius, ref h2);

            return h;
        }

        Vector3 GetExtents(ProxyShape shape)
        {
            switch (shape)
            {
                case ProxyShape.Box: return m_BoxSize * 0.5f;
                case ProxyShape.Sphere: return Vector3.one * m_SphereRadius;
                default: return Vector3.one;
            }
        }
    }
}
