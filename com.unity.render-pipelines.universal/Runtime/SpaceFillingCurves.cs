using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    static class SpaceFillingCurves
    {
        // "Insert" a 0 bit after each of the 16 low bits of x.
        // Ref: https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/
        static uint Part1By1(uint x)
        {
            x &= 0x0000ffff;                  // x = ---- ---- ---- ---- fedc ba98 7654 3210
            x = (x ^ (x <<  8)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
            x = (x ^ (x <<  4)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
            x = (x ^ (x <<  2)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
            x = (x ^ (x <<  1)) & 0x55555555; // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
            return x;
        }

        // Inverse of Part1By1 - "delete" all odd-indexed bits.
        // Ref: https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/
        static uint Compact1By1(uint x)
        {
            x &= 0x55555555;                  // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
            x = (x ^ (x >>  1)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
            x = (x ^ (x >>  2)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
            x = (x ^ (x >>  4)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
            x = (x ^ (x >>  8)) & 0x0000ffff; // x = ---- ---- ---- ---- fedc ba98 7654 3210
            return x;
        }

        public static uint EncodeMorton2D(uint2 coord)
        {
            return (Part1By1(coord.y) << 1) + Part1By1(coord.x);
        }

        public static uint2 DecodeMorton2D(uint code)
        {
            return math.uint2(Compact1By1(code >> 0), Compact1By1(code >> 1));
        }
    }
}
