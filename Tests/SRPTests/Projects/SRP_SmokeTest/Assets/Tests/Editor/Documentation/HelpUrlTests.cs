using System;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Tests.Documentation
{
    public class HelpUrlTestsBase
    {
        RenderPipelineAsset m_GraphicsSettingsRPAsset = null;

        [SetUp]
        public void SetUp()
        {
            m_GraphicsSettingsRPAsset = GraphicsSettings.defaultRenderPipeline;
        }

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_GraphicsSettingsRPAsset;
        }

        public const string k_URPAssetPath = "Assets/PipelineAssets/UniversalRenderPipelineAsset.asset";
        public const string k_HDRPAssetPath = "Assets/PipelineAssets/HDRenderPipelineAsset.asset";

        public static RenderPipelineAsset LoadAsset(string renderPipelineAssetPath)
        {
            if (string.IsNullOrEmpty(renderPipelineAssetPath))
                return null;

            var asset = AssetDatabase.LoadMainAssetAtPath(renderPipelineAssetPath) as RenderPipelineAsset;
            Assert.IsNotNull(asset, renderPipelineAssetPath, $"Unable to load {renderPipelineAssetPath}");
            return asset;
        }
    }

    public class HelpUrlTests : HelpUrlTestsBase
    {
        // PipelineHelpURL tests

        [PipelineHelpURL("UniversalRenderPipelineAsset", "pageName", "pageHash")]
        [PipelineHelpURL("HDRenderPipelineAsset", "pageName", "pageHash")]
        class Type_WithPipelineHelpURL_HDRP_And_URP
        {
        }

        [PipelineHelpURL("Foo", pageName: "foo")]
        class Type_WithPipelineHelpURL_UnknownPipeline
        {
        }

        [PipelineHelpURL(null, null)]
        class Type_WithPipelineHelpURL_Null
        {
        }

        static TestCaseData[] s_PipelineHelpURLTestCases =
        {
            new TestCaseData(null, typeof(Type_WithPipelineHelpURL_HDRP_And_URP))
                .SetName("When BiRP is active, TryGetHelpURL(Type_WithPipelineHelpURL_HDRP_And_URP) returns invalid URL")
                .Returns(false),
            new TestCaseData(k_URPAssetPath, typeof(Type_WithPipelineHelpURL_HDRP_And_URP))
                .SetName("When URP is active, TryGetHelpURL(Type_WithPipelineHelpURL_HDRP_And_URP) returns valid URL")
                .Returns(true),
            new TestCaseData(k_HDRPAssetPath, typeof(Type_WithPipelineHelpURL_HDRP_And_URP))
                .SetName("When HDRP is active, TryGetHelpURL(Type_WithPipelineHelpURL_HDRP_And_URP) returns valid URL")
                .Returns(true),
            new TestCaseData(null, typeof(Type_WithPipelineHelpURL_UnknownPipeline))
                .SetName("When BiRP is active, TryGetHelpURL(Type_WithPipelineHelpURL_UnknownPipeline) returns invalid URL")
                .Returns(false),
            new TestCaseData(k_URPAssetPath, typeof(Type_WithPipelineHelpURL_UnknownPipeline))
                .SetName("When URP is active, TryGetHelpURL(Type_WithPipelineHelpURL_UnknownPipeline) returns invalid URL")
                .Returns(false),
            new TestCaseData(null, typeof(Type_WithPipelineHelpURL_Null))
                .SetName("When BiRP is active, TryGetHelpURL(Type_WithPipelineHelpURL_Null) returns invalid URL")
                .Returns(false),
            new TestCaseData(k_URPAssetPath, typeof(Type_WithPipelineHelpURL_Null))
                .SetName("When URP is active, TryGetHelpURL(Type_WithPipelineHelpURL_Null) returns invalid URL")
                .Returns(false),
        };

        [Test, TestCaseSource(nameof(s_PipelineHelpURLTestCases))]
        public bool PipelineHelpURLAttribute(string renderPipelineAsset, Type renderPipelineType)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAsset);

            DocumentationUtils.TryGetHelpURL(renderPipelineType, out string url);

            if (url != null)
            {
                Assert.True(url.Contains("docs.unity3d.com"));
                Assert.True(url.EndsWith("pageName.html#pageHash"));
            }

            return url != null;
        }

        // CurrentPipelineHelpURL tests

        [CurrentPipelineHelpURL("pageName", "pageHash")]
        class Type_WithCurrentPipelineHelpURL
        {
        }

        static TestCaseData[] s_CurrentPipelineHelpURLTestCases =
        {
            new TestCaseData(null, typeof(Type_WithCurrentPipelineHelpURL))
                .SetName("When BiRP is active, TryGetHelpURL(Type_WithCurrentPipelineHelpURL) returns invalid URL")
                .Returns(false),
            new TestCaseData(k_URPAssetPath, typeof(Type_WithCurrentPipelineHelpURL))
                .SetName("When URP is active, TryGetHelpURL(Type_WithCurrentPipelineHelpURL) returns valid URL")
                .Returns(true),
            new TestCaseData(k_HDRPAssetPath, typeof(Type_WithCurrentPipelineHelpURL))
                .SetName("When HDRP is active, TryGetHelpURL(Type_WithCurrentPipelineHelpURL) returns valid URL")
                .Returns(true),
        };

        [Test, TestCaseSource(nameof(s_CurrentPipelineHelpURLTestCases))]
        public bool CurrentPipelineHelpURLAttribute(string renderPipelineAsset, Type renderPipelineType)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAsset);

            DocumentationUtils.TryGetHelpURL(renderPipelineType, out string url);

            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                Assert.True(url.Contains("com.unity.render-pipelines.universal"));
            else if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset)
                Assert.True(url.Contains("com.unity.render-pipelines.high-definition"));

            if (url != null)
            {
                Assert.True(url.Contains("docs.unity3d.com"));
                Assert.True(url.EndsWith("pageName.html#pageHash"));
            }

            return url != null;
        }

        // CoreRPHelpURL tests

        [CoreRPHelpURL("pageName")]
        class Type_WithCoreRPHelpURL
        {
        }

        static TestCaseData[] s_CoreRPHelpURLTestCases =
        {
            new TestCaseData(null, typeof(Type_WithCoreRPHelpURL))
                .SetName("TryGetHelpURL(Type_WithCoreRPHelpURL) returns valid URL")
                .Returns(true),
        };

        [Test, TestCaseSource(nameof(s_CoreRPHelpURLTestCases))]
        public bool CoreRPHelpURLAttribute(string renderPipelineAsset, Type renderPipelineType)
        {
            DocumentationUtils.TryGetHelpURL(renderPipelineType, out string url);
            if (url != null)
            {
                Assert.True(url.Contains("docs.unity3d.com"));
                Assert.True(url.Contains("com.unity.render-pipelines.core"));
                Assert.True(url.EndsWith("pageName.html"));
            }

            return url != null;
        }
    }
}
