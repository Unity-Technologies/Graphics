using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler
{
    /// <summary>
    /// A fixed-size array that can contain up to maximum render target attachment amount of items.
    /// </summary>
    /// <typeparam name="DataType">The type of data to store in the array.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedAttachmentArray<DataType> where DataType : unmanaged
    {
        /// <summary>
        /// Returns an empty array.
        /// </summary>
        public static FixedAttachmentArray<DataType> Empty = new FixedAttachmentArray<DataType>(0);

        /// <summary>
        /// The maximum number of elements that can be stored in the array.
        /// </summary>
        public const int MaxAttachments = 8;

        /// This is a fixed size struct that emulates itself as an array
        /// similar to how Unity.Math emulates fixed size arrays
        private DataType a0, a1, a2, a3, a4, a5, a6, a7;
        private int activeAttachments;

        /// <summary>
        /// Created an new array with the specified number of attachments.
        /// </summary>
        /// <param name="numAttachments">Number of attachments to consider valid.</param>
        /// <exception cref="ArgumentException">Thrown if the amount of elements is less than 0 or more than MaxAttachments</exception>
        public FixedAttachmentArray(int numAttachments)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (numAttachments < 0 || numAttachments > MaxAttachments)
            {
                throw new ArgumentException($"FixedAttachmentArray - numAttachments must be in range of [0, {MaxAttachments}[");
            }
#endif
            a0 = a1 = a2 = a3 = a4 = a5 = a6 = a7 = new DataType();
            activeAttachments = numAttachments;
        }

        /// <summary>
        /// Intialize the FixedAttachmentArray by copying data from the passed in c# array.
        /// </summary>
        /// <param name="attachments">The C# array from which to copy the elements.</param>
        public FixedAttachmentArray(DataType[] attachments) : this(attachments.Length)
        {
            for (int i = 0; i < activeAttachments; ++i)
            {
                this[i] = attachments[i];
            }
        }

        /// <summary>
        /// Intialize the FixedAttachmentArray by copying data from the passed in native array.
        /// </summary>
        /// <param name="attachments">The native array from which to copy the elements.</param>
        public FixedAttachmentArray(NativeArray<DataType> attachments) : this(attachments.Length)
        {
            for (int i = 0; i < activeAttachments; ++i)
            {
                this[i] = attachments[i];
            }
        }

        /// <summary>
        /// Number of attachments in the array alway less or equal than MaxAttachments
        /// </summary>
        public int size
        {
            get
            {
                return activeAttachments;
            }
        }

        /// <summary>
        /// Clear the array.
        /// </summary>
        public void Clear()
        {
            activeAttachments = 0;
        }

        /// <summary>
        /// Add an element tot the array.
        /// </summary>
        /// <param name="data">Element to add</param>
        /// <returns>Returns the index where the item was added.</returns>
        /// <exception cref="IndexOutOfRangeException">If the maximum amount of elements (MaxAttachments) is reached.</exception>
        public int Add(in DataType data)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if ((uint)activeAttachments >= MaxAttachments)
                throw new IndexOutOfRangeException($"A FixedAttachmentArray can only contain {MaxAttachments} items.");
#endif
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

        /// <summary>
        /// Get the element at the specified index in the array.
        /// </summary>
        /// <param name="index">Index of the element.</param>
        /// <value>The value of the element.</value>
        /// <exception cref="IndexOutOfRangeException">If the index is outside the valid range.</exception>
        public ref DataType this[int index]
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if ((uint)index >= MaxAttachments)
                    throw new IndexOutOfRangeException($"FixedAttachmentArray - index must be in range of [0, {MaxAttachments}[");
                if ((uint)index >= activeAttachments)
                    throw new IndexOutOfRangeException($"FixedAttachmentArray - index must be in range of [0, {activeAttachments}[");
#endif
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
