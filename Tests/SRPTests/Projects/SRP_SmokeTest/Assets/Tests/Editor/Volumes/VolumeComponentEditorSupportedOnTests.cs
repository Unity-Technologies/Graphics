using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Tests;

namespace UnityEditor.Rendering.Tests
{
    class VolumeComponentEditorSupportedOnTests : RenderPipelineTests
    {
        static TestCaseData[] s_TestCaseDataGetItem =
        {
            new TestCaseData(
                    null,
                    new[] { typeof(VolumeComponentSupportedEverywhere) },
                    new[] { typeof(VolumeComponentSupportedOnAnySRP), typeof(VolumeComponentSupportedOnCustomSRP) })
                .SetName("Given null SRP asset (Builtin), volumeManager.baseComponentTypeArray contains volume component without attribute but not others"),

            new TestCaseData(
                    typeof(CustomRenderPipelineAsset),
                    new[] { typeof(VolumeComponentSupportedEverywhere), typeof(VolumeComponentSupportedOnAnySRP), typeof(VolumeComponentSupportedOnCustomSRP)},
                    new Type[] {})
                .SetName("Given CustomRenderPipelineAsset, volumeManager.baseComponentTypeArray contains all volume components"),

            new TestCaseData(
                    typeof(SecondCustomRenderPipelineAsset),
                    new[] { typeof(VolumeComponentSupportedEverywhere), typeof(VolumeComponentSupportedOnAnySRP)},
                    new[] { typeof(VolumeComponentSupportedOnCustomSRP) })
                .SetName("Given SecondCustomRenderPipelineAsset, volumeManager.baseComponentTypeArray does not contains component that only supports CustomSRP")
        };

        [Test, TestCaseSource(nameof(s_TestCaseDataGetItem))]
        public void TestVolumeManagerSupportedOnFiltering(Type renderPipelineAssetType, Type[] expectedTypes, Type[] notExpectedTypes)
        {
            var volumeManager = new VolumeManager();
            volumeManager.LoadBaseTypes(renderPipelineAssetType);

            foreach (var expectedType in expectedTypes)
                Assert.That(() => volumeManager.baseComponentTypeArray.First(t => t == expectedType), Throws.Nothing);

            foreach (var notExpectedType in notExpectedTypes)
                Assert.That(() => volumeManager.baseComponentTypeArray.First(t => t == notExpectedType), Throws.InvalidOperationException);
        }
    }
}
