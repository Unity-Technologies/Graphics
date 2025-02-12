using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace MultipleSRP.EditMode
{
    [TestFixture("Assets/URPDefaultResources/UniversalRenderPipelineAsset.asset", TestName = "URP")]
    [TestFixture("Assets/HDRPDefaultResources/HDRenderPipelineAsset.asset", TestName = "HDRP")]
    [TestFixture("", TestName = "BuiltIn")]
    public class HelpURLTests
    {
        static IEnumerable<TestCaseData> GetTestCases()
        {
            var types = TypeCache.GetTypesWithAttribute<HelpURLAttribute>();
            foreach (var type in types)
            {
                yield return new TestCaseData(type).SetName($"{type.FullName}").Returns(null);
            }
        }

        readonly string m_Path;
    
        HttpClient m_Client;
        RenderPipelineAsset m_SavedGraphicsRenderPipelineAsset;

        public HelpURLTests(string path)
        {
            m_Path = path;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            m_Client = new HttpClient();
            m_SavedGraphicsRenderPipelineAsset = GraphicsSettings.defaultRenderPipeline;

            GraphicsSettings.defaultRenderPipeline = LoadAsset();
        }

        RenderPipelineAsset LoadAsset()
        {
            if (string.IsNullOrEmpty(m_Path))
                return null;
        
            var asset = AssetDatabase.LoadMainAssetAtPath(m_Path);
            Assume.That(asset, Is.Not.Null);
            return asset as RenderPipelineAsset;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_Client.Dispose();
            GraphicsSettings.defaultRenderPipeline = m_SavedGraphicsRenderPipelineAsset;
        }
    
        [Ignore("Keep it explicit as it may not match with published documentation")]
        [Timeout(1500)]
        [UnityTest, TestCaseSource(nameof(GetTestCases))]
        public IEnumerator CheckHelpUrlsPointToTheExistingPage(Type type)
        {
            yield return null;
        
            DocumentationUtils.TryGetHelpURL(type, out var url);
            var task = Task.Run(() => CheckUrlFor404Async(m_Client, url));
            while (!task.IsCompleted)
                yield return null;
            Assert.That(task.Result, Is.True, () =>
            {
                var message = $"Type {type.FullName} have invalid help url link.\nInvalid Url: {url}\n";
                return message;
            });
        }

        static async Task<bool> CheckUrlFor404Async(HttpClient client, string url)
        {
            try
            {
                // Send a GET request to the URL
                HttpResponseMessage response = await client.GetAsync(url);

                // Check if the status code is 404
                return response.StatusCode != HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., network issues, invalid URL)
                Debug.LogError("Error: " + ex.Message);
                return false;
            }
        }
    }
}