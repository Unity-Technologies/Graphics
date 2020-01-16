using System;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary> The layer mask used by materials for decals. </summary>
    [Serializable]
    public struct DecalLayer : IEquatable<DecalLayer>
    {
        /// <summary>Number of layers possible.</summary>
        public const int LayerCount = 8;
        /// <summary>Names of the layers.</summary>
        public static readonly string[] LayerNames = Enumerable.Range(0, LayerCount)
            .Select(i => $"Decal Layer {i}")
            .ToArray();

        /// <summary>Decal Layer 0.</summary>
        public static readonly DecalLayer Layer0 = new DecalLayer(0);
        /// <summary>Decal Layer 1.</summary>
        public static readonly DecalLayer Layer1 = new DecalLayer(1);
        /// <summary>Decal Layer 2.</summary>
        public static readonly DecalLayer Layer2 = new DecalLayer(2);
        /// <summary>Decal Layer 3.</summary>
        public static readonly DecalLayer Layer3 = new DecalLayer(3);
        /// <summary>Decal Layer 4.</summary>
        public static readonly DecalLayer Layer4 = new DecalLayer(4);
        /// <summary>Decal Layer 5.</summary>
        public static readonly DecalLayer Layer5 = new DecalLayer(5);
        /// <summary>Decal Layer 6.</summary>
        public static readonly DecalLayer Layer6 = new DecalLayer(6);
        /// <summary>Decal Layer 7.</summary>
        public static readonly DecalLayer Layer7 = new DecalLayer(7);

        [SerializeField]
        int m_Value;

        public DecalLayer(int value) => m_Value = value % LayerCount;

        public static explicit operator int(in DecalLayer v) => v.m_Value;
        public static explicit operator uint(in DecalLayer v) => (uint)v.m_Value;
        public static explicit operator DecalLayer(in int v) => new DecalLayer(v);
        public static explicit operator DecalLayerMask(in DecalLayer layer) => new DecalLayerMask(1 << layer.m_Value);
        public static DecalLayer operator&(in DecalLayer l, in DecalLayer r) => new DecalLayer((int)l & (int)r);
        public static DecalLayer operator|(in DecalLayer l, in DecalLayer r) => new DecalLayer((int)l | (int)r);
        public static DecalLayer operator^(in DecalLayer l, in DecalLayer r) => new DecalLayer((int)l ^ (int)r);

        public override string ToString() => $"DecalLayer({m_Value:x2})";

        public bool Equals(DecalLayer other)
        {
            return m_Value == other.m_Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DecalLayer other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Value;
        }

        public static bool operator ==(DecalLayer left, DecalLayer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DecalLayer left, DecalLayer right)
        {
            return !left.Equals(right);
        }
    }
}
