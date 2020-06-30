using System;

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

        /// <summary>An invalid layer mask.</summary>
        public static readonly DecalLayerMask Invalid = new DecalLayerMask {m_Value = (1 << Capacity) - 1};

        [SerializeField] int m_Value;

        /// <summary>
        ///     Instantiate a new decal layer mask.
        ///     Only relevant bits are stored. The number of relevant bits is <see cref="Capacity" />.
        /// </summary>
        /// <param name="value">The value to use for the decal layer mask.</param>
        public DecalLayerMask(int value)
        {
            m_Value = value & 0xFF;
        }

        /// <summary>
        ///     Convert a decal layer mask to an int.
        /// </summary>
        /// <param name="v">The decal layer mask to convert.</param>
        /// <returns>The corresponding int value of the layer mask.</returns>
        public static explicit operator int(in DecalLayerMask v)
        {
            return v.m_Value;
        }

        /// <summary>
        ///     Convert a decal layer mask to an uint.
        /// </summary>
        /// <param name="v">The decal layer mask to convert.</param>
        /// <returns>The corresponding int value of the layer mask.</returns>
        public static explicit operator uint(in DecalLayerMask v)
        {
            return (uint)v.m_Value;
        }

        /// <summary>
        ///     Convert a int to a decal layer mask.
        ///     The value provided will be sanitized. <see cref="DecalLayerMask(System.Int32)" />.
        /// </summary>
        /// <param name="v">The int to convert.</param>
        /// <returns>The corresponding decal layer mask.</returns>
        public static explicit operator DecalLayerMask(in int v)
        {
            return new DecalLayerMask(v);
        }

        /// <summary>
        ///     Performs a bitwise & operator.
        ///     The result contains only layers contained in both arguments.
        /// </summary>
        /// <param name="l">The left value of the operator.</param>
        /// <param name="r">The right value of the operator.</param>
        /// <returns>The resulting decal layer mask.</returns>
        public static DecalLayerMask operator &(in DecalLayerMask l, in DecalLayerMask r)
        {
            return new DecalLayerMask((int) l & (int) r);
        }

        /// <summary>
        ///     Performs a bitwise | operator.
        ///     The result contains all layers from both arguments.
        /// </summary>
        /// <param name="l">The left value of the operator.</param>
        /// <param name="r">The right value of the operator.</param>
        /// <returns>The resulting decal layer mask.</returns>
        public static DecalLayerMask operator |(in DecalLayerMask l, in DecalLayerMask r)
        {
            return new DecalLayerMask((int) l | (int) r);
        }


        /// <summary>
        ///     Performs a bitwise ^ operator.
        ///     The result contains all layers contained in one and only one of the arguments.
        /// </summary>
        /// <param name="l">The left value of the operator.</param>
        /// <param name="r">The right value of the operator.</param>
        /// <returns>The resulting decal layer mask.</returns>
        public static DecalLayerMask operator ^(in DecalLayerMask l, in DecalLayerMask r)
        {
            return new DecalLayerMask((int) l ^ (int) r);
        }

        /// <summary>
        ///     Pretty format a decal layer mask.
        /// </summary>
        /// <returns>A string representing the decal layer mask for human.</returns>
        public override string ToString()
        {
            return $"DecalLayerMask({m_Value:x2})";
        }

        /// <summary>Compare to another <see cref="DecalLayerMask" />.</summary>
        /// <param name="other">The value to compare to</param>
        /// <returns><code>true</code> when this value equals <paramref name="other" />. <code>false</code> otherwise.</returns>
        public bool Equals(DecalLayerMask other)
        {
            return m_Value == other.m_Value;
        }

        /// <summary>Compare to an object.</summary>
        /// <param name="other">The value to compare to</param>
        /// <returns><code>true</code> when this value equals <paramref name="other" />. <code>false</code> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is DecalLayerMask other && Equals(other);
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
        ///     Compares two decal layers. <see cref="Equals(UnityEngine.Rendering.HighDefinition.DecalLayerMask)" />.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><code>true</code> when both value are equals, <code>false</code> otherwise.</returns>
        public static bool operator ==(DecalLayerMask left, DecalLayerMask right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Compares two decal layers. <see cref="Equals(UnityEngine.Rendering.HighDefinition.DecalLayerMask)" />.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns><code>true</code> when both value are not equals, <code>false</code> otherwise.</returns>
        public static bool operator !=(DecalLayerMask left, DecalLayerMask right)
        {
            return !left.Equals(right);
        }
    }
}
