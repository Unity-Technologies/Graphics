using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentEditorSupportedOnTests : RenderPipelineTests
    {
        [VolumeComponentMenu("SupportedOnTests/SupportedEverywhere")]
        public class VolumeComponentSupportedEverywhere : VolumeComponent
        {
        }

        [VolumeComponentMenu("SupportedOnTests/SupportedOnAnySRP")]
        [SupportedOnRenderPipeline]
        public class VolumeComponentSupportedOnAnySRP : VolumeComponent
        {
        }

        [VolumeComponentMenu("SupportedOnTests/SupportedOnCustomSRP")]
        [SupportedOnRenderPipeline(typeof(CustomSRPAsset))]
        public class VolumeComponentSupportedOnCustomSRP : VolumeComponent
        {
        }

        class CustomSRPAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline() => throw new NotImplementedException();
        }

        class AnotherSRPAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline() => throw new NotImplementedException();
        }

        static TestCaseData[] s_TestCaseDataGetItem =
        {
            new TestCaseData(
                    null,
                    new[] { typeof(VolumeComponentSupportedEverywhere) },
                    new[] { typeof(VolumeComponentSupportedOnAnySRP), typeof(VolumeComponentSupportedOnCustomSRP) })
                .SetName("Given null SRP asset (Builtin), volumeManager.baseComponentTypeArray contains volume component without attribute but not others"),

            new TestCaseData(
                    typeof(CustomSRPAsset),
                    new[] { typeof(VolumeComponentSupportedEverywhere), typeof(VolumeComponentSupportedOnAnySRP), typeof(VolumeComponentSupportedOnCustomSRP)},
                    new Type[] {})
                .SetName("Given CustomSRPAsset, volumeManager.baseComponentTypeArray contains all volume components"),

            new TestCaseData(
                    typeof(AnotherSRPAsset),
                    new[] { typeof(VolumeComponentSupportedEverywhere), typeof(VolumeComponentSupportedOnAnySRP)},
                    new[] { typeof(VolumeComponentSupportedOnCustomSRP) })
                .SetName("Given AnotherSRPAsset, volumeManager.baseComponentTypeArray does not contains component that only supports CustomSRP")
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
