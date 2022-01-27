using System;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public struct QualifiedCategory : IEquatable<QualifiedCategory>
        {
            CategoryId primary;
            CategoryId secondary;

            public bool Equals(QualifiedCategory other)
            {
                return primary.Equals(other.primary) && secondary.Equals(other.secondary);
            }

            public override bool Equals(object obj)
            {
                return obj is QualifiedCategory other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(primary, secondary);
            }
        }
    }
}
