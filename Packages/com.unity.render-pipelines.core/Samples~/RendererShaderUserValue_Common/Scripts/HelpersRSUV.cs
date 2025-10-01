using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class HelpersRSUV
{

    // Set a bit to 0 or 1 at a specific bitIndex in a uint
    public static uint SetBit(uint value, int bitIndex, bool b)
    {
        if (bitIndex < 0 || bitIndex > 31)
            throw new ArgumentOutOfRangeException(nameof(bitIndex), "bitIndex must be between 0 and 31");

        if (b)
            value |= (1u << bitIndex);
        else
            value &= ~(1u << bitIndex);

        return value;
    }

    // Encode "value" into X bitCount starting at bitOffset
    public static uint EncodeData(uint flags, int value, int bitOffset, int length)
    {
        if (length <= 0 || length > 32)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 1 and 32.");
        if (bitOffset < 0 || bitOffset > 31)
            throw new ArgumentOutOfRangeException(nameof(bitOffset), "Offset must be between 0 and 31.");
        if (bitOffset + length > 32)
            throw new ArgumentOutOfRangeException("bitOffset + length exceeds 32 bits.");

        // Extract and set individual bits into result
        for (int i = 0; i < length; i++)
        {
            bool bit = ((value >> i) & 1) != 0;
            flags = SetBit(flags, bitOffset + i, bit);
        }

        return flags;
    }


    public static int DecodeData(uint value, int bitOffset, int length)
    {
        if (length <= 0 || length > 32)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 1 and 32.");
        if (bitOffset < 0 || bitOffset > 31)
            throw new ArgumentOutOfRangeException(nameof(bitOffset), "Offset must be between 0 and 31.");
        if (bitOffset + length > 32)
            throw new ArgumentOutOfRangeException("bitOffset + length exceeds 32 bits.");

        // Shift right to remove lower irrelevant bits, then mask the desired field
        uint mask = (length == 32) ? uint.MaxValue : (1u << length) - 1;
        uint extracted = (value >> bitOffset) & mask;

        return (int)extracted;
    }

    // Encode a Color32 type into the full raw uint
    public static uint EncodeData(Color32 color)
    {
        return ((uint)color.r << 24) |
               ((uint)color.g << 16) |
               ((uint)color.b << 8) |
               (uint)color.a;
    }

    // Encode a Color type into the full raw uint
    public static uint EncodeData(Color color)
    {
        byte r = (byte)(Mathf.Clamp01(color.r) * 255f);
        byte g = (byte)(Mathf.Clamp01(color.g) * 255f);
        byte b = (byte)(Mathf.Clamp01(color.b) * 255f);
        byte a = (byte)(Mathf.Clamp01(color.a) * 255f);

        return EncodeData(new Color32(r, g, b, a));
    }

   
}
