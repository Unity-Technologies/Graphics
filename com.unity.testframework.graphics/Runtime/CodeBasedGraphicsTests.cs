using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Interfaces;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.TestTools.TestRunner.Api;
#endif

namespace UnityEngine.TestTools.Graphics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public class CodeBasedGraphicsTestAttribute : Attribute
    {
        public string ReferenceImagesRoot { get; private set; }
        public string ActualImagesRoot { get; private set; }

        public CodeBasedGraphicsTestAttribute(string referenceImagesRoot, string actualImagesRoot = null)
        {
            ReferenceImagesRoot = referenceImagesRoot;
            ActualImagesRoot = string.IsNullOrEmpty(actualImagesRoot) ? Path.Combine(Path.GetDirectoryName(referenceImagesRoot), "ActualImages") : actualImagesRoot;
        }
    }

#if UNITY_EDITOR
    public static class CodeBasedGraphicsTests
    {
        public static void AsyncEnumerate(Action<IEnumerable<GraphicsTestCase>> onEnumerate)
        {
            if (onEnumerate == null)
                throw new ArgumentNullException(nameof(onEnumerate));

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RetrieveTestList(TestMode.EditMode, (testRoot) =>
            {
                var newGraphicsTests = new List<GraphicsTestCase>();
                TraverseTests(testRoot, newGraphicsTests);
                if (newGraphicsTests.Count > 0)
                    onEnumerate(newGraphicsTests);
            });
        }

        public static Texture2D LoadReferenceImage(string testName, CodeBasedGraphicsTestAttribute attrib)
        {
            if (attrib == null)
                throw new ArgumentNullException(nameof(attrib));

            var referenceImageFolder = Path.Combine(attrib.ReferenceImagesRoot, TestUtils.GetCurrentTestResultsFolderPath());
            var referenceImagePath = Path.Combine(referenceImageFolder, $"{testName}.png");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);
        }

        public static bool TryFindCodeBasedGraphicsTestAttribute(ITest test, out CodeBasedGraphicsTestAttribute attrib)
            => TryFindCodeBasedGraphicsTestAttribute(test.Method, test.TypeInfo, out attrib);

        public static bool TryFindCodeBasedGraphicsTestAttribute(ITestAdaptor test, out CodeBasedGraphicsTestAttribute attrib)
            => TryFindCodeBasedGraphicsTestAttribute(test.Method, test.TypeInfo, out attrib);

        private static bool TryFindCodeBasedGraphicsTestAttribute(IMethodInfo method, ITypeInfo type, out CodeBasedGraphicsTestAttribute attrib)
        {
            var attribs = method.GetCustomAttributes<CodeBasedGraphicsTestAttribute>(true);
            if (attribs.Length == 0)
                attribs = type.GetCustomAttributes<CodeBasedGraphicsTestAttribute>(true);
            if (attribs.Length != 0)
            {
                attrib = attribs[0];
                return true;
            }
            else
            {
                attrib = null;
                return false;
            }
        }

        private static void TraverseTests(ITestAdaptor test, List<GraphicsTestCase> collection)
        {
            if (!test.HasChildren && Array.IndexOf(test.Categories, "Graphics") >= 0
                && TryFindCodeBasedGraphicsTestAttribute(test, out var attrib))
            {
                var referenceImage = LoadReferenceImage(test.Name, attrib);
                collection.Add(new GraphicsTestCase(test.Name, attrib, referenceImage));
            }

            foreach (var child in test.Children)
                TraverseTests(child, collection);
        }
    }
#endif
}
