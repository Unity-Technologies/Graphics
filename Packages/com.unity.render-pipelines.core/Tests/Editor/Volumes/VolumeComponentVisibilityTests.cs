using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    public class VolumeComponentVisibilityTests : RenderPipelineTests
    {
        [HideInInspector]
        [VolumeComponentMenu("Supported On Tests/No supported on")]
        public class VolumeComponentNoSupportedOn : VolumeComponent
        {
        }

        [HideInInspector]
        [VolumeComponentMenu("Supported On Tests/Not Specified Pipeline Supported On")]
        [SupportedOnRenderPipeline]
        public class VolumeComponentNotSpecifiedSupportedOn : VolumeComponent
        {
        }

        [HideInInspector]
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

        class SecondCustomRenderPipelineAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline()
                => new SecondCustomRenderPipeline();
        }

        class SecondCustomRenderPipeline : RenderPipeline
        {
            protected override void Render(ScriptableRenderContext context, Camera[] cameras)
            {
            }
        }

        static TestCaseData[] s_TestCaseDataGetItem =
        {
            new TestCaseData(typeof(VolumeComponentNoSupportedOn), null)
                .SetName($"Given BuiltIn pipeline When checking visibility for {nameof(VolumeComponent)} editor without attributes Then it's hidden in {nameof(VolumeComponentEditor)}")
                .Returns(false),
            new TestCaseData(typeof(VolumeComponentNotSpecifiedSupportedOn), null)
                .SetName(
                    $"Given BuiltIn pipeline When checking visibility for {nameof(VolumeComponent)} editor with {nameof(SupportedOnRenderPipelineAttribute)} without parameters Then it's hidden in {nameof(VolumeComponentEditor)}")
                .Returns(false),
            new TestCaseData(typeof(VolumeComponentCustomRenderPipelineAsset), null)
                .SetName(
                    $"Given BuiltIn pipeline When checking visibility for {nameof(VolumeComponent)} editor with {nameof(SupportedOnRenderPipelineAttribute)} with {nameof(CustomRenderPipelineAsset)} Then it's hidden in {nameof(VolumeComponentEditor)}")
                .Returns(false),
            new TestCaseData(typeof(VolumeComponentNoSupportedOn), typeof(SecondCustomRenderPipelineAsset))
                .SetName($"Given {nameof(SecondCustomRenderPipelineAsset)} pipeline When checking visibility for {nameof(VolumeComponent)} editor without attributes Then it's visible in {nameof(VolumeComponentEditor)}")
                .Returns(true),
            new TestCaseData(typeof(VolumeComponentNotSpecifiedSupportedOn), typeof(SecondCustomRenderPipelineAsset))
                .SetName(
                    $"Given {nameof(SecondCustomRenderPipelineAsset)} pipeline When checking visibility for {nameof(VolumeComponent)} editor with {nameof(SupportedOnRenderPipelineAttribute)} without parameters Then it's visible in {nameof(VolumeComponentEditor)}")
                .Returns(true),
            new TestCaseData(typeof(VolumeComponentCustomRenderPipelineAsset), typeof(SecondCustomRenderPipelineAsset))
                .SetName(
                    $"Given {nameof(SecondCustomRenderPipelineAsset)} pipeline When checking visibility for {nameof(VolumeComponent)} editor with {nameof(SupportedOnRenderPipelineAttribute)} with {nameof(CustomRenderPipelineAsset)} Then it's hidden in {nameof(VolumeComponentEditor)}")
                .Returns(false),
            new TestCaseData(typeof(VolumeComponentNoSupportedOn), typeof(CustomRenderPipelineAsset))
                .SetName($"Given {nameof(CustomRenderPipelineAsset)} pipeline When checking visibility for {nameof(VolumeComponent)} editor without attributes Then it's visible in {nameof(VolumeComponentEditor)}")
                .Returns(true),
            new TestCaseData(typeof(VolumeComponentNotSpecifiedSupportedOn), typeof(CustomRenderPipelineAsset))
                .SetName(
                    $"Given {nameof(CustomRenderPipelineAsset)} pipeline When checking visibility for {nameof(VolumeComponent)} editor with {nameof(SupportedOnRenderPipelineAttribute)} without parameters Then it's visible in {nameof(VolumeComponentEditor)}")
                .Returns(true),
            new TestCaseData(typeof(VolumeComponentCustomRenderPipelineAsset), typeof(CustomRenderPipelineAsset))
                .SetName(
                    $"Given {nameof(CustomRenderPipelineAsset)} pipeline When checking visibility for {nameof(VolumeComponent)} editor with {nameof(SupportedOnRenderPipelineAttribute)} with {nameof(CustomRenderPipelineAsset)} Then it's visible in {nameof(VolumeComponentEditor)}")
                .Returns(true),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDataGetItem))]
        public bool DetermineVisibilityMethodTests(Type volumeComponentType, Type renderPipelineAssetType)
        {
            //Arrange
            SetupRenderPipeline(renderPipelineAssetType);
            var component = (VolumeComponent)ScriptableObject.CreateInstance(volumeComponentType);
            var editor = (VolumeComponentEditor)Editor.CreateEditor(component);
            editor.Init();

            //Act
            editor.DetermineVisibility(renderPipelineAssetType, RenderPipelineManager.currentPipeline?.GetType());
            bool visible = editor.visible;

            ScriptableObject.DestroyImmediate(editor);
            ScriptableObject.DestroyImmediate(component);

            //Assert
            return visible;
        }
    }
}
