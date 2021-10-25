using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class NodeTests : ShaderGraphTestRenderer
    {
        [UnityTest]
        public IEnumerator NodeTestTest()
        {
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/NodeTestTest.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();
            var colorStrings = new string[] { "_COLOR_RED", "_COLOR_GREEN", "_COLOR_BLUE" };
            var colors = new Color32[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255) };
            for (int i = 0; i < 3; i++)
            {
                RunNodeTest(graph, $"NodeTestTest_{colorStrings[i]}",
                    expectedColor: colors[i],
                    setupMaterial: m => m.EnableKeyword(colorStrings[i]));
            }
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator TransformV1MatchesOldTransform()
        {
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/TransformV1MatchesOldTransform.shadergraph";
            var graph = LoadGraph(graphPath);

            // first check that it renders red in the initial state, to check that the test works
            // (graph is initially set up with non-matching transforms)
            ResetTestReporting();
            RunNodeTest(graph, $"TransformV1_default", expectedIncorrectPixels: defaultResolution * defaultResolution);
            ReportTests();

            // now check all possible settings
            ResetTestReporting();
            var xform = graph.GetNodes<TransformNode>().First();
            var old = graph.GetNodes<OldTransformNode>().First();

            var oldConversionTypes = new ConversionType[] { ConversionType.Position, ConversionType.Direction };
            foreach (ConversionType conversionType in oldConversionTypes)
            {
                foreach (CoordinateSpace source in Enum.GetValues(typeof(CoordinateSpace)))
                {
                    foreach (CoordinateSpace dest in Enum.GetValues(typeof(CoordinateSpace)))
                    {
                        // setup transform(v1) node
                        xform.conversion = new CoordinateSpaceConversion(source, dest);
                        xform.conversionType = conversionType;
                        xform.normalize = false;

                        // setup old transform node
                        old.conversion = new CoordinateSpaceConversion(source, dest);
                        old.conversionType = conversionType;

                        RunNodeTest(graph, $"TransformNodeOld_{source}_to_{dest}_{conversionType}");
                    }

                    // have to yield to let a frame pass or it will break
                    // (unity only releases some resources at the end of the frame)
                    yield return null;
                }
            }
            ReportTests();
        }

        [UnityTest]
        public IEnumerator TransformInverses()
        {
            // Test that A->B and B->A result in the original value
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/TransformInverses.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();

            // check all possible settings
            var xforms = graph.GetNodes<TransformNode>();
            var xform = xforms.First();
            var inv = xforms.Skip(1).First();
            foreach (ConversionType conversionType in Enum.GetValues(typeof(ConversionType)))
            {
                foreach (CoordinateSpace source in Enum.GetValues(typeof(CoordinateSpace)))
                {
                    foreach (CoordinateSpace dest in Enum.GetValues(typeof(CoordinateSpace)))
                    {
                        // setup transform node
                        xform.conversion = new CoordinateSpaceConversion(source, dest);
                        xform.conversionType = conversionType;
                        xform.normalize = false;

                        // setup inverse transform node
                        inv.conversion = new CoordinateSpaceConversion(dest, source);
                        inv.conversionType = conversionType;
                        inv.normalize = false;

                        RunNodeTest(graph, $"TransformInverse_{source}_to_{dest}_{conversionType}", errorThreshold: 1);
                    }

                    // have to yield to let a frame pass or it will break
                    // (unity only releases some resources at the end of the frame)
                    yield return null;
                }
            }
            ReportTests();
        }

        [UnityTest]
        public IEnumerator TransformABC()
        {
            // Test that transforming from A->B then B->C is the same as A->C (for all A,B,C)
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/TransformABC.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();

            var xforms = graph.GetNodes<TransformNode>();
            var A_to_C = xforms.FirstOrDefault(n => (n.conversion.from == CoordinateSpace.Object) && (n.conversion.to == CoordinateSpace.Tangent));
            var A_to_B = xforms.FirstOrDefault(n => (n.conversion.from == CoordinateSpace.Object) && (n.conversion.to == CoordinateSpace.View));
            var B_to_C = xforms.FirstOrDefault(n => (n.conversion.from == CoordinateSpace.View) && (n.conversion.to == CoordinateSpace.Tangent));

            // check all possible settings
            foreach (ConversionType conversionType in Enum.GetValues(typeof(ConversionType)))
            {
                foreach (CoordinateSpace A in Enum.GetValues(typeof(CoordinateSpace)))
                {
                    foreach (CoordinateSpace B in Enum.GetValues(typeof(CoordinateSpace)))
                    {
                        foreach (CoordinateSpace C in Enum.GetValues(typeof(CoordinateSpace)))
                        {
                            // setup transforms
                            A_to_C.conversion = new CoordinateSpaceConversion(A, C);
                            A_to_C.conversionType = conversionType;
                            A_to_C.normalize = false;

                            A_to_B.conversion = new CoordinateSpaceConversion(A, B);
                            A_to_B.conversionType = conversionType;
                            A_to_B.normalize = false;

                            B_to_C.conversion = new CoordinateSpaceConversion(B, C);
                            B_to_C.conversionType = conversionType;
                            B_to_C.normalize = false;

                            RunNodeTest(graph, $"TransformABC_{A}_{B}_{C}_{conversionType}", errorThreshold: 1);
                        }

                        // have to yield to let a frame pass or it will break
                        // (unity only releases some resources at the end of the frame)
                        yield return null;
                    }
                }
            }
            ReportTests();
        }

        [UnityTest]
        public IEnumerator TransformNormalize()
        {
            // Test that A->B then normalizing is the same as A->B with normalize enabled
            // for all direction and normal conversion types
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/TransformNormalize.shadergraph";
            var graph = LoadGraph(graphPath);

            // now check all possible settings
            ResetTestReporting();
            var xforms = graph.GetNodes<TransformNode>();
            var norm = xforms.FirstOrDefault(n => n.normalize);
            var unnorm = xforms.FirstOrDefault(n => !n.normalize);

            var normalizeConversionTypes = new ConversionType[] { ConversionType.Direction, ConversionType.Normal };
            foreach (ConversionType conversionType in normalizeConversionTypes)
            {
                foreach (CoordinateSpace source in Enum.GetValues(typeof(CoordinateSpace)))
                {
                    foreach (CoordinateSpace dest in Enum.GetValues(typeof(CoordinateSpace)))
                    {
                        // setup normalized transform
                        norm.conversion = new CoordinateSpaceConversion(source, dest);
                        norm.conversionType = conversionType;

                        // setup unnormalized transform
                        unnorm.conversion = new CoordinateSpaceConversion(source, dest);
                        unnorm.conversionType = conversionType;

                        RunNodeTest(graph, $"TransformNormalize_{source}_to_{dest}_{conversionType}");
                    }

                    // have to yield to let a frame pass or it will break
                    // (unity only releases some resources at the end of the frame)
                    yield return null;
                }
            }
            ReportTests();
        }

        [UnityTest]
        public IEnumerator BoolConversion()
        {
            // Test that converting from bool => float gives the correct result
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/BoolConversion.shadergraph";
            var graph = LoadGraph(graphPath);

            var boolNode = graph.GetNodes<BooleanNode>().First();

            ResetTestReporting();

            boolNode.m_Value = false;
            RunNodeTest(graph, $"BoolConversion_false",
                setupMaterial: (mat) =>
                {
                    mat.SetFloat("_FloatVal", 0.0f);
                });

            boolNode.m_Value = true;
            RunNodeTest(graph, $"BoolConversion_true",
                setupMaterial: (mat) =>
                {
                    mat.SetFloat("_FloatVal", 1.0f);
                });

            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator BoolConversionV2()
        {
            // Test that converting from bool => float2 gives the correct result
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/BoolConversionV2.shadergraph";
            var graph = LoadGraph(graphPath);

            var boolNode = graph.GetNodes<BooleanNode>().First();

            ResetTestReporting();

            boolNode.m_Value = false;
            RunNodeTest(graph, $"BoolConversion_false",
                setupMaterial: (mat) =>
                {
                    mat.SetVector("_V2Val", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                });

            boolNode.m_Value = true;
            RunNodeTest(graph, $"BoolConversion_true",
                setupMaterial: (mat) =>
                {
                    mat.SetVector("_V2Val", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                });

            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator BoolConversionV3()
        {
            // Test that converting from bool => float3 gives the correct result
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/BoolConversionV3.shadergraph";
            var graph = LoadGraph(graphPath);

            var boolNode = graph.GetNodes<BooleanNode>().First();

            ResetTestReporting();

            boolNode.m_Value = false;
            RunNodeTest(graph, $"BoolConversion_false",
                setupMaterial: (mat) =>
                {
                    mat.SetVector("_V3Val", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                });

            boolNode.m_Value = true;
            RunNodeTest(graph, $"BoolConversion_true",
                setupMaterial: (mat) =>
                {
                    mat.SetVector("_V3Val", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                });

            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator BoolConversionV4()
        {
            // Test that converting from bool => float4 gives the correct result
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/BoolConversionV4.shadergraph";
            var graph = LoadGraph(graphPath);

            var boolNode = graph.GetNodes<BooleanNode>().First();

            ResetTestReporting();

            boolNode.m_Value = false;
            RunNodeTest(graph, $"BoolConversion_false",
                setupMaterial: (mat) =>
                {
                    mat.SetVector("_V4Val", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                });

            boolNode.m_Value = true;
            RunNodeTest(graph, $"BoolConversion_true",
                setupMaterial: (mat) =>
                {
                    mat.SetVector("_V4Val", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
                });

            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator ScreenPositionDefault()
        {
            // Test that default screen space acts as expected relative to raw screen space
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/ScreenPositionDefault.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();
            RunNodeTest(graph, "ScreenPositionDefault");
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator ScreenPositionVertex()
        {
            // Test that default screen space acts as expected relative to raw screen space
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/ScreenPositionVertex.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();
            RunNodeTest(graph, "ScreenPositionVertex");
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator PixelPosition()
        {
            // Test that pixel position acts as expected relative to raw screen space
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/PixelPosition.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();
            RunNodeTest(graph, "PixelPosition");
            ReportTests();
            yield break;
        }

        [UnityTest]
        public IEnumerator PixelPositionVertex()
        {
            // Test that pixel position acts as expected relative to raw screen space
            string graphPath = "Assets/CommonAssets/Graphs/NodeTests/PixelPositionVertex.shadergraph";
            var graph = LoadGraph(graphPath);
            ResetTestReporting();
            RunNodeTest(graph, "PixelPositionVertex");
            ReportTests();
            yield break;
        }
    }
}
