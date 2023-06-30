using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    // A fixed-size array that can contain up to maximum render target attachment amount of items
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedAttachmentArray<DataType> where DataType : unmanaged
    {
        public static FixedAttachmentArray<DataType> Empty = new FixedAttachmentArray<DataType>(0);

        // This is a fixed size struct that emulates itself as an array
        // similar to how Unity.Math emulates size arrays
        public const int MaxAttachments = 8;
        private DataType a0, a1, a2, a3, a4, a5, a6, a7;
        private int activeAttachments;

        public FixedAttachmentArray(int numAttachments)
        {
            if (numAttachments < 0 || numAttachments > MaxAttachments)
            {
                throw new ArgumentException($"FixedAttachmentArray - numAttachments must be in range of [0, {MaxAttachments}[");
            }
            a0 = a1 = a2 = a3 = a4 = a5 = a6 = a7 = new DataType();
            activeAttachments = numAttachments;
        }
        public FixedAttachmentArray(DataType[] attachments) : this(attachments.Length)
        {
            for (int i = 0; i < activeAttachments; ++i)
            {
                this[i] = attachments[i];
            }
        }
        public FixedAttachmentArray(NativeArray<DataType> attachments) : this(attachments.Length)
        {
            for (int i = 0; i < activeAttachments; ++i)
            {
                this[i] = attachments[i];
            }
        }

        public int size
        {
            get
            {
                return activeAttachments;
            }
        }

        public void Clear()
        {
            activeAttachments = 0;
        }

        // Returns the index where the item was added
        public int Add(in DataType data)
        {
            if ((uint)activeAttachments >= MaxAttachments)
                throw new IndexOutOfRangeException($"A FixedAttachmentArray can only contain {MaxAttachments} items.");

            int index = activeAttachments;
            unsafe
            {
                fixed (FixedAttachmentArray<DataType>* self = &this)
                {
                    DataType* array = (DataType*)self;
                    array[index] = data;
                }
            }
            activeAttachments++;
            return index;
        }

        public ref DataType this[int index]
        {
            get
            {
                if ((uint)index >= MaxAttachments)
                    throw new IndexOutOfRangeException($"FixedAttachmentArray - index must be in range of [0, {MaxAttachments}[");
                if ((uint)index >= activeAttachments)
                    throw new IndexOutOfRangeException($"FixedAttachmentArray - index must be in range of [0, {activeAttachments}[");
                unsafe
                {
                    fixed (FixedAttachmentArray<DataType>* self = &this)
                    {
                        DataType* array = (DataType*)self;
                        return ref array[index];
                    }
                }
            }
        }
    }
}
