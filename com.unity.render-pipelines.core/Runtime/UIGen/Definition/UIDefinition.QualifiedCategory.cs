using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public struct QualifiedCategory : IEquatable<QualifiedCategory>
        {
            [MustUseReturnValue]
            public static bool From(
                CategoryId primary,
                CategoryId? secondary,
                out QualifiedCategory category,
                [NotNullWhen(false)] out Exception error
            )
            {
                category = default;
                if (primary == CategoryId.Empty)
                {
                    error = new ArgumentException($"Primary category must not be empty.").WithStackTrace();
                    return false;
                }

                category = new QualifiedCategory(primary, secondary);
                error = default;
                return true;
            }

            public readonly CategoryId primary;
            public readonly CategoryId? secondary;

            QualifiedCategory(CategoryId primary, CategoryId? secondary)
            {
                this.primary = primary;
                this.secondary = secondary;
            }

            public bool Equals(QualifiedCategory other)
            {
                return primary.Equals(other.primary) && Nullable.Equals(secondary, other.secondary);
            }

            public override bool Equals(object obj)
            {
                return obj is QualifiedCategory other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(primary, secondary);
            }

            public override string ToString()
            {
                return secondary.HasValue && !string.IsNullOrEmpty((string) secondary.Value)
                    ? $"{primary}.{secondary.Value}"
                    : (string) primary;
            }
        }
    }
}
