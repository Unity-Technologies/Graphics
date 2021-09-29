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
    class NodeTests
    {
        const int res = 128;

        readonly Vector3 testPosition = new Vector3(0.24699998f, 0.51900005f, 0.328999996f);
        readonly Quaternion testRotation = new Quaternion(-0.164710045f, -0.0826543793f, -0.220811233f, 0.957748055f);

        [UnityTest]
        public IEnumerator TransformV1MatchesOldTransform()
        {
            string kGraphName = "Assets/CommonAssets/Graphs/TransformGraph.shadergraph";

            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out var graph);
            Assert.NotNull(graph, $"Invalid graph data found for {kGraphName}");
            graph.OnEnable();
            graph.ValidateGraph();

            var renderer = new ShaderGraphTestRenderer();

            // first check that it renders red in the initial state, to check that the test works
            // (graph is initially set up with non-matching transforms)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor(res, res, GraphicsFormat.R8G8B8A8_SRGB, depthBufferBits: 32);
                var target = RenderTexture.GetTemporary(descriptor);

                // use a non-standard transform, so that view, object, etc. transforms are non trivial
                renderer.RenderQuadPreview(graph, target, testPosition, testRotation, useSRP: true);

                int incorrectPixels = ShaderGraphTestRenderer.CountPixelsNotEqual(target, new Color32(0, 255, 0, 255), false);
                //Debug.Log($"Initial state: {target.width}x{target.height} Failing pixels: {incorrectPixels}");

                if (incorrectPixels != res * res)
                    ShaderGraphTestRenderer.SaveToPNG(target, "test-results/NodeTests/TransformNodeOld_default.png");

                Assert.AreEqual(res * res, incorrectPixels, $"Initial state should have {res * res} failing pixels");

                RenderTexture.ReleaseTemporary(target);
                yield return null;
            }

            var xform = graph.GetNodes<TransformNode>().First();
            var old = graph.GetNodes<OldTransformNode>().First();

            // now check all possible settings
            string assertString = null;
            int assertIncorrectPixels = 0;

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

                        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(res, res, GraphicsFormat.R8G8B8A8_SRGB, depthBufferBits: 32);
                        var target = RenderTexture.GetTemporary(descriptor);

                        // use a non-standard transform, so that view, object, etc. transforms are non trivial
                        renderer.RenderQuadPreview(graph, target, testPosition, testRotation, useSRP: true);

                        int incorrectPixels = ShaderGraphTestRenderer.CountPixelsNotEqual(target, new Color32(0, 255, 0, 255), false);

                        // test failing some tests
                        if (UnityEngine.Random.value < 0.1f)
                            incorrectPixels = 42;
                        //Debug.Log($"{source} to {dest} ({conversionType}: {target.width}x{target.height} Failing pixels: {incorrectPixels}");

                        if (incorrectPixels != 0)
                        {
                            assertString = $"{incorrectPixels} incorrect pixels detected: {source} to {dest} ({conversionType})";
                            assertIncorrectPixels = incorrectPixels;

                            ShaderGraphTestRenderer.SaveToPNG(target, $"test-results/NodeTests/TransformNodeOld_{source}_to_{dest}_{conversionType}.diff.png", reportArtifact: true);

                            renderer.RenderQuadPreview(graph, target, testPosition, testRotation, useSRP: true, ShaderGraphTestRenderer.Mode.EXPECTED);
                            ShaderGraphTestRenderer.SaveToPNG(target, $"test-results/NodeTests/TransformNodeOld_{source}_to_{dest}_{conversionType}.expected.png", reportArtifact: true);

                            renderer.RenderQuadPreview(graph, target, testPosition, testRotation, useSRP: true, ShaderGraphTestRenderer.Mode.ACTUAL);
                            ShaderGraphTestRenderer.SaveToPNG(target, $"test-results/NodeTests/TransformNodeOld_{source}_to_{dest}_{conversionType}.png", reportArtifact: true);
                        }

                        RenderTexture.ReleaseTemporary(target);
                    }

                    // have to yield to let a frame pass
                    // unity only releases some resources at the end of a frame
                    // and if you do too many renders in a frame it will run out
                    yield return null;
                }
            }

            // we assert at the end of the test, so we always produce all of the test results before asserting
            Assert.AreEqual(0, assertIncorrectPixels, assertString);
        }
    }
}
