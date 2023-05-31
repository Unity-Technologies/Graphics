using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    [StructLayout(LayoutKind.Sequential)]
    struct Fixed2<T> where T : unmanaged
    {
        public T item1;
        public T item2;

        public Fixed2(T item1) : this(item1, item1)
        {
        }

        public Fixed2(T item1, T item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        public unsafe T this[int index]
        {
            get
            {
                CheckRange(index);
                fixed (T* items = &item1)
                {
                    return items[index];
                }
            }
            set
            {
                CheckRange(index);
                fixed (T* items = &item1)
                {
                    items[index] = value;
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckRange(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index is < 0 or > 1)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range of 2.");
            }
#endif
        }
    }
}
