using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.UIGen
{
    public static class Utilities
    {
        public static bool FailIfNullOrEmpty(
            [AllowNull]this string value,
            [DisallowNull] string name,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (string.IsNullOrEmpty(value))
            {
                error = new ArgumentNullException(name);
                return false;
            }

            error = null;
            return true;
        }
    }
}
