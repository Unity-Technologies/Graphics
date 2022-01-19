using NUnit.Framework;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Tests
{
    public class CollectionExtensionTests
    {
        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new List<int>(), 0)
                .Returns(new List<int>())
                .SetName("Empty"),
            new TestCaseData(new List<int>(), -1)
                .Returns(new List<int>())
                .SetName("EmptyNegative"),
            new TestCaseData(new List<int>() {1,2}, -1)
                .Returns(new List<int>() {1,2})
                .SetName("NonEmptyNegative"),
            new TestCaseData(new List<int>() {1,2}, 2)
                .Returns(new List<int>())
                .SetName("All"),
            new TestCaseData(new List<int>() {1,2}, 1)
                .Returns(new List<int>() {1})
                .SetName("JustLast"),
            new TestCaseData(new List<int>() {1,2}, 3)
                .Returns(new List<int>())
                .SetName("BiggerThanCollection"),
            new TestCaseData(new List<int>() {1,2,3,4,5,6,7}, 3)
                .Returns(new List<int>(){1,2,3,4})
                .SetName("SomeElements"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public List<int> Tests(List<int> ints, int elementsToRemove)
        {
            ints.RemoveBack(elementsToRemove);
            return ints;
        }
    }
}
