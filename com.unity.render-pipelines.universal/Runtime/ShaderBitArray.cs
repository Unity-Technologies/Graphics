using System;

namespace UnityEngine.Rendering.Universal
{
    // A dynamic array of bits backed by a managed array of floats,
    // since that's what Unity Shader constant API offers.
    //
    // Example:
    // ShaderBitArray bits;
    // bits.Resize(8);
    // bits[0] = true;
    // cmd.SetGlobalFloatArray("_BitArray", bits.data);
    // bits.Clear();
    internal struct ShaderBitArray
    {
        const int k_BitsPerElement = 32;
        const int k_ElementShift = 5;
        const int k_ElementMask = (1 << k_ElementShift) - 1;

        private float[] m_Data;

        public int elemLength => m_Data == null ? 0 : m_Data.Length;
        public int bitCapacity => elemLength * k_BitsPerElement;
        public float[] data => m_Data;

        public void Resize(int bitCount)
        {
            if (bitCapacity > bitCount)
                return;

            int newElemCount = ((bitCount + (k_BitsPerElement - 1)) / k_BitsPerElement);
            if (newElemCount == m_Data?.Length)
                return;

            var newData = new float[newElemCount];
            if (m_Data != null)
            {
                for (int i = 0; i < m_Data.Length; i++)
                    newData[i] = m_Data[i];
            }
            m_Data = newData;
        }

        public void Clear()
        {
            for (int i = 0; i < m_Data.Length; i++)
                m_Data[i] = 0;
        }

        private void GetElementIndexAndBitOffset(int index, out int elemIndex, out int bitOffset)
        {
            elemIndex = index >> k_ElementShift;
            bitOffset = index & k_ElementMask;
        }

        public bool this[int index]
        {
            get
            {
                GetElementIndexAndBitOffset(index, out var elemIndex, out var bitOffset);

                unsafe
                {
                    fixed (float* floatData = m_Data)
                    {
                        uint* uintElem = (uint*)&floatData[elemIndex];
                        bool val = ((*uintElem) & (1u << bitOffset)) != 0u;
                        return val;
                    }
                }
            }
            set
            {
                GetElementIndexAndBitOffset(index, out var elemIndex, out var bitOffset);
                unsafe
                {
                    fixed (float* floatData = m_Data)
                    {
                        uint* uintElem = (uint*)&floatData[elemIndex];
                        if (value == true)
                            *uintElem = (*uintElem) | (1u << bitOffset);
                        else
                            *uintElem = (*uintElem) & ~(1u << bitOffset);
                    }
                }
            }
        }

        public override string ToString()
        {
            unsafe
            {
                Debug.Assert(bitCapacity < 4096, "Bit string too long! It was truncated!");
                int len = Math.Min(bitCapacity, 4096);
                byte* buf = stackalloc byte[len];
                for (int i = 0; i < len; i++)
                {
                    buf[i] = (byte)(this[i] ? '1' : '0');
                }

                return new string((sbyte*)buf, 0, len, System.Text.Encoding.UTF8);
            }
        }
    }
}
