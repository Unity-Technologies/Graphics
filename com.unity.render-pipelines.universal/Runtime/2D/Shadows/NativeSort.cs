using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

//namespace UnityEngine.Experimental.Rendering.LWRP
namespace Unity.Collections
{
    internal static class NativeSortExtension
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct LongDoubleUnion
        {
            [FieldOffset(0)]
            public long longValue;
            [FieldOffset(0)]
            public double doubleValue;
        }


        public static int lzcnt(uint x)
        {
            if (x == 0)
                return 32;
            LongDoubleUnion u;
            u.doubleValue = 0.0;
            u.longValue = 0x4330000000000000L + x;
            u.doubleValue -= 4503599627370496.0;
            return 0x41E - (int)(u.longValue >> 52);
        }

        public static int log2_floor(int value)
        {
            return 31 - lzcnt((uint)value);
        }


        struct DefaultComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y) => x.CompareTo(y);
        }
        
        unsafe public static void Sort<T>(this NativeArray<T> array) where T : struct, IComparable<T>
        {
            array.Sort(new DefaultComparer<T>());
        }
        unsafe public static void Sort<T, U>(this NativeArray<T> array, U comp) where T : struct where U : IComparer<T>
        {
            
            IntroSort<T, U>(array.GetUnsafePtr(), 0, array.Length - 1, 2 * log2_floor(array.Length), comp);
        }

        unsafe public static void Sort<T>(this NativeSlice<T> slice) where T : struct, IComparable<T>
        {
            slice.Sort(new DefaultComparer<T>());
        }
        unsafe public static void Sort<T, U>(this NativeSlice<T> slice, U comp) where T : struct where U : IComparer<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (slice.Stride != UnsafeUtility.SizeOf<T>())
                throw new InvalidOperationException("Sort requires that stride matches the size of the source type");
#endif

            IntroSort<T, U>(slice.GetUnsafePtr(), 0, slice.Length - 1, 2 * log2_floor(slice.Length), comp);
        }

        const int k_IntrosortSizeThreshold = 16;
        unsafe static void IntroSort<T, U>(void* array, int lo, int hi, int depth, U comp) where T : struct where U : IComparer<T>
        {
            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= k_IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems<T, U>(array, lo, hi, comp);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems<T, U>(array, lo, hi - 1, comp);
                        SwapIfGreaterWithItems<T, U>(array, lo, hi, comp);
                        SwapIfGreaterWithItems<T, U>(array, hi - 1, hi, comp);
                        return;
                    }

                    InsertionSort<T, U>(array, lo, hi, comp);
                    return;
                }

                if (depth == 0)
                {
                    HeapSort<T, U>(array, lo, hi, comp);
                    return;
                }
                depth--;

                int p = Partition<T, U>(array, lo, hi, comp);
                IntroSort<T, U>(array, p + 1, hi, depth, comp);
                hi = p - 1;
            }
        }

        unsafe static void InsertionSort<T, U>(void* array, int lo, int hi, U comp) where T : struct where U : IComparer<T>
        {
            int i, j;
            T t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = UnsafeUtility.ReadArrayElement<T>(array, i + 1);
                while (j >= lo && comp.Compare(t, UnsafeUtility.ReadArrayElement<T>(array, j)) < 0)
                {
                    UnsafeUtility.WriteArrayElement<T>(array, j + 1, UnsafeUtility.ReadArrayElement<T>(array, j));
                    j--;
                }
                UnsafeUtility.WriteArrayElement<T>(array, j + 1, t);
            }
        }

        unsafe static int Partition<T, U>(void* array, int lo, int hi, U comp) where T : struct where U : IComparer<T>
        {
            int mid = lo + ((hi - lo) / 2);
            SwapIfGreaterWithItems<T, U>(array, lo, mid, comp);
            SwapIfGreaterWithItems<T, U>(array, lo, hi, comp);
            SwapIfGreaterWithItems<T, U>(array, mid, hi, comp);

            T pivot = UnsafeUtility.ReadArrayElement<T>(array, mid);
            Swap<T>(array, mid, hi - 1);
            int left = lo, right = hi - 1;

            while (left < right)
            {
                while (comp.Compare(pivot, UnsafeUtility.ReadArrayElement<T>(array, ++left)) > 0) ;
                while (comp.Compare(pivot, UnsafeUtility.ReadArrayElement<T>(array, --right)) < 0) ;

                if (left >= right)
                    break;

                Swap<T>(array, left, right);
            }

            Swap<T>(array, left, (hi - 1));
            return left;
        }

        unsafe static void HeapSort<T, U>(void* array, int lo, int hi, U comp) where T : struct where U : IComparer<T>
        {
            int n = hi - lo + 1;

            for (int i = n / 2; i >= 1; i--)
            {
                Heapify<T, U>(array, i, n, lo, comp);
            }

            for (int i = n; i > 1; i--)
            {
                Swap<T>(array, lo, lo + i - 1);
                Heapify<T, U>(array, 1, i - 1, lo, comp);
            }
        }

        unsafe static void Heapify<T, U>(void* array, int i, int n, int lo, U comp) where T : struct where U : IComparer<T>
        {
            T val = UnsafeUtility.ReadArrayElement<T>(array, lo + i - 1);
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && (comp.Compare(UnsafeUtility.ReadArrayElement<T>(array, lo + child - 1), UnsafeUtility.ReadArrayElement<T>(array, (lo + child))) < 0))
                {
                    child++;
                }
                if (comp.Compare(UnsafeUtility.ReadArrayElement<T>(array, (lo + child - 1)), val) < 0)
                    break;

                UnsafeUtility.WriteArrayElement<T>(array, lo + i - 1, UnsafeUtility.ReadArrayElement<T>(array, lo + child - 1));
                i = child;
            }
            UnsafeUtility.WriteArrayElement(array, lo + i - 1, val);
        }

        unsafe static void Swap<T>(void* array, int lhs, int rhs) where T : struct
        {
            T val = UnsafeUtility.ReadArrayElement<T>(array, lhs);
            UnsafeUtility.WriteArrayElement<T>(array, lhs, UnsafeUtility.ReadArrayElement<T>(array, rhs));
            UnsafeUtility.WriteArrayElement<T>(array, rhs, val);
        }

        unsafe static void SwapIfGreaterWithItems<T, U>(void* array, int lhs, int rhs, U comp) where T : struct where U : IComparer<T>
        {
            if (lhs != rhs)
            {
                if (comp.Compare(UnsafeUtility.ReadArrayElement<T>(array, lhs), UnsafeUtility.ReadArrayElement<T>(array, rhs)) > 0)
                {
                    Swap<T>(array, lhs, rhs);
                }
            }
        }
    }
}

