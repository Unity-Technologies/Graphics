using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    class SwapExtensionsTests
    {
        static TestCaseData[] s_ListTestsCaseDatas =
        {
            new TestCaseData(new int[] {1,2,3,4,5,6}, 0, 5)
                .SetName("Swap first for last")
                .Returns(new int[] {6,2,3,4,5,1}),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 5, 0)
                .SetName("Swap last for first")
                .Returns(new int[] {6,2,3,4,5,1}),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 1, 2)
                .SetName("Swap elements in the middle")
                .Returns(new int[] {1,3,2,4,5,6}),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 0, 2)
                .SetName("Swap first for something in the middle")
                .Returns(new int[] {3,2,1,4,5,6}),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 5, 2)
                .SetName("Swap last for something in the middle")
                .Returns(new int[] {1,2,6,4,5,3}),
        };

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public int[] TrySwap(int[] ints, int from, int to)
        {
            int[] copy = new int[ints.Length];
            Array.Copy(ints, 0, copy, 0, ints.Length);

            Assert.IsTrue(copy.TrySwap(from, to, out var _));
            return copy;
        }

        static TestCaseData[] s_ListTestsCaseDatasExceptions =
        {
            new TestCaseData(null, -1, 1).SetName("Null list").Returns(typeof(ArgumentNullException)),
            new TestCaseData(new int[] {1,2,3,4,5,6}, -1, 1).SetName("From negative").Returns(typeof(ArgumentOutOfRangeException)),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 6, 1).SetName("From larger than collection").Returns(typeof(ArgumentOutOfRangeException)),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 1, -1).SetName("To negative").Returns(typeof(ArgumentOutOfRangeException)),
            new TestCaseData(new int[] {1,2,3,4,5,6}, 1, 6).SetName("To larger than collection").Returns(typeof(ArgumentOutOfRangeException)),
        };

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatasExceptions))]
        public Type ExceptionsAreCorrect(int[] ints, int from, int to)
        {
            if (ints != null)
            {
                int[] copy = new int[ints.Length];
                Array.Copy(ints, 0, copy, 0, ints.Length);

                copy.TrySwap(from, to, out var error);
                return error.GetType();
            }
            else
            {
                ints.TrySwap(from, to, out var error);
                return error.GetType();
            }
        }
    }
}
