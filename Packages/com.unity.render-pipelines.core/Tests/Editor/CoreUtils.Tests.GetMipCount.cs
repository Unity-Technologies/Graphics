using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    partial class CoreUtilsTests
    {
        static TestCaseData[] s_TestsCaseDatasInt =
        {
            new TestCaseData(8192).Returns(14),
            new TestCaseData(4096).Returns(13),
            new TestCaseData(2048).Returns(12),
            new TestCaseData(1024).Returns(11),
            new TestCaseData(512).Returns(10),
            new TestCaseData(256).Returns(9),
            new TestCaseData(128).Returns(8),
            new TestCaseData(64).Returns(7),
            new TestCaseData(32).Returns(6),
            new TestCaseData(16).Returns(5),
            new TestCaseData(8).Returns(4),
            new TestCaseData(4).Returns(3),
            new TestCaseData(2).Returns(2),
            new TestCaseData(1).Returns(1),
        };

        [Test, TestCaseSource(nameof(s_TestsCaseDatasInt))]
        public int GetMipCountInt(int size)
        {
            return CoreUtils.GetMipCount(size);
        }

        static TestCaseData[] s_TestsCaseDatasFloat =
        {
            new TestCaseData(8192.0f).Returns(14),
            new TestCaseData(4096.0f).Returns(13),
            new TestCaseData(2048.0f).Returns(12),
            new TestCaseData(1024.0f).Returns(11),
            new TestCaseData(512.0f).Returns(10),
            new TestCaseData(256.0f).Returns(9),
            new TestCaseData(128.0f).Returns(8),
            new TestCaseData(64.0f).Returns(7),
            new TestCaseData(32.0f).Returns(6),
            new TestCaseData(16.0f).Returns(5),
            new TestCaseData(8.0f).Returns(4),
            new TestCaseData(4.0f).Returns(3),
            new TestCaseData(2.0f).Returns(2),
            new TestCaseData(1.0f).Returns(1),
        };

        [Test, TestCaseSource(nameof(s_TestsCaseDatasFloat))]
        public int GetMipCountFloat(float size)
        {
            return CoreUtils.GetMipCount(size);
        }
    }
}
