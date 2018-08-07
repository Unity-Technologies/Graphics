using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Rendering;
using Attribute = System.Attribute;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks a test which takes <c>GraphicsTestCase</c> instances as wanting to have them generated automatically by
    /// the scene/reference-image management feature in the framework. 
    /// </summary>
    public class UseGraphicsTestCasesAttribute : Attribute, ITestBuilder
    {
        string m_ReferenceImagePath = string.Empty;

        public UseGraphicsTestCasesAttribute()
        {}

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


        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            List<TestMethod> results = new List<TestMethod>();

            IGraphicsTestCaseProvider provider = Provider;

            try
            {
                foreach (var testCase in provider.GetTestCases())
                {
                    var test = new TestMethod(method, suite)
                    {
                        parms = new TestCaseParameters(new object[] {testCase})
                    };
                    test.parms.ApplyToTest(test);
                    test.Name = System.IO.Path.GetFileNameWithoutExtension(testCase.ScenePath);

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

            Console.WriteLine("Generated {0} graphics test cases.", results.Count);
            return results;
        }
    }
}
