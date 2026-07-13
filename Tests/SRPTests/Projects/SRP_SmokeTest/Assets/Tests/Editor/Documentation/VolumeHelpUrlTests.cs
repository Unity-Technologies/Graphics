using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Tests.Documentation
{
    public class VolumeHelpUrlTests : HelpUrlTestsBase
    {
        static TestCaseData[] s_VolumeTestsCaseDatas =
        {
            new TestCaseData("Assets/PipelineAssets/UniversalRenderPipelineAsset.asset" , typeof(UniversalRenderPipeline), "urp/Volumes")
                .SetName("Volumes URL's are correct when URP is the active pipeline"),
            new TestCaseData("Assets/PipelineAssets/HDRenderPipelineAsset.asset", typeof(HDRenderPipeline), "understand-volumes")
                .SetName("Volumes URL's are correct when HDRP is the active pipeline"),
        };

        [Test, TestCaseSource(nameof(s_VolumeTestsCaseDatas))]
        public void CheckVolumeUrls(string renderPipelineAsset, Type renderPipelineType, string pageName)
        {
            var go = new GameObject("CheckVolumeUrlAreCorrectForURP");
            var component = go.AddComponent<Volume>();

            var expectedLink = SetRPAndGetExpectedDocumentationLink(renderPipelineAsset, renderPipelineType, pageName);

            Assert.AreEqual(expectedLink, Help.GetHelpURLForObject(component));

            Object.DestroyImmediate(go);
        }

        static TestCaseData[] s_VolumeProfileTestsCaseDatas =
        {
            new TestCaseData(k_URPAssetPath, typeof(UniversalRenderPipeline), "urp/Volume-Profile")
                .SetName("Volumes URL's are correct when URP is the active pipeline"),
            new TestCaseData(k_HDRPAssetPath, typeof(HDRenderPipeline), "create-a-volume-profile")
                .SetName("Volumes URL's are correct when HDRP is the active pipeline"),
        };

        [Test, TestCaseSource(nameof(s_VolumeProfileTestsCaseDatas))]
        public void CheckVolumeProfileUrls(string renderPipelineAsset, Type renderPipelineType, string pageName)
        {
            var volumeProfileAsset = AssetDatabase.LoadMainAssetAtPath("Assets/DefaultResources/DefaultVolumeProfile.asset");
            Assume.That(volumeProfileAsset, Is.Not.Null);

            var expectedLink = SetRPAndGetExpectedDocumentationLink(renderPipelineAsset, renderPipelineType, pageName);

            Assert.AreEqual(expectedLink, Help.GetHelpURLForObject(volumeProfileAsset));
        }

        string SetRPAndGetExpectedDocumentationLink(string renderPipelineAsset, Type renderPipelineType, string pageName)
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset(renderPipelineAsset);

            // URP documentation is in the Unity Manual since 6000.0. Page paths are relative to the
            // Manual's "md" root (passed in via pageName, e.g. "urp/Volumes").
            if (renderPipelineType == typeof(UniversalRenderPipeline))
                return DocumentationInfo.GetManualLink(pageName);

            Assume.That(DocumentationUtils.TryGetPackageInfoForType(renderPipelineType, out var name, out var version), Is.True);
            return DocumentationInfo.GetPackageLink(name, version, pageName);
        }
    }
}
