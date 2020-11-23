using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

public class UniversalGraphicsTests
{
#if UNITY_ANDROID
    static bool wasFirstSceneRan = false;
    const int firstSceneAdditionalFrames = 3;
#endif
    public const string universalPackagePath = "Assets/ReferenceImages";

    [UnityTest, Category("UniversalRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(universalPackagePath)]


    public IEnumerator Run(GraphicsTestCase testCase)
    {
        //Ignore this test if test scene is NOT in the deferred test scene list
        if ( !System.Array.Exists(IncludeTheseTestsForDeferred, x => testCase.ScenePath.ToString().Contains(x)) )
        {
            Assert.Ignore("This test is ignored for Deferred Rendering Path."); 
        }

#if ENABLE_VR
        // XRTODO: Fix XR tests on macOS or disable them from Yamato directly
        if (XRGraphicsAutomatedTests.enabled && (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer))
            Assert.Ignore("Universal XR tests do not run on macOS.");
#endif
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<UniversalGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find UniversalGraphicsTestSettings");

#if ENABLE_VR
        if (XRGraphicsAutomatedTests.enabled)
        {
            if (settings.XRCompatible)
            {
                XRGraphicsAutomatedTests.running = true;
            }
            else
            {
                Assert.Ignore("Test scene is not compatible with XR and will be skipped.");
            }
        }
#endif

        Scene scene = SceneManager.GetActiveScene();

        yield return null;

        int waitFrames = settings.WaitFrames;

        if (settings.ImageComparisonSettings.UseBackBuffer && settings.WaitFrames < 1)
        {
            waitFrames = 1;
        }
        for (int i = 0; i < waitFrames; i++)
            yield return new WaitForEndOfFrame();

#if UNITY_ANDROID
        // On Android first scene often needs a bit more frames to load all the assets
        // otherwise the screenshot is just a black screen
        if (!wasFirstSceneRan)
        {
            for(int i = 0; i < firstSceneAdditionalFrames; i++)
            {
                yield return null;
            }
            wasFirstSceneRan = true;
        }
#endif

        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);

        // Does it allocate memory when it renders what's on the main camera?
        bool allocatesMemory = false;
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        try
        {
            ImageAssert.AllocatesMemory(mainCamera, settings?.ImageComparisonSettings);
        }
        catch (AssertionException)
        {
            allocatesMemory = true;
        }

        if (allocatesMemory)
            Assert.Fail("Allocated memory when rendering what is on main camera");
    }

    public static void SetRenderersToDeferred()
    {
        UniversalRenderPipelineAsset pipelineAsset = (UniversalRenderPipelineAsset) GraphicsSettings.renderPipelineAsset;
        if(pipelineAsset != null)
        {
            var rendererList = pipelineAsset.m_RendererDataList;
            for(int i=0; i<rendererList.Length; i++)
            {
                if(rendererList[i].GetType() == typeof(ForwardRendererData))
                {
                    var forwardData = (ForwardRendererData) rendererList[i];
                    forwardData.renderingMode = RenderingMode.Deferred;
                }
            }
        }
    }

    public static string[] IncludeTheseTestsForDeferred = new string[]
    {
        "001_SimpleCube",
        "053_UnlitShader",

        "007_LitShaderMaps",
        "012_PBS_EnvironmentBRDF_Spheres",
        "026_Shader_PBRscene",
        "035_Shader_TerrainShaders",

        "029_Particles",
        "037_Particles_Standard",

        "050_Shader_Graphs",

        "023_Lighting_Mixed_Indirect",
        "043_Lighting_Mixed_ShadowMask",
        "049_Lighting_Mixed_Subtractive",

        "010_AdditionalLightsSorted_Deferred",
        "018_LightLayers",

        "111_CameraStackMSAA",
        "123_CameraStackingClear",
        "014_CameraMulti_MiniMap",
        "015_CameraMulti_FPSCam",

        "140_SSAO_DepthOnly_Projection",
        "142_SSAO_DepthNormal_Projection",
        "144_SSAO_RenderToBackBuffer"
    };


#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }

#if ENABLE_VR
    [TearDown]
    public void ResetSystemState()
    {
        XRGraphicsAutomatedTests.running = false;
    }
#endif
#endif
}
