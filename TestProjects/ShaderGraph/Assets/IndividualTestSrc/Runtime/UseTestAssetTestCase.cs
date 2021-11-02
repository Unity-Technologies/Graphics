using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;
using UnityEngine.XR.Management;
//using UnityEngine.XR.Management;
using Attribute = System.Attribute;

public class UseTestAssetTestCaseAttribute : UnityEngine.TestTools.UnityTestAttribute, ITestBuilder
{

     public interface ITestAssetTestProvider
    {
        public IEnumerable<TestAssetTestData> GetTestCases();
    }

    public static string LoadedXRDevice
    {
        get
        {
#if ENABLE_VR || ENABLE_AR
            // Reuse standard (non-VR) reference images
            if (RuntimeSettings.reuseTestsForXR)
                return "None";

            // XR SDK path
            var activeLoader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (activeLoader != null)
                return activeLoader.name;

            // Legacy VR path
            if (UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.loadedDeviceName.Length > 0)
                return UnityEngine.XR.XRSettings.loadedDeviceName;

#endif
            return "None";
        }
    }

#if UNITY_EDITOR
    public class EditorProvider : ITestAssetTestProvider
    {
        static string k_fileLocation = $"Assets/ReferenceImages/{QualitySettings.activeColorSpace}/{Application.platform}/{SystemInfo.graphicsDeviceType}/{LoadedXRDevice}";
        public IEnumerable<TestAssetTestData> GetTestCases()
        {
            Debug.Log("EditorProvider.GetTestCases()");
            List<TestAssetTestData> output = new List<TestAssetTestData>();
            foreach(var testAsset in SetupTestAssetTestCases.ShaderGraphTests)
            {
                Debug.Log("TestAsset:" + testAsset.name);
                foreach(var individualTest in testAsset.testMaterial)
                {
                    if(testAsset.name == null || individualTest.material == null || individualTest.material.name == null)
                    {
                        continue;
                    }
                    var directory = $"{k_fileLocation}/{testAsset.name}/";
                    if(!TestAssetTestData.TryGetTestData(directory, testAsset.name, individualTest.material, out TestAssetTestData data))
                    {
                        continue;
                    }

                    data.referenceImage = AssetDatabase.LoadAssetAtPath<Texture2D>(data.GetReferenceImagePath());
                    data.testMaterial = individualTest.material;
                    data.customMesh = testAsset.customMesh;
                    output.Add(data);
                }
            }
            return output;
        }
    }
#else

    public class PlayerProvider : ITestAssetTestProvider
    {
        public IEnumerable<TestAssetTestData> GetTestCases()
        {
            Debug.Log("PlayerProvider.GetTestCases()");
            AssetBundle referenceImagesBundle = null;

            // apparently unity automatically saves the asset bundle as all lower case
            var referenceImagesBundlePath = string.Format("referenceimages--individual--{0}-{1}-{2}-{3}",
                UseGraphicsTestCasesAttribute.ColorSpace,
                UseGraphicsTestCasesAttribute.Platform,
                UseGraphicsTestCasesAttribute.GraphicsDevice,
                UseGraphicsTestCasesAttribute.LoadedXRDevice).ToLower();

            referenceImagesBundlePath = Path.Combine(Application.streamingAssetsPath, referenceImagesBundlePath);

#if UNITY_ANDROID
            // Unlike standalone where you can use File.Read methods and pass the path to the file,
            // Android requires UnityWebRequest to read files from local storage
            referenceImagesBundle = GetRefImagesBundleViaWebRequest(referenceImagesBundlePath);

#else
            if (File.Exists(referenceImagesBundlePath))
                referenceImagesBundle = AssetBundle.LoadFromFile(referenceImagesBundlePath);

#endif

            if (referenceImagesBundle != null)
            {
                foreach (TextAsset individualTestData in referenceImagesBundle.LoadAllAssets(typeof(TextAsset)))
                {
                    TestAssetTestData data = new TestAssetTestData();
                    data.FromJson(individualTestData.text);
                    Debug.Log("data:" + data.testName);
                    if (data.TestMaterialLookup == null || data.TestMaterialLookup.Length == 0)
                    {
                        continue;
                    }
                    Debug.Log("data.TestMaterialPath:" + data.TestMaterialLookup);
                    data.testMaterial = referenceImagesBundle.LoadAsset<Material>(data.TestMaterialLookup);
                    Debug.Log("material loaded " + (data.testMaterial != null).ToString());
                    if(data.CustomMeshLookup != null && data.CustomMeshLookup.Length > 0)
                    {
                        data.customMesh = referenceImagesBundle.LoadAsset<Mesh>(data.CustomMeshLookup);
                    }
                    if(data.ReferenceImageLookup != null && data.ReferenceImageLookup.Length > 0)
                    {
                        data.referenceImage = referenceImagesBundle.LoadAsset<Texture2D>(data.ReferenceImageLookup);
                    }
                    Debug.Log("returning data");
                    yield return data;
                }
            }
        }

        private AssetBundle GetRefImagesBundleViaWebRequest(string referenceImagesBundlePath)
        {
            AssetBundle referenceImagesBundle = null;
            using (var webRequest = new UnityWebRequest(referenceImagesBundlePath))
            {
                var handler = new DownloadHandlerAssetBundle(referenceImagesBundlePath, 0);
                webRequest.downloadHandler = handler;

                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    // wait for response
                }

                if (string.IsNullOrEmpty(webRequest.error))
                {
                    referenceImagesBundle = handler.assetBundle;
                }
                else
                {
                    Debug.Log("Error loading reference image bundle, " + webRequest.error);
                }
            }
            return referenceImagesBundle;
        }
    }
#endif

    public ITestAssetTestProvider Provider
    {
        get
        {
#if UNITY_EDITOR
            return new EditorProvider();
#else
            return new PlayerProvider();
#endif
        }
    }


    NUnitTestCaseBuilder m_builder = new NUnitTestCaseBuilder();

    IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
    {
        List<TestMethod> results = new List<TestMethod>();

        foreach (var materialTest in Provider.GetTestCases())
        {
            if (materialTest.testMaterial == null || materialTest.testName == null || materialTest.testName.Length == 0)
            {
                continue;
            }

            TestCaseData data = new TestCaseData(new object[] { materialTest });
            data.SetName(materialTest.testMaterial.name);
            data.ExpectedResult = new UnityEngine.Object();
            data.HasExpectedResult = true;
            data.SetCategory(materialTest.testName);

            TestMethod test = this.m_builder.BuildTestMethod(method, suite, data);
            if (test.parms != null)
                test.parms.HasExpectedResult = false;

            string testName = $"{materialTest.testName}-{materialTest.testMaterial.name}";
            test.Name = testName;
            results.Add(test);
        }

        return results;
    }


}

