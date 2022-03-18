using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class StringExtensionsTests
    {
        [Test]
        [TestCase("MyObject", "MyObject", 0)]
        [TestCase("MyObject1", "MyObject1", 0)]
        [TestCase("MyObject001", "MyObject001", 0)]
        [TestCase("MyObject (01)", "MyObject", 1, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("MyObject (42)", "MyObject", 42, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("MyObject (42)", "MyObject", 42, EditorSettings.NamingScheme.SpaceParenthesis, 1)]
        [TestCase("MyObject (042)", "MyObject", 42, EditorSettings.NamingScheme.SpaceParenthesis, 3)]
        [TestCase("MyObject (0042)", "MyObject", 42, EditorSettings.NamingScheme.SpaceParenthesis, 4)]
        [TestCase("MyObject (00042)", "MyObject", 42, EditorSettings.NamingScheme.SpaceParenthesis, 5)]
        [TestCase("MyObject_01", "MyObject", 1, EditorSettings.NamingScheme.Underscore, 2)]
        [TestCase("MyObject_1", "MyObject", 1, EditorSettings.NamingScheme.Underscore, 1)]
        [TestCase("MyObject__1", "MyObject_", 1, EditorSettings.NamingScheme.Underscore, 1)]
        [TestCase("MyObject.01", "MyObject", 1, EditorSettings.NamingScheme.Dot, 2)]
        [TestCase("MyObject.1", "MyObject", 1, EditorSettings.NamingScheme.Dot, 1)]
        [TestCase("MyObject..1", "MyObject.", 1, EditorSettings.NamingScheme.Dot, 1)]
        [TestCase("MyObject_42", "MyObject_42", 0, EditorSettings.NamingScheme.Dot, 2)]
        [TestCase("MyObject_42", "MyObject_42", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("MyObject.42", "MyObject.42", 0, EditorSettings.NamingScheme.Underscore, 2)]
        [TestCase("MyObject.42", "MyObject.42", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("MyObject (42)", "MyObject (42)", 0, EditorSettings.NamingScheme.Dot, 2)]
        [TestCase("MyObject (42)", "MyObject (42)", 0, EditorSettings.NamingScheme.Underscore, 2)]
        [TestCase("MyObject-(42)", "MyObject-(42)", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("MyObject.(42)", "MyObject.(42)", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("MyObject (42", "MyObject (42", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase("(42)", "(42)", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        [TestCase(" (42)", " (42)", 0, EditorSettings.NamingScheme.SpaceParenthesis, 2)]
        public void TestExtractBaseNameAndIndex(string originalName, string expectedBaseName, int expectedIndex, EditorSettings.NamingScheme namingScheme = EditorSettings.NamingScheme.SpaceParenthesis, int namingDigits = 0)
        {
            EditorSettings.gameObjectNamingScheme = namingScheme;
            EditorSettings.gameObjectNamingDigits = namingDigits;
            originalName.ExtractBaseNameAndIndex(out var basename, out var index);
            Assert.That(index, NUnit.Framework.Is.EqualTo(expectedIndex));
            Assert.That(basename, NUnit.Framework.Is.EqualTo(expectedBaseName));
        }

        EditorSettings.NamingScheme m_OriginalNamingScheme;
        int m_OriginalNumDigits;

        [SetUp]
        public void Setup()
        {
            m_OriginalNamingScheme = EditorSettings.gameObjectNamingScheme;
            m_OriginalNumDigits = EditorSettings.gameObjectNamingDigits;
        }

        [TearDown]
        public void TearDown()
        {
            EditorSettings.gameObjectNamingScheme = m_OriginalNamingScheme;
            EditorSettings.gameObjectNamingDigits = m_OriginalNumDigits;
        }
    }
}
