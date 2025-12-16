using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class ShaderGraphGraphicsTests
{
    [IgnoreGraphicsTest("InputNodes|SamplerStateTests|UVNodes", "GLES3 renders these tests incorrectly (FB: 1354427)", runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Android }, graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.PlayStation4 })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.XboxOne })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Metal })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLCore })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D12 })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Switch })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.XboxOneD3D12 })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.GameCoreXboxOne })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.GameCoreXboxSeries })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.PlayStation5 })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.PlayStation5NGGC })]
    [IgnoreGraphicsTest("InstanceIDWithKeywords", "Platform Independent", graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.WebGPU })]
    [IgnoreGraphicsTest("TransformNode", "Test is unstable", colorSpaces: new ColorSpace[] { ColorSpace.Linear }, runtimePlatforms: new RuntimePlatform[] { RuntimePlatform.Android }, graphicsDeviceTypes: new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan })]
    [IgnoreGraphicsTest("InstancedRendering", "Test requires conversion to Render Graph")]
    
    [SceneGraphicsTest("Assets/Scenes")]
    [UnityTest, Category("ShaderGraph")]
    public IEnumerator Run(SceneGraphicsTestCase testCase)
    {
        GraphicsTestLogger.Log($"Running test case {testCase.ScenePath} with reference image {testCase.ScenePath}. {testCase.ReferenceImage.LoadMessage}.");
		GraphicsTestLogger.Log($"Running test case '{testCase}' with scene '{testCase.ScenePath}' {testCase.ReferenceImage.LoadMessage}.");
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        var settings = Object.FindFirstObjectByType<ShaderGraphGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find ShaderGraphGraphicsTestSettings");
        settings.OnTestBegin();

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage.Image, camera, settings.ImageComparisonSettings, testCase.ReferenceImage.LoadMessage);
        settings.OnTestComplete();
    }
}
