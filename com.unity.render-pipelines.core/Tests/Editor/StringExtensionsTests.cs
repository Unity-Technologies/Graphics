using NUnit.Framework;
using System.Linq;

namespace UnityEditor.Rendering.Tests
{
    class StringExtensionsTests
    {
        static TestCaseData[] s_Input =
        {
            new TestCaseData("A/B")
                .Returns("A_B")
                .SetName("Fogbugz - 1408027"),
            new TestCaseData("A" + new string(System.IO.Path.GetInvalidFileNameChars()) + "B")
                .Returns("A_B")
                .SetName("All Chars Replaced"),
            new TestCaseData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ")
                .Returns("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ")
                .SetName("Nothing replaced")
        };

        [Test, TestCaseSource(nameof(s_Input))]
        [Property("Fogbugz", "1408027")]
        public string ReplaceInvalidFileNameCharacters(string input)
        {
            return input.ReplaceInvalidFileNameCharacters();
        }

        [Test]
        public void CheckExtensionTests(
            [Values("Folder1/file.testextension", "Folder1/file.TestExtension", "Folder1/file.TESTEXTENSION", "file.testextension", "Folder1/Folder2/Folder3/file.testextension")] string input,
            [Values(".testextension", ".TESTEXTENSION", ".TestExtension", ".wrong")]string extension)
        {
            bool expected = input.ToLower().EndsWith(extension.ToLower());
            bool actual = input.HasExtension(extension);
            Assert.AreEqual(expected, actual);
        }
    }
}
