using System;

namespace UnityEditor.ShaderGraph
{
    static class GuidEncoder
    {
        public static string Encode(Guid guid)
        {
            return guid.ToString("N");
        }
    }
}
