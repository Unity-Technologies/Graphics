using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
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
            
        XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
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
        ScreenCapture.CaptureScreenshot(tempScreenshotFile);
        
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
        actualImage = ChangeTextureFormat(actualImage, testCase.ReferenceImage.format);

        // delete temporary file
        File.Delete(tempScreenshotFile);

        ImageAssert.AreEqual(testCase.ReferenceImage, actualImage, settings.ImageComparisonSettings);
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
