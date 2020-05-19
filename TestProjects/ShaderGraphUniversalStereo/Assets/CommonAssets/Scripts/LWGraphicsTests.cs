using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class LWGraphicsTests
{

    public const string lwPackagePath = "Assets/ReferenceImages";

    [UnityTest, Category("LightWeightRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(lwPackagePath)]


    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<LWGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find LWGraphicsTestSettings");

        // Stereo screen capture on Mac generates monoscopic images and won't be fixed.
        Assume.That((Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer), "Stereo tests do not run on MacOSX.");

        var referenceImage = testCase.ReferenceImage;
        // make sure we're rendering in the same size as the reference image, otherwise this is not really comparable.
        Screen.SetResolution(referenceImage.width, referenceImage.height, FullScreenMode.Windowed);

#if UNITY_2020_2_OR_NEWER
        // Ensure a valid XR display is active
        List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances(xrDisplays);
        Assume.That(xrDisplays.Count > 0 && xrDisplays[0].running, "No XR display active!");

        // Set mirror view to side-by-side (both eyes)
        xrDisplays[0].SetPreferredMirrorBlitMode(XRMirrorViewBlitMode.SideBySide);
#else
        XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
#endif

        yield return null;

        foreach (var camera in cameras)
            camera.stereoTargetEye = StereoTargetEyeMask.Both;

        var tempScreenshotFile = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        // clean up previous file if it happens to exist at this point
        if(FileAvailable(tempScreenshotFile))
            System.IO.File.Delete(tempScreenshotFile);

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

        // wait for rendering to complete
        yield return new WaitForEndOfFrame();

        // we'll take a screenshot here, as what we want to compare is the actual result on-screen.
        // ScreenCapture.CaptureScreenshotAsTexture --> does not work since colorspace is wrong, would need colorspace change and thus color compression
        // ScreenCapture.CaptureScreenshotIntoRenderTexture --> does not work since texture is flipped, would need another pass
        // so we need to capture and reload the resulting file.
        ScreenCapture.CaptureScreenshot(tempScreenshotFile, ScreenCapture.StereoScreenCaptureMode.BothEyes);

        // NOTE: there's discussions around whether Unity has actually documented this correctly.
        // Unity says: next frame MUST have the file ready
        // Community says: not true, file write might take longer, so have to explicitly check the file handle before use
        // https://forum.unity.com/threads/how-to-wait-for-capturescreen-to-complete.172194/
        yield return null;
        while(!FileAvailable(tempScreenshotFile))
            yield return null;

        // load the screenshot back into memory and change to the same format as we want to compare with
        var actualImage = new Texture2D(1,1);
        actualImage.LoadImage(System.IO.File.ReadAllBytes(tempScreenshotFile));

        if(actualImage.width != referenceImage.width || actualImage.height != referenceImage.height) {
            Debug.LogWarning("[" + testCase.ScenePath + "] Image size differs (ref: " + referenceImage.width + "x" + referenceImage.height + " vs. actual: " + actualImage.width + "x" + actualImage.height + "). " + (Application.isEditor ? " is your GameView set to a different resolution than the reference images?" : "is your build size different than the reference images?"));
            actualImage = ChangeTextureSize(actualImage, referenceImage.width, referenceImage.height);
        }
        // ref is usually in RGB24 or RGBA32 while actual is in ARGB32, we need to convert formats
        if(referenceImage.format != actualImage.format) {
            actualImage = ChangeTextureFormat(actualImage, referenceImage.format);
        }

        // delete temporary file
        File.Delete(tempScreenshotFile);

        // for testing
        // File.WriteAllBytes("reference.png", referenceImage.EncodeToPNG());
        // File.WriteAllBytes("actual.png", actualImage.EncodeToPNG());

        ImageAssert.AreEqual(referenceImage, actualImage, settings.ImageComparisonSettings);
    }

    static bool FileAvailable(string path) {
        if (!File.Exists(path)) {
            return false;
        }

        FileInfo file = new System.IO.FileInfo(path);
        FileStream stream = null;

        try {
            stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        }
        catch (IOException) {
            // Can be either:
            // - file is processed by another thread
            // - file is still being written to
            // - file does not really exist yet
            return false;
        }
        finally {
            if (stream != null)
                stream.Close();
        }

        return true;
    }


    static Texture2D ChangeTextureSize(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Bilinear;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        var nTex = new Texture2D(newWidth, newHeight, source.format, false);
        nTex.ReadPixels(new Rect(0, 0, newWidth, newWidth), 0,0);
        nTex.Apply();
        RenderTexture.active = null;
        return nTex;
    }

    static Texture2D ChangeTextureFormat(Texture2D texture, TextureFormat newFormat)
    {
        Texture2D tex = new Texture2D(texture.width, texture.height, newFormat, false);
        tex.SetPixels(texture.GetPixels());
        tex.Apply();

        return tex;
    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
