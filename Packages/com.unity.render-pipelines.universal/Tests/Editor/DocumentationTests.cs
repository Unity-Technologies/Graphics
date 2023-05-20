using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Documentation
{
    class DocumentationTests
    {
        private bool TestURL(string url)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

                // Timeout set to 10 seconds
                request.Timeout = 10000;

                // Only get header information
                request.Method = "HEAD";

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    int statusCode = (int)response.StatusCode;

                    // Successful requests
                    if (statusCode >= 100 && statusCode < 400)
                        return true;

                    // Server Errors
                    if (statusCode >= 500 && statusCode <= 510)
                        return false;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        [Test]
        public void TestURPHelpURLAttributes()
        {
            // Start with checking a bad URL to make sure TestURL() works.
            string badURL = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@16.0/manual/incorrect-url";
            bool badURLResults = TestURL(badURL);
            Assert.IsFalse(badURLResults, "TestURL() failed. A broken URL should have been detected.");

            // Find everything that uses URPHelpURLAttribute
            TypeCache.TypeCollection typesWithHelpUrls = TypeCache.GetTypesWithAttribute<URPHelpURLAttribute>();

            // Check if all the URLs are valid.
            bool allURLsAreValid = true;
            StringBuilder failures = new ();
            foreach (Type type in typesWithHelpUrls)
            {
                Debug.Log("Checking \"" + type.FullName + "\"");
                object[] attributes = type.GetCustomAttributes(typeof(URPHelpURLAttribute), false);
                for (int i = 0; i < attributes.Length; i++)
                {
                    URPHelpURLAttribute attribute = attributes[i] as URPHelpURLAttribute;
                    Assert.IsNotNull(attribute);

                    string url = attribute.URL;
                    bool urlIsValid = TestURL(url);

                    Debug.Log("\t" + url);
                    Debug.Log(urlIsValid ? "\tSuccess!\n" : "\tFailure!\n");

                    if (urlIsValid)
                        continue;

                    allURLsAreValid = false;
                    failures.AppendLine("\t" + type.FullName);
                    failures.AppendLine("\t" + url);
                    failures.AppendLine();
                }
            }

            if (!allURLsAreValid)
                Assert.Fail("The following URLs failed:\n" + failures);
        }
    }
}
