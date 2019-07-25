using NUnit.Framework;
using DocumentationTestLibrary = UnityEditor.Rendering.Tests.DocumentationTests;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    class DocumentationTests
    {
        [Test]
        public void AllDocumentationLinksAccessible()
        {
            foreach (var url in DocumentationTestLibrary.GetDocumentationURLsForAssembly(typeof(UnityEngine.Rendering.HighDefinition.Documentation).Assembly))
                DocumentationTestLibrary.IsLinkValid(url);
        }
    }
}
