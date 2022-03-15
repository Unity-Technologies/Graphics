using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using Attribute = System.Attribute;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks a test which takes <c>GraphicsTestCase</c> instances as wanting to have them generated automatically by
    /// the scene/reference-image management feature in the framework.
    /// </summary>
    public class UseGraphicsTestCasesAttribute : UnityEngine.TestTools.UnityTestAttribute, ITestBuilder
    {
        string m_ReferenceImagePath = string.Empty;

        NUnitTestCaseBuilder _builder = new NUnitTestCaseBuilder();

        public UseGraphicsTestCasesAttribute() { }

        public UseGraphicsTestCasesAttribute(string referenceImagePath)
        {
            m_ReferenceImagePath = referenceImagePath;
        }

        /// <summary>
        /// The <c>IGraphicsTestCaseProvider</c> which will be used to generate the <c>GraphicsTestCase</c> instances for the tests.
        /// </summary>
        public IGraphicsTestCaseProvider Provider
        {
            get
            {
#if UNITY_EDITOR
                return new UnityEditor.TestTools.Graphics.EditorGraphicsTestCaseProvider(m_ReferenceImagePath);
#else
                return new RuntimeGraphicsTestCaseProvider();
#endif
            }
        }

        public static ColorSpace ColorSpace
        {
            get
            {
                return QualitySettings.activeColorSpace;
            }
        }

        public static RuntimePlatform Platform
        {
            get
            {
                return Application.platform;
            }
        }

        public static GraphicsDeviceType GraphicsDevice
        {
            get
            {
                return SystemInfo.graphicsDeviceType;
            }
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
                if (XR.XRSettings.enabled && XR.XRSettings.loadedDeviceName.Length > 0)
                    return XR.XRSettings.loadedDeviceName;

#endif
                return "None";
            }
        }


        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            List<TestMethod> results = new List<TestMethod>();

            IGraphicsTestCaseProvider provider = Provider;

            try
            {
                foreach (var testCase in provider.GetTestCases())
                {
                    TestCaseData data = new TestCaseData(new object[] { testCase });

                    data.SetName(testCase.Name);
                    data.ExpectedResult = new Object();
                    data.HasExpectedResult = true;

                    TestMethod test = this._builder.BuildTestMethod(method, suite, data);
                    if (test.parms != null)
                        test.parms.HasExpectedResult = false;

                    test.Name = testCase.Name;

                    results.Add(test);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to generate graphics testcases!");
                Debug.LogException(ex);
                throw;
            }

            suite.Properties.Set("ColorSpace", ColorSpace);
            suite.Properties.Set("RuntimePlatform", Platform);
            suite.Properties.Set("GraphicsDevice", GraphicsDevice);
            suite.Properties.Set("LoadedXRDevice", LoadedXRDevice);

            Console.WriteLine("Generated {0} graphics test cases.", results.Count);
            return results;
        }

        public static GraphicsTestCase GetCaseFromScenePath(string scenePath, string referenceImagePath = null)
        {
            UseGraphicsTestCasesAttribute tmp = new UseGraphicsTestCasesAttribute(string.IsNullOrEmpty(referenceImagePath) ? String.Empty : referenceImagePath);

            var provider = tmp.Provider;

            return provider.GetTestCaseFromPath(scenePath);
        }
    }
}
