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

        /// <summary>An invalid decal layer.</summary>
        public static readonly DecalLayer Invalid = new DecalLayer {m_Value = (1 << LayerCount) - 1};

        [SerializeField] int m_Value;

        /// <summary>
        ///     Instantiate a new decal layer.
        ///     The actual value stored is <code><paramref name="value" /> % <see cref="LayerCount" /></code>.
        /// </summary>
        /// <param name="value">The value to use for the decal layer.</param>
        public DecalLayer(int value)
        {
            m_Value = value % LayerCount;
        }

        /// <summary>
        ///     Convert a decal layer to an int.
        /// </summary>
        /// <param name="v">The decal layer to convert.</param>
        /// <returns>The number of the layer.</returns>
        public static explicit operator int(in DecalLayer v)
        {
            return v.m_Value;
        }

        /// <summary>
        ///     Convert a decal layer to a uint.
        /// </summary>
        /// <param name="v">The decal layer to convert.</param>
        /// <returns>The number of the layer.</returns>
        public static explicit operator uint(in DecalLayer v)
        {
            return (uint) v.m_Value;
        }

        /// <summary>
        ///     Convert a int to a decal layer.
        ///     The value provided will be sanitized. <see cref="DecalLayer(System.Int32)" />.
        /// </summary>
        /// <param name="v">The int to convert.</param>
        /// <returns>The corresponding decal layer.</returns>
        public static explicit operator DecalLayer(in int v)
        {
            return new DecalLayer(v);
        }

        /// <summary>
        ///     Convert a decal layer to a decal layer mask.
        /// </summary>
        /// <param name="v">The decal layer to convert.</param>
        /// <returns>The corresponding decal layer mask.</returns>
        public static explicit operator DecalLayerMask(in DecalLayer layer)
        {
            return new DecalLayerMask(1 << layer.m_Value);
        }

        /// <summary>
        ///     Pretty format a decal layer.
        /// </summary>
        /// <returns>A string representing the decal layer for human.</returns>
        public override string ToString()
        {
            return $"DecalLayer({m_Value:x2})";
        }

        /// <summary>Compare to another <see cref="DecalLayer" />.</summary>
        /// <param name="other">The value to compare to</param>
        /// <returns><code>true</code> when this value equals <paramref name="other" />. <code>false</code> otherwise.</returns>
        public bool Equals(DecalLayer other)
        {
            return m_Value == other.m_Value;
        }

        /// <summary>Compare to an object.</summary>
        /// <param name="other">The value to compare to</param>
        /// <returns><code>true</code> when this value equals <paramref name="other" />. <code>false</code> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is DecalLayer other && Equals(other);
        }

        /// <summary>
        ///     Compute the hashcode of this value.
        /// </summary>
        /// <returns>The hashcode of this value.</returns>
        public override int GetHashCode()
        {
            return m_Value;
        }

        /// <summary>
        ///     Compares two decal layers. <see cref="Equals(UnityEngine.Rendering.HighDefinition.DecalLayer)" />.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><code>true</code> when both value are equals, <code>false</code> otherwise.</returns>
        public static bool operator ==(DecalLayer left, DecalLayer right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Compares two decal layers. <see cref="Equals(UnityEngine.Rendering.HighDefinition.DecalLayer)" />.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><code>true</code> when both value are not equals, <code>false</code> otherwise.</returns>
        public static bool operator !=(DecalLayer left, DecalLayer right)
        {
            return !left.Equals(right);
        }
    }
}
