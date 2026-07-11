using System.Collections;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    // NOTE: Important! IgnoreGraphicsTest uses a pattern to ignore all Unity-scenes found which name matches that pattern.
    // That means that, you can filter-out more than one test without realizing if not careful (i.e. the pattern '5011_VolumetricClouds'
    // will filter-out the tests 5011_VolumetricClouds, 5011_VolumetricCloudsShadows and 5011_VolumetricCloudsShadowsBake).
    // To avoid this, add an extra '$' character to the end of your pattern to make it a strict-match.
    [MockHmdSetup]
    [TestFixtureSource(
        typeof(HighDefinitionTestFixtureData),
        nameof(HighDefinitionTestFixtureData.FixtureParams)
    )]
    public class ShaderGraphTests : HdrpTestsBase
    {
        public ShaderGraphTests(GpuResidentDrawerContext grdContext) : base(grdContext) { }

        [UnityTest]
        [SceneGraphicsTest(@"Assets/GraphicTests/Scenes/8x_ShaderGraph/^[0-9]+")]
        [Timeout(450 * 1000)]
        [IgnoreGraphicsTest(
            "8101_Opaque$",
            "(Intel Mac) Small divergence around 'Iridescence Specular Occlusion from Bent Normal' material. Might need a ref-image update.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest("8207_InstanceIDWithKeywords$", "Missing ref-image.")]
        [IgnoreGraphicsTest(
            "8212_Fullscreen",
            "Unstable - see https://jira.unity3d.com/browse/UUM-100863",
            GraphicsDeviceType.Metal
        )]
        [IgnoreGraphicsTest(
            "8213_Thickness$",
            "(Intel Mac) Bunny in the middle should be translucent-pink, not black.",
            GraphicsDeviceType.Metal,
            Architecture.X64
        )]
        [IgnoreGraphicsTest(
            "8109_IsFrontFace",
            "Disabled for Instability https://jira.unity3d.com/browse/UUM-146901",
            RuntimePlatform.WindowsEditor
        )]
        [TestNotSupportedOn(
            "8103|8107|8108|8207_InstanceID|8207_InstancingAPIs|8210|8211",
            "Not XR compatible",
            StereoRenderingContext.StereoEnabled
        )]
        [TestNotSupportedOn(
            "8103|8107|8108|8207_InstanceID|8207_InstancingAPIs|8210|8211",
            "Not XR compatible", XrDevice.Any
        )]
        public IEnumerator GraphicsTest(SceneGraphicsTestCase testCase)
        {
            yield return HDRP_GraphicTestRunner.Run(testCase);
        }

    }
}
