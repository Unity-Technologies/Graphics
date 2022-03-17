using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class TestGraphicsTests : FoundryTestRenderer
    {
        [UnityTest]
        public IEnumerator ImageComparisonWorksOnConstantShader()
        {
            string shaderPath = "Assets/TestAssets/TestGraphicsTests/ConstantRGB.shader";
            var shader = (Shader)AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Shader));
            if (shader == null)
            {
                Debug.LogError("Cannot load shader " + shaderPath);
                yield break;
            }

            ResetTestReporting();
            var colorStrings = new string[] { "_COLOR_RED", "_COLOR_GREEN", "_COLOR_BLUE" };
            var colors = new Color32[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255) };
            for (int i = 0; i < 3; i++)
            {
                TestShaderIsConstantColor(shader,
                    $"ImageComparisonWorksOnConstantShader_{colorStrings[i]}",
                    expectedColor: colors[i],
                    setupMaterial: m => m.EnableKeyword(colorStrings[i]));
            }
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator ImageComparisonReportsIncorrectPixels()
        {
            string shaderPath = "Assets/TestAssets/TestGraphicsTests/ConstantRGB.shader";
            var shader = (Shader)AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Shader));

            if (shader == null)
            {
                Debug.LogError("Cannot load shader " + shaderPath);
                yield break;
            }

            ResetTestReporting();
            var colorStrings = new string[] { "_COLOR_RED", "_COLOR_GREEN", "_COLOR_BLUE" };
            // error of 7 on each color
            var colors = new Color32[] { new Color32(255, 7, 0, 255), new Color32(0, 255, 7, 255), new Color32(0, 7, 255, 255) };
            for (int i = 0; i < 3; i++)
            {
                TestShaderIsConstantColor(shader,
                    $"ImageComparisonReportsIncorrectPixels_{colorStrings[i]}",
                    expectedColor: colors[i],
                    expectedIncorrectPixels: defaultResolution * defaultResolution,
                    errorThreshold: 6,  // threshold is below 7, all pixels should be detected as incorrect
                    setupMaterial: m => m.EnableKeyword(colorStrings[i]));
            }
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator ImageComparisonThresholdIgnoresSmallerDifferences()
        {
            string shaderPath = "Assets/TestAssets/TestGraphicsTests/ConstantRGB.shader";
            var shader = (Shader)AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Shader));

            if (shader == null)
            {
                Debug.LogError("Cannot load shader " + shaderPath);
                yield break;
            }

            ResetTestReporting();
            var colorStrings = new string[] { "_COLOR_RED", "_COLOR_GREEN", "_COLOR_BLUE" };
            // error of 7 on each color
            var colors = new Color32[] { new Color32(255, 7, 0, 255), new Color32(0, 255, 7, 255), new Color32(0, 7, 255, 255) };
            for (int i = 0; i < 3; i++)
            {
                TestShaderIsConstantColor(shader,
                    $"ImageComparisonThresholdIgnoresSmallerDifferences_{colorStrings[i]}",
                    expectedColor: colors[i],
                    expectedIncorrectPixels: 0,
                    errorThreshold: 8,  // error within threshold, no pixels should be detected as incorrect
                    setupMaterial: m => m.EnableKeyword(colorStrings[i]),
                    reportArtifacts: false);
            }
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator ShaderAddTest()
        {
            string shaderPath = "Assets/TestAssets/TestGraphicsTests/AddTest.shader";
            var shader = (Shader)AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Shader));

            if (shader == null)
            {
                Debug.LogError("Cannot load shader " + shaderPath);
                yield break;
            }

            ResetTestReporting();
            float[] values = { 0.0f, 0.1f, 0.2f, 0.35f, 0.6f };
            foreach (var a in values)
            {
                foreach (var b in values)
                {
                    float expected = a + b;

                    // test (a + b == expected) on the GPU
                    TestShaderDiffIsGreen(shader,
                        $"AddTest_{a}_{b}",
                        setupMaterial: m =>
                        {
                            m.SetVector("_Expected", new Vector4(expected, expected, expected, expected));
                            m.SetVector("_A", new Vector4(a, a, a, a));
                            m.SetVector("_B", new Vector4(b, b, b, b));
                        });

                    // test (a + b) != (expected + epsilon) on the GPU, should return red
                    TestShaderIsConstantColor(shader,
                        $"AddTestIncorrect_{a}_{b}",
                        expectedColor: new Color32(255, 0, 0, 255), // red (mismatch)
                        setupMaterial: m =>
                        {
                            float e = expected + 0.001f;
                            m.SetVector("_Expected", new Vector4(e, e, e, e));
                            m.SetVector("_A", new Vector4(a, a, a, a));
                            m.SetVector("_B", new Vector4(b, b, b, b));
                        });
                }
            }
            ReportTests();
            yield break;
        }
    }
}
