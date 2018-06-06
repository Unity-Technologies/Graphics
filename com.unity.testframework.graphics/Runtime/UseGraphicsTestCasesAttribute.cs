using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Attribute = System.Attribute;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks a test which takes <c>GraphicsTestCase</c> instances as wanting to have them generated automatically by
    /// the scene/reference-image management feature in the framework. 
    /// </summary>
    public class UseGraphicsTestCasesAttribute : Attribute, ITestBuilder
    {
        /// <summary>
        /// The <c>IGraphicsTestCaseProvider</c> which will be used to generate the <c>GraphicsTestCase</c> instances for the tests.
        /// </summary>
        public static IGraphicsTestCaseProvider Provider
        {
            get
            {
#if UNITY_EDITOR
                return new UnityEditor.TestTools.Graphics.EditorGraphicsTestCaseProvider();
#else
                return new RuntimeGraphicsTestCaseProvider();
#endif
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

            suite.Properties.Set("ColorSpace", provider.ColorSpace);
            suite.Properties.Set("RuntimePlatform", provider.Platform);
            suite.Properties.Set("GraphicsDevice", provider.GraphicsDevice);

            Console.WriteLine("Generated {0} graphics test cases.", results.Count);
            return results;
        }
    }
}