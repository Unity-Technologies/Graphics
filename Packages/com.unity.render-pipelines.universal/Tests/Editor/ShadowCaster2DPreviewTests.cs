using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// Generates the ShadowCaster2D master preview and checks it is valid ShaderLab.
    ///
    /// This exists because of a bug that shipped: all five passes were authored with
    /// <c>useInPreview = false</c>, and the generator skips every such pass when building the preview,
    /// so the SubShader came out with no <c>Pass</c> at all. ShaderLab rejects an empty SubShader with
    /// "syntax error, unexpected '}'" on its closing brace, which is a confusing thing to read.
    ///
    /// None of the 61 descriptor assertions saw it. They inspect the PassDescriptors, which were all
    /// present and correct -- the passes were removed later, by a filter those tests never model. Only
    /// generating the source catches this class of defect, which is the same reason
    /// EveryFixtureShaderInThisFolderCompiles exists for the real shader.
    ///
    /// Worth recording the wrong turn as well: the master preview is generated from the graph target's
    /// own SubShader, NOT from PreviewTarget. PreviewTarget serves node previews. An earlier analysis
    /// concluded the opposite and inferred that preview render state and passes could be ignored.
    /// </summary>
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class ShadowCaster2DPreviewTests
    {
        // Built exactly as NewGraphAction does, so this is the graph the Create menu produces.
        static GraphData NewShadowCaster2DGraph()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalShadowCaster2DSubTarget)),
                "UniversalShadowCaster2DSubTarget is not a registered SubTarget of UniversalTarget.");

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(
                new Target[] { target },
                new[]
                {
                    BlockFields.VertexDescription.Position,
                    UniversalBlockFields.VertexDescription.ShadowDisplacement,
                UniversalBlockFields.VertexDescription.ShadowSideSoftness,
                UniversalBlockFields.VertexDescription.ShadowBackSoftness,
                    BlockFields.SurfaceDescription.Alpha,
                });
            graph.AddCategory(CategoryData.DefaultCategory());
            graph.ValidateGraph();
            return graph;
        }

        static string GeneratePreviewSource()
        {
            var graph = NewShadowCaster2DGraph();
            return new Generator(graph, graph.outputNode, GenerationMode.Preview, "Master")
                .generatedShader;
        }

        static string GenerateRealSource()
        {
            var graph = NewShadowCaster2DGraph();
            return new Generator(graph, null, GenerationMode.ForReals, "ShadowCaster2D")
                .generatedShader;
        }

        [Test]
        public void MasterPreviewSubShaderContainsAPass()
        {
            string source = GeneratePreviewSource();

            int subShader = source.IndexOf("SubShader", StringComparison.Ordinal);
            Assert.Greater(subShader, -1, "No SubShader in the generated preview:\n" + source);

            StringAssert.Contains("Pass", source.Substring(subShader),
                "The master preview SubShader contains no Pass, which is invalid ShaderLab. Every pass " +
                "was probably authored with useInPreview = false. Generated source:\n" + source);
        }

        // The real shader must declare all five, whatever the preview does with its subset.
        [TestCase("Self")]
        [TestCase("UnshadowMark")]
        [TestCase("UnshadowUnmark")]
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void RealShaderDeclaresPass(string passName)
        {
            StringAssert.Contains($"Name \"{passName}\"", GenerateRealSource());
        }

        // A projected pass in the preview would decode the 14-float Unity.SoftShadow payload out of a
        // stock preview mesh that carries no such vertex data.
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void MasterPreviewExcludesProjectedPasses(string passName)
        {
            StringAssert.DoesNotContain($"Name \"{passName}\"", GeneratePreviewSource());
        }

        // The Shadow Distance port must reach the generated projection UNCONDITIONALLY.
        //
        // It once did not: the graph value was used only when the port was CONNECTED, gated on
        // TargetFieldContext.connectedBlocks, with a _ShadowLength material property as the fallback.
        // connectedBlocks is IsSlotConnected, and an inline value typed into the port is not a
        // connection -- so typing a value into Shadow Displacement, the commonest way to author a port,
        // did nothing whatsoever. Nothing caught it: the block was registered, the descriptors were
        // right, and the shader compiled.
        [Test]
        public void ShadowDisplacementPortReachesTheProjection()
        {
            string source = GenerateRealSource();

            StringAssert.Contains("ShadowDisplacement", source,
                "The Shadow Displacement port does not appear in the generated shader at all.");

            // A fallback uniform would mean the port can be bypassed, which is how it came to be inert.
            // _ShadowLength is the built-in path's own uniform and has no business in a generated
            // shader at all: naming it anywhere here means the graph value is not what drives the throw.
            StringAssert.DoesNotContain("_ShadowLength", source,
                "The projection still reaches for the _ShadowLength uniform, so the port can be " +
                "silently ignored.");
        }

        // The vertex graph has to be emitted, or BuildVertexDescription does not exist and the five
        // per-point evaluations cannot compile. UniversalTarget only sets Fields.GraphVertex from
        // Position / Normal / Tangent, none of which this SubTarget registers.
        [Test]
        public void VertexGraphIsEmitted()
        {
            StringAssert.Contains("FEATURES_GRAPH_VERTEX", GenerateRealSource(),
                "Fields.GraphVertex was not set, so the vertex graph is absent.");
        }

        /// <summary>Returns the generated source between this pass's Name and the next pass's.</summary>
        static string PassBody(string source, string passName)
        {
            int start = source.IndexOf($"Name \"{passName}\"", StringComparison.Ordinal);
            Assert.Greater(start, -1, $"Pass \"{passName}\" is not in the generated shader.");

            int next = source.IndexOf("Name \"", start + 6, StringComparison.Ordinal);
            return next < 0 ? source.Substring(start) : source.Substring(start, next - start);
        }

        // Wherever FEATURES_GRAPH_VERTEX is defined, VertexDescription MUST carry a Position member.
        //
        // Varyings.hlsl does `input.positionOS = vertexDescription.Position;` with no guard of its own --
        // Normal and Tangent have FEATURES_GRAPH_VERTEX_*_OUTPUT guards, Position does not. And HLSL
        // type-checks BuildVaryings' body in every pass that includes Varyings.hlsl, whether or not that
        // pass calls it, so this is not something the projected passes escape by taking their position
        // from SoftShadowProject instead.
        //
        // Scoping the field to the projected passes was tried and does not work, for exactly that
        // reason: the failure is at definition, not at the call site. Registering Position is the only
        // fix, which is why it is not an optional port.
        [TestCase("Self")]
        [TestCase("UnshadowMark")]
        [TestCase("UnshadowUnmark")]
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void EveryPassWithAVertexGraphSuppliesPosition(string passName)
        {
            string source = GenerateRealSource();
            string body = PassBody(source, passName);

            if (!body.Contains("#define FEATURES_GRAPH_VERTEX"))
                Assert.Pass($"{passName} has no vertex graph, so Position is not required.");

            StringAssert.Contains("float3 Position", body,
                $"{passName} defines FEATURES_GRAPH_VERTEX but its VertexDescription has no Position " +
                "member, so Varyings.hlsl fails to compile with \"invalid subscript 'Position'\".");
        }

        [Test, Explicit("Diagnostic only; writes the generated sources to temp for reading.")]
        public void DumpGeneratedSources()
        {
            string dir = Path.Combine(Path.GetTempPath(), "shadowcaster2d-diag");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "real.shader"), GenerateRealSource());
            File.WriteAllText(Path.Combine(dir, "preview.shader"), GeneratePreviewSource());
            Debug.Log($"[shadowcaster2d] wrote generated sources to {dir}");
        }
    }
}
