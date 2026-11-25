using System;

namespace UnityEditor.ShaderGraph
{
    static class GuidEncoder
    {
        public static string Encode(Guid guid)
        {
            return guid.ToString("N");
        }

        internal static string EncodeLegacy(Guid guid)
        {
            string encoded = Convert.ToBase64String(guid.ToByteArray());
            return MonoStringHash(encoded).ToString("X");
        }

        static int MonoStringHash(string value)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                for (int i = 0; i < value.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ value[i];
                    if (i + 1 < value.Length)
                        hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
                }
                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
