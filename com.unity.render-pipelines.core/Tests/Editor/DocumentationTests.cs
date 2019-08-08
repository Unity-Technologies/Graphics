using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor.Tests")]

namespace UnityEditor.Rendering.Tests
{
    class DocumentationTests
    {
        [Test]
        public void Validation_GoogleOnline()
            => IsLinkValid("http://www.google.com");

        [Test]
        public void Validation_ErrorDown()
            => IsLinkValid("http://Error.com", shouldExist: false);
        
        //TODO: reactivate this test once documentation is provided in Core
        //[Test]
        //public void AllDocumentationLinksAccessible()
        //{
        //    foreach (var url in GetDocumentationURLsForAssembly(typeof(UnityEngine.Rendering.Documentation).Assembly))
        //        IsLinkValid(url);
        //}

        public static void IsLinkValid(string url, bool shouldExist = true)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var asyncOp = webRequest.SendWebRequest();
                while (!webRequest.isDone)
                    new WaitForEndOfFrame();
                Assert.IsTrue((webRequest.isNetworkError || webRequest.isHttpError) ^ shouldExist, $"{url}\n{webRequest.error}");
            }
        }

        public static IEnumerable<string> GetDocumentationURLsForAssembly(System.Reflection.Assembly assembly)
            => TypeCache
                .GetTypesWithAttribute<HelpURLAttribute>()
                .Where(x => x.Assembly == assembly)
                .Select(x => (x.GetCustomAttributes(typeof(HelpURLAttribute), false).First() as HelpURLAttribute).URL)
                .Distinct();
    }
}
