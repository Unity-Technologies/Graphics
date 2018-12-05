#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    public class ResultsUtility
    {
        public const string ActualImagesRoot = "Assets/ActualImages";

        private static T GetEnumPropertyValue<T>(XmlDocument doc, string name)
        {
            var node = doc.SelectSingleNode(string.Format("//property[@name='{0}']", name));
            if (node == null)
                throw new KeyNotFoundException();

            return (T) Enum.Parse(typeof(T), node.Attributes["value"].Value);
        }

        [MenuItem("Tests/Extract images from TestResults.xml...")]
        internal static void ExtractImagesFromResultsXml()
        {
            var filePath =
                EditorUtility.OpenFilePanel("Select TestResults.xml file", Environment.CurrentDirectory, "xml");
            if (!string.IsNullOrEmpty(filePath))
            {
                ResultsUtility.ExtractImagesFromResultsXml(filePath);
            }
        }

        internal static void ExtractImagesFromResultsXml(string xmlFilePath)
        {
            if (!Directory.Exists(ActualImagesRoot))
                Directory.CreateDirectory(ActualImagesRoot);

            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);

            var colorSpace = GetEnumPropertyValue<ColorSpace>(doc, "ColorSpace");
            var platform = GetEnumPropertyValue<RuntimePlatform>(doc, "RuntimePlatform");
            var graphicsDevice = GetEnumPropertyValue<GraphicsDeviceType>(doc, "GraphicsDevice");

            var path = Path.Combine(ActualImagesRoot, string.Format("{0}/{1}/{2}", colorSpace, platform, graphicsDevice));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var imagesWritten = new HashSet<string>();

            foreach (var failedTestCase in doc.SelectNodes("//test-case[@result!='Passed']").OfType<XmlElement>())
            {
                var testName = failedTestCase.Attributes["name"].Value;

                var imageProperty = (XmlElement)failedTestCase.SelectSingleNode("./properties/property[@name='Image']");
                if (imageProperty == null)
                    continue;

                var bytes = Convert.FromBase64String(imageProperty.Attributes["value"].Value);
                var imagePath = path + "/" + testName + ".png";
                File.WriteAllBytes(imagePath, bytes);
                imagesWritten.Add(imagePath);

                var diffProperty = (XmlElement) failedTestCase.SelectSingleNode("./properties/property[@name='DiffImage']");
                if (diffProperty == null)
                    continue;

                bytes = Convert.FromBase64String(diffProperty.Attributes["value"].Value);
                imagePath = path + "/" + testName + ".diff.png";
                File.WriteAllBytes(imagePath, bytes);
                imagesWritten.Add(imagePath);
            }

            AssetDatabase.Refresh();

            Utils.SetupReferenceImageImportSettings(imagesWritten);
        }

        public static void ExtractImagesFromTestProperties(TestContext.TestAdapter test)
        {
            if (!(test.Properties.ContainsKey("Image") ||
                  test.Properties.ContainsKey("DiffImage")))
                return;
            
            var dirName = Path.Combine(ActualImagesRoot, string.Format("{0}/{1}/{2}", UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice));
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            var imagesWritten = new HashSet<string>();

            if (test.Properties.ContainsKey("Image"))
            {
                var bytes = Convert.FromBase64String((string)TestContext.CurrentContext.Test.Properties.Get("Image"));
                var path = Path.Combine(dirName, TestContext.CurrentContext.Test.Name + ".png");
                File.WriteAllBytes(path, bytes);
                imagesWritten.Add(path);
            }

            if (test.Properties.ContainsKey("DiffImage"))
            {
                var bytes = Convert.FromBase64String((string)TestContext.CurrentContext.Test.Properties.Get("DiffImage"));
                var path = Path.Combine(dirName, TestContext.CurrentContext.Test.Name + ".diff.png");
                File.WriteAllBytes(path, bytes);
                imagesWritten.Add(path);
            }

            AssetDatabase.Refresh();

            Utils.SetupReferenceImageImportSettings(imagesWritten);
        }
    }
}
#endif
