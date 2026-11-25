using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Tests.Documentation
{
    public class CameraAndLightHelpUrlTests : HelpUrlTestsBase
    {
        GameObject m_GameObject;

        [SetUp]
        public void CreateGameObject()
        {
            Help.ClearCache();
            m_GameObject = new GameObject(nameof(CameraAndLightHelpUrlTests));
        }

        [TearDown]
        public void DestroyGameObject()
        {
            Object.DestroyImmediate(m_GameObject);
        }

        static TestCaseData[] s_URPTestCaseDatas =
        {
            new TestCaseData(typeof(Camera), "urp/urp-cameras-landing")
                .SetName("{m}(Camera URL points to the URP documentation when URP is the active pipeline)"),
            new TestCaseData(typeof(Light), "urp/light-component")
                .SetName("{m}(Light URL points to the URP documentation when URP is the active pipeline)"),
        };

        [Test, TestCaseSource(nameof(s_URPTestCaseDatas))]
        public void CheckUrlsWithURP(Type componentType, string pageName)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(k_URPAssetPath);
            var component = m_GameObject.AddComponent(componentType);

            Assert.AreEqual(DocumentationInfo.GetManualLink(pageName), Help.GetHelpURLForObject(component));
        }

        static TestCaseData[] s_FallbackTestCaseDatas =
        {
            new TestCaseData(null, typeof(Camera), "class-Camera")
                .SetName("{m}(Camera URL falls back to the built-in page when BiRP is the active pipeline)"),
            new TestCaseData(null, typeof(Light), "class-Light")
                .SetName("{m}(Light URL falls back to the built-in page when BiRP is the active pipeline)"),
            new TestCaseData(k_HDRPAssetPath, typeof(Camera), "class-Camera")
                .SetName("{m}(Camera URL falls back to the built-in page when HDRP is the active pipeline)"),
            new TestCaseData(k_HDRPAssetPath, typeof(Light), "class-Light")
                .SetName("{m}(Light URL falls back to the built-in page when HDRP is the active pipeline)"),
        };

        [Test, TestCaseSource(nameof(s_FallbackTestCaseDatas))]
        public void CheckUrlsFallBackWithoutURP(string renderPipelineAsset, Type componentType, string topic)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAsset);
            var component = m_GameObject.AddComponent(componentType);

            var url = Help.GetHelpURLForObject(component);
            StringAssert.Contains(topic, url);
            StringAssert.DoesNotContain("/urp/", url);
        }
    }
}
