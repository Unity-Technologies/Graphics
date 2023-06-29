using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.LightBaking.Tests
{
    public class FileUtil
    {
        private static readonly string PlatformReferenceImageDirectory = "Assets/ReferenceImages/";
        public static readonly string LightBakerTempOutputPath = "LightBakerOutput"; // TODO: Use (LightProbe|Lightmap)Request.outputFolderPath instead.
        private static readonly string ActualImageDirectory = "Assets/ActualImagesBase/";

        private static string FormatFilename(string filename) => filename.Replace("(", "_").Replace(")", "_").Replace("\"", "").Replace(",", "-");

        public static string PlatformReferenceImagePath { get => Path.Combine(PlatformReferenceImageDirectory, Path.Combine(TestUtils.GetCurrentTestResultsFolderPath(), FormatFilename(TestContext.CurrentContext.Test.Name))); }
        public static string ActualImagePath { get => Path.Combine(ActualImageDirectory, FormatFilename(TestContext.CurrentContext.Test.Name)); }
    }
}
