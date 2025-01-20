using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.LightBaking.Tests
{
    public class FileUtil
    {
        private static readonly string PlatformReferenceImageDirectory = "Assets/ReferenceImages/";
        private static readonly string ReferenceImageBaseDirectory = "Assets/ReferenceImagesBase/";
        public static readonly string LightBakerTempOutputPath = "LightBakerOutput"; // TODO: Use (LightProbe|Lightmap)Request.outputFolderPath instead.
        private static readonly string ActualImageDirectory = "Assets/ActualImagesBase/";

        private static string FormatFilename(string filename) => filename.Replace("(", "_").Replace(")", "_").Replace("\"", "").Replace(",", "-");

        public static string PlatformReferenceImagePath
        {
            get
            {
                string filename = FormatFilename(TestContext.CurrentContext.Test.Name);
                string path = Path.Combine(PlatformReferenceImageDirectory,
                    Path.Combine(GraphicsTestPlatform.Current.ResultsPath, filename));
                bool specificFileExists = File.Exists($"{path}.exr");
                return specificFileExists ? path : Path.Combine(ReferenceImageBaseDirectory, filename);
            }
        }

        public static string ActualImagePath { get => Path.Combine(ActualImageDirectory, FormatFilename(TestContext.CurrentContext.Test.Name)); }
    }
}
