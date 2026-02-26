using System;
using System.Text;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    internal static class URP2D_Crc32
    {
        // 8 tables of 256 uints each
        private static readonly uint[][] Tables;

        static URP2D_Crc32()
        {
            Tables = new uint[8][];
            for (int i = 0; i < 8; i++) Tables[i] = new uint[256];

            uint polynomial = 0xEDB88320;
            // Fill Table 0
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                    entry = (entry & 1) == 1 ? (entry >> 1) ^ polynomial : (entry >> 1);
                Tables[0][i] = entry;
            }

            // Fill Tables 1 through 7
            for (int t = 1; t < 8; t++)
            {
                for (int i = 0; i < 256; i++)
                {
                    uint prev = Tables[t - 1][i];
                    Tables[t][i] = (prev >> 8) ^ Tables[0][prev & 0xFF];
                }
            }
        }

        public static uint Compute(ReadOnlySpan<byte> data)
        {
            uint crc = 0xFFFFFFFF;
            int i = 0;
            int length = data.Length;

            // Pull tables into local variables to help the JIT optimize register usage
            uint[] t0 = Tables[0];
            uint[] t1 = Tables[1];
            uint[] t2 = Tables[2];
            uint[] t3 = Tables[3];
            uint[] t4 = Tables[4];
            uint[] t5 = Tables[5];
            uint[] t6 = Tables[6];
            uint[] t7 = Tables[7];

            // Process 8 bytes at a time
            while (length - i >= 8)
            {
                // We use bitwise logic to extract the 4 bytes of the current CRC
                // and combine them with the next 4 bytes of data.
                uint step1 = data[i] ^ (crc & 0xFF);
                uint step2 = data[i + 1] ^ ((crc >> 8) & 0xFF);
                uint step3 = data[i + 2] ^ ((crc >> 16) & 0xFF);
                uint step4 = data[i + 3] ^ (crc >> 24);

                crc = t7[step1] ^ t6[step2] ^ t5[step3] ^ t4[step4] ^
                      t3[data[i + 4]] ^ t2[data[i + 5]] ^ t1[data[i + 6]] ^ t0[data[i + 7]];
                i += 8;
            }

            // Remaining bytes
            while (i < length)
            {
                crc = (crc >> 8) ^ t0[(crc ^ data[i++]) & 0xFF];
            }

            return ~crc;
        }

        public static uint Compute(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            // Use the most efficient way to get bytes without huge allocations
            return Compute(Encoding.UTF8.GetBytes(input));
        }

    }
}
