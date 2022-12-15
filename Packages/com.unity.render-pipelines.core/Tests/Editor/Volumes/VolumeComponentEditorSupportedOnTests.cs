using System;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentEditorSupportedOnTests : RenderPipelineTests
    {
        [VolumeComponentMenu("Supported On Tests/No supported on")]
        public class VolumeComponentNoSupportedOn : VolumeComponent
        {
        }

        [HideInInspector]
        [VolumeComponentMenu("Supported On Tests/Hidden")]
        public class VolumeComponentHidden : VolumeComponent
        {
        }

        [VolumeComponentMenu("Supported On Tests/Not Specified Pipeline Supported On")]
        [SupportedOnRenderPipeline]
        public class VolumeComponentNotSpecifiedSupportedOn : VolumeComponent
        {
        }

        [VolumeComponentMenu("Supported On Tests/Not Specified Pipeline Supported On")]
        [SupportedOnRenderPipeline(typeof(CustomRenderPipelineAsset))]
        public class VolumeComponentCustomRenderPipelineAsset : VolumeComponent
        {
        }

        class CustomRenderPipelineAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline()
                => new CustomRenderPipeline();
        }

        class CustomRenderPipeline : RenderPipeline
        {
            protected override void Render(ScriptableRenderContext context, Camera[] cameras)
            {
            }
        }

        static TestCaseData[] s_TestCaseDataGetItem =
        {
            new TestCaseData(null, null, typeof(VolumeComponentNoSupportedOn))
                .SetName("Given null types when asking for supported volume components then returns volume component only without attribute (BuiltIn)"),
            new TestCaseData(typeof(CustomRenderPipeline), typeof(CustomRenderPipelineAsset), typeof(VolumeComponentNoSupportedOn))
                .SetName("Given CustomRenderPipeline types when asking for supported volume components then return contains volume component without attribute (BuiltIn)"),
            new TestCaseData(typeof(CustomRenderPipeline), typeof(CustomRenderPipelineAsset), typeof(VolumeComponentNotSpecifiedSupportedOn))
                .SetName("Given CustomRenderPipeline types when asking for supported volume components then return contains volume component with attribute but without specified pipeline type (Any SRP pipeline)"),
            new TestCaseData(typeof(CustomRenderPipeline), typeof(CustomRenderPipelineAsset), typeof(VolumeComponentCustomRenderPipelineAsset))
                .SetName("Given CustomRenderPipeline types when asking for supported volume components then return contains volume component with attribute with specified pipeline type (Specific SRP pipeline)"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDataGetItem))]
        public void TestGetSupportedVolumeComponents(Type renderPipelineType, Type renderPipelineAssetType, Type expectedType)
        {
            var volumeComponents = VolumeManager.GetSupportedVolumeComponents(renderPipelineType, renderPipelineAssetType);
            Assert.That(() => volumeComponents.First(t => t.Item2 == expectedType), Throws.Nothing);
        }
    }
}
