using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provides test assertion helpers for working with images.
    /// </summary>
    public class ImageAssert
    {
        const int k_BatchSize = 1024;

        /// <summary>
        /// Render an image from the given camera and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image that should be rendered by the camera.</param>
        /// <param name="camera">The camera to render from.</param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        public static void AreEqual(Texture2D expected, Camera camera, ImageComparisonSettings settings = null)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            AreEqual(expected, new List<Camera>{camera}, settings);
        }

        /// <summary>
        /// Render an image from the given cameras and compare it to the reference image.
        /// </summary>
        /// <param name="expected">The expected image that should be rendered by the camera.</param>
        /// <param name="cameras">The cameras to render from.</param>
        /// <param name="settings">Optional settings that control how the image comparison is performed. Can be null, in which case the rendered image is required to be exactly identical to the reference.</param>
        public static void AreEqual(Texture2D expected, IEnumerable<Camera> cameras, ImageComparisonSettings settings = null)
        {
            if (cameras == null)
                throw new ArgumentNullException(nameof(cameras));

            if (settings == null)
                settings = new ImageComparisonSettings();

            int width = settings.TargetWidth;
            int height = settings.TargetHeight;
            var format = expected != null ? expected.format : TextureFormat.ARGB32;

            var rt = RenderTexture.GetTemporary(width, height, 24);
            Texture2D actual = null;
            try
            {
                foreach (var camera in cameras)
                {
                    camera.targetTexture = rt;
                    camera.Render();
                    camera.targetTexture = null;
                }

                actual = new Texture2D(width, height, format, false);
                RenderTexture.active = rt;
                actual.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = null;

                actual.Apply();

                AreEqual(expected, actual, settings);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                if (actual != null)
                    UnityEngine.Object.Destroy(actual);
            }
        }

        /// <summary>
        /// Compares an image to a 'reference' image to see if it looks correct.
        /// </summary>
        /// <param name="expected">What the image is supposed to look like.</param>
        /// <param name="actual">What the image actually looks like.</param>
        /// <param name="settings">Optional settings that control how the comparison is performed. Can be null, in which case the images are required to be exactly identical.</param>
        public static void AreEqual(Texture2D expected, Texture2D actual, ImageComparisonSettings settings = null)
        {
            if (actual == null)
                throw new ArgumentNullException("actual");

#if UNITY_EDITOR
            var imagesWritten = new HashSet<string>();
            var dirName = Path.Combine("Assets/ActualImages", string.Format("{0}/{1}/{2}", UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice));
            Directory.CreateDirectory(dirName);
#endif

            try
            {
                Assert.That(expected, Is.Not.Null, "No reference image was provided.");

                Assert.That(actual.width, Is.EqualTo(expected.width),
                    "The expected image had width {0}px, but the actual image had width {1}px.", expected.width,
                    actual.width);
                Assert.That(actual.height, Is.EqualTo(expected.height),
                    "The expected image had height {0}px, but the actual image had height {1}px.", expected.height,
                    actual.height);

                Assert.That(actual.format, Is.EqualTo(expected.format),
                    "The expected image had format {0} but the actual image had format {1}.", expected.format,
                    actual.format);

                using (var expectedPixels = new NativeArray<Color32>(expected.GetPixels32(0), Allocator.TempJob))
                using (var actualPixels = new NativeArray<Color32>(actual.GetPixels32(0), Allocator.TempJob))
                using (var diffPixels = new NativeArray<Color32>(expectedPixels.Length, Allocator.TempJob))
                using (var sumOverThreshold = new NativeArray<float>(Mathf.CeilToInt(expectedPixels.Length / (float)k_BatchSize), Allocator.TempJob))
                {
                    if (settings == null)
                        settings = new ImageComparisonSettings();

                    new ComputeDiffJob
                    {
                        expected = expectedPixels,
                        actual = actualPixels,
                        diff = diffPixels,
                        sumOverThreshold = sumOverThreshold,
                        pixelThreshold = settings.PerPixelCorrectnessThreshold
                    }.Schedule(expectedPixels.Length, k_BatchSize).Complete();

                    float averageDeltaE = sumOverThreshold.Sum() / (expected.width * expected.height);

                    try
                    {
                        Assert.That(averageDeltaE, Is.LessThanOrEqualTo(settings.AverageCorrectnessThreshold));
                    }
                    catch (AssertionException)
                    {
                        var diffImage = new Texture2D(expected.width, expected.height, TextureFormat.RGB24, false);
                        var diffPixelsArray = new Color32[expected.width * expected.height];
                        diffPixels.CopyTo(diffPixelsArray);
                        diffImage.SetPixels32(diffPixelsArray, 0);
                        diffImage.Apply(false);

#if UNITY_EDITOR
                        if (sDontWriteToLog)
                        {
                            var bytes = diffImage.EncodeToPNG();
                            var path = Path.Combine(dirName, TestContext.CurrentContext.Test.Name + ".diff.png");
                            File.WriteAllBytes(path, bytes);
                            imagesWritten.Add(path);
                        }
                        else
#endif
                        TestContext.CurrentContext.Test.Properties.Set("DiffImage", Convert.ToBase64String(diffImage.EncodeToPNG()) );

                        throw;
                    }
                }
            }
            catch (AssertionException)
            {
#if UNITY_EDITOR
                if (sDontWriteToLog)
                {
                    var bytes = actual.EncodeToPNG();
                    var path = Path.Combine(dirName, TestContext.CurrentContext.Test.Name + ".png");
                    File.WriteAllBytes(path, bytes);
                    imagesWritten.Add(path);

                    AssetDatabase.Refresh();

                    UnityEditor.TestTools.Graphics.Utils.SetupReferenceImageImportSettings(imagesWritten);
                }
                else
#endif
                    TestContext.CurrentContext.Test.Properties.Set("Image", Convert.ToBase64String(actual.EncodeToPNG()));

                throw;
            }
        }

        struct ComputeDiffJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> expected;
            [ReadOnly] public NativeArray<Color32> actual;
            public NativeArray<Color32> diff;
            public float pixelThreshold;

            [NativeDisableParallelForRestriction]
            public NativeArray<float> sumOverThreshold;

            public void Execute(int index)
            {
                var exp = RGBtoJAB(expected[index]);
                var act = RGBtoJAB(actual[index]);

                float deltaE = JABDeltaE(exp, act);
                float overThreshold = Mathf.Max(0f, deltaE - pixelThreshold);
                int batch = index / k_BatchSize;
                sumOverThreshold[batch] = sumOverThreshold[batch] + overThreshold;

                // deltaE is linear, convert it to sRGB for easier debugging
                deltaE = Mathf.LinearToGammaSpace(deltaE);
                var colorResult = new Color(deltaE, deltaE, deltaE, 1f);
                diff[index] = colorResult;
            }
        }

        // Linear RGB to XYZ using D65 ref. white
        static Vector3 RGBtoXYZ(Color color)
        {
            float x = color.r * 0.4124564f + color.g * 0.3575761f + color.b * 0.1804375f;
            float y = color.r * 0.2126729f + color.g * 0.7151522f + color.b * 0.0721750f;
            float z = color.r * 0.0193339f + color.g * 0.1191920f + color.b * 0.9503041f;
            return new Vector3(x * 100f, y * 100f, z * 100f);
        }

        // sRGB to JzAzBz
        // https://www.osapublishing.org/oe/fulltext.cfm?uri=oe-25-13-15131&id=368272
        static Vector3 RGBtoJAB(Color color)
        {
            var xyz = RGBtoXYZ(color.linear);

            const float kB  = 1.15f;
            const float kG  = 0.66f;
            const float kC1 = 0.8359375f;        // 3424 / 2^12
            const float kC2 = 18.8515625f;       // 2413 / 2^7
            const float kC3 = 18.6875f;          // 2392 / 2^7
            const float kN  = 0.15930175781f;    // 2610 / 2^14
            const float kP  = 134.034375f;       // 1.7 * 2523 / 2^5
            const float kD  = -0.56f;
            const float kD0 = 1.6295499532821566E-11f;

            float x2 = kB * xyz.x - (kB - 1f) * xyz.z;
            float y2 = kG * xyz.y - (kG - 1f) * xyz.x;

            float l = 0.41478372f * x2 + 0.579999f * y2 + 0.0146480f * xyz.z;
            float m = -0.2015100f * x2 + 1.120649f * y2 + 0.0531008f * xyz.z;
            float s = -0.0166008f * x2 + 0.264800f * y2 + 0.6684799f * xyz.z;
            l = Mathf.Pow(l / 10000f, kN);
            m = Mathf.Pow(m / 10000f, kN);
            s = Mathf.Pow(s / 10000f, kN);

            // Can we switch to unity.mathematics yet?
            var lms = new Vector3(l, m, s);
            var a = new Vector3(kC1, kC1, kC1) + kC2 * lms;
            var b = Vector3.one + kC3 * lms;
            var tmp = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);

            lms.x = Mathf.Pow(tmp.x, kP);
            lms.y = Mathf.Pow(tmp.y, kP);
            lms.z = Mathf.Pow(tmp.z, kP);

            var jab = new Vector3(
                0.5f * lms.x + 0.5f * lms.y,
                3.524000f * lms.x + -4.066708f * lms.y + 0.542708f * lms.z,
                0.199076f * lms.x + 1.096799f * lms.y + -1.295875f * lms.z
            );

            jab.x = ((1f + kD) * jab.x) / (1f + kD * jab.x) - kD0;

            return jab;
        }

        static float JABDeltaE(Vector3 v1, Vector3 v2)
        {
            float c1 = Mathf.Sqrt(v1.y * v1.y + v1.z * v1.z);
            float c2 = Mathf.Sqrt(v2.y * v2.y + v2.z * v2.z);

            float h1 = Mathf.Atan(v1.z / v1.y);
            float h2 = Mathf.Atan(v2.z / v2.y);

            float deltaH = 2f * Mathf.Sqrt(c1 * c2) * Mathf.Sin((h1 - h2) / 2f);
            float deltaE = Mathf.Sqrt(Mathf.Pow(v1.x - v2.x, 2f) + Mathf.Pow(c1 - c2, 2f) + deltaH * deltaH);
            return deltaE;
        }

#if UNITY_EDITOR
        // Hack do disable writing to the XML Log of TestRunner (to avoid editor hanging when tests are run locally)
        static string s_DontWriteToLogPath = "Library/DontWriteToLog";

        static bool sDontWriteToLog
        {
            get
            {
                return File.Exists( s_DontWriteToLogPath ) ;
            }
        }

        [MenuItem("Tests/XML Logging/Disable")]
        public static void DisableXMLLogging()
        {
            File.WriteAllText( s_DontWriteToLogPath, "" );
        }

        [MenuItem("Tests/XML Logging/Enable")]
        public static void EnableXMLLogging()
        {
            File.Delete(s_DontWriteToLogPath);
        }

#endif

    }
}
