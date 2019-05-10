using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;

public class HDRP_GraphicTestRunner
{
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases]
    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
        for (int i=0 ; i<5 ; ++i)
            yield return null;

        // Load the test settings
        var settings = GameObject.FindObjectOfType<HDRP_TestSettings>();

        // Search for a valid camera
        var camera = (settings != null)? settings.GetComponent<Camera>() : null;
        if (camera == null) camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera == null) camera = GameObject.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Assert.Fail("Missing camera for graphic tests.");
        }

        Time.captureFramerate = settings.captureFramerate;

        if (settings.doBeforeTest != null)
        {
            settings.doBeforeTest.Invoke();

            // Wait again one frame, to be sure.
            yield return null;
        }

        for (int i=0 ; i<settings.waitFrames ; ++i)
            yield return null;

        var settingsSG = (GameObject.FindObjectOfType<HDRP_TestSettings>() as HDRP_ShaderGraph_TestSettings);
        if (settingsSG == null || !settingsSG.compareSGtoBI)
        {
            if (settings.multiObjectsTest != null && settings.multiObjectsTest.Length > 0)
            {
                // Multiple captures against same reference image

                // Hide all objects
                foreach (var obj in settings.multiObjectsTest)
                    if (obj != null) obj.SetActive(false);

                string baseException = null;
                string failedObjects = "";

                // Hide the previous one, and show the current one, then do a capture and test.
                for (int i = 0; i < settings.multiObjectsTest.Length; ++i)
                {
                    if (i>0 && settings.multiObjectsTest[i-1]!=null) settings.multiObjectsTest[i-1].SetActive(false);
                    if (settings.multiObjectsTest[i] == null ) continue;

                    settings.multiObjectsTest[i].SetActive(true);

                    // Catch if the image comparison failed.
                    try
                    {
                        ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null);
                    }
                    catch (AssertionException e)
                    {
                        var objectHierarchyName = settings.multiObjectsTest[i].name;
                        var parent = settings.multiObjectsTest[i].transform.parent;
                        while (parent != null)
                        {
                            objectHierarchyName = parent.gameObject.name + "/" + objectHierarchyName;
                            parent = parent.parent;
                        }

                        if ( baseException == null) baseException = e.Message;
                        failedObjects += Environment.NewLine + objectHierarchyName;
                    }
                }

                if ( baseException != null)
                    throw new AssertionException( baseException + Environment.NewLine + "Image comparison failed on:" + failedObjects);
            }
            else
            {
                // Standard Test
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null) ? settings.ImageComparisonSettings : null);
            }
        }
        else
        {
            if (settingsSG.sgObjs == null)
            {
                Assert.Fail("Missing Shader Graph objects in test scene.");
            }
            if (settingsSG.biObjs == null)
            {
                Assert.Fail("Missing comparison objects in test scene.");
            }

            settingsSG.sgObjs.SetActive(true);
            settingsSG.biObjs.SetActive(false);
            yield return null; // Wait a frame
            yield return null;
            bool sgFail = false;
            bool biFail = false;

            // First test: Shader Graph
            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
            }
            catch (AssertionException)
            {
                sgFail = true;
            }

            settingsSG.sgObjs.SetActive(false);
            settingsSG.biObjs.SetActive(true);
            settingsSG.biObjs.transform.position = settingsSG.sgObjs.transform.position; // Move to the same location.
            yield return null; // Wait a frame
            yield return null;

            // Second test: HDRP/Lit Materials
            try
            {
                ImageAssert.AreEqual(testCase.ReferenceImage, camera, (settings != null)?settings.ImageComparisonSettings:null);
            }
            catch (AssertionException)
            {
                biFail = true;
            }

            // Informs which ImageAssert failed, if any.
            if (sgFail && biFail) Assert.Fail("Both Shader Graph and Non-Shader Graph Objects failed to match the reference image");
            else if (sgFail) Assert.Fail("Shader Graph Objects failed.");
            else if (biFail) Assert.Fail("Non-Shader Graph Objects failed to match Shader Graph objects.");
        }
    }

#if UNITY_EDITOR

    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif

}
