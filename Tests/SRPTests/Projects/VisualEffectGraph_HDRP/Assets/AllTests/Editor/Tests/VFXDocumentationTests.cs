using System;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;

using UnityEditor;
using UnityEditor.VFX;

namespace AllTests.Editor.Tests
{
    [TestFixture]
    public class VFXDocumentationTests
    {
        [Test]
        public void CheckAllNodesDocumentationLinks()
        {
            var failureCount = 0;
            var failureLog = new StringBuilder();
            var nodesWithDoc = TypeCache.GetTypesWithAttribute(typeof(VFXHelpURLAttribute));
            foreach (var node in nodesWithDoc)
            {
                var helpUrlAttribute = (VFXHelpURLAttribute)Attribute.GetCustomAttributes(node, typeof(VFXHelpURLAttribute)).Single();
                if (!TryGetHeaders(helpUrlAttribute.URL, out var statusCode))
                {
                    failureCount++;
                    failureLog.AppendLine($"URL: {helpUrlAttribute.URL} Status: {statusCode}");
                }
                else
                {
                    Assert.AreEqual(HttpStatusCode.OK,  statusCode, $"URL: {helpUrlAttribute.URL} Status: {statusCode}");
                }
            }

            if (failureLog.Length > 0)
            {
                Assert.Fail($"Number of failures: {failureCount}\n{failureLog}");
            }
        }

        private bool TryGetHeaders(string url, out HttpStatusCode statusCode)
        {
            statusCode = HttpStatusCode.NotFound;
            var request = HttpWebRequest.Create(url);
            request.Method = "HEAD";
            try
            {
                using var response = request.GetResponse() as HttpWebResponse;
                if (response != null)
                {
                    statusCode = response.StatusCode;
                    response.Close();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
