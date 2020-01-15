using System;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary> The layer mask used by materials for decals. </summary>
    [Serializable]
    public struct DecalLayerMask : IEquatable<DecalLayerMask>
    {
        /// <summary>Number of layers possible.</summary>
        public const int Capacity = DecalLayer.LayerCount;
        /// <summary>Names of the layers.</summary>
        public static readonly string[] LayerNames = DecalLayer.LayerNames;

        /// <summary>No layers are accepted.</summary>
        public static readonly DecalLayerMask None = new DecalLayerMask(0);
        /// <summary>First layer is accepted.</summary>
        public static readonly DecalLayerMask Layer0 = new DecalLayerMask(1);
        /// <summary>All layers are accepted.</summary>
        public static readonly DecalLayerMask Full = new DecalLayerMask((1 << Capacity) - 1);

        [SerializeField]
        int m_Value;

        public DecalLayerMask(int value) => m_Value = value & 0xFF;

        public static explicit operator int(in DecalLayerMask v) => v.m_Value;
        public static explicit operator DecalLayerMask(in int v) => new DecalLayerMask(v);
        public static DecalLayerMask operator&(in DecalLayerMask l, in DecalLayerMask r) => new DecalLayerMask((int)l & (int)r);
        public static DecalLayerMask operator|(in DecalLayerMask l, in DecalLayerMask r) => new DecalLayerMask((int)l | (int)r);
        public static DecalLayerMask operator^(in DecalLayerMask l, in DecalLayerMask r) => new DecalLayerMask((int)l ^ (int)r);

        public override string ToString() => $"DecalLayerMask({m_Value:x2})";

        public bool Equals(DecalLayerMask other)
        {
            return m_Value == other.m_Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DecalLayerMask other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Value;
        }

        public static bool operator ==(DecalLayerMask left, DecalLayerMask right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DecalLayerMask left, DecalLayerMask right)
        {
            return !left.Equals(right);
        }
    }
}
