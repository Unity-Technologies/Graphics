using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class UniversalTerrainLitCodeGenTests
    {
        internal enum VertexContext
        {
            WithPositionBlock,
            Empty,
        }

        // TerrainLit emits the primary shader plus the hidden _AddPass and _BaseMapGen shaders, so
        // assertions have to cover all of them, not just Generator.generatedShader.
        static List<(string name, string code)> GenerateShaders(VertexContext vertexContext, GenerationMode mode)
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalTerrainLitSubTarget)),
                "TerrainLit subtarget is not registered on UniversalTarget");

            var blocks = vertexContext == VertexContext.WithPositionBlock
                ? new[] { BlockFields.VertexDescription.Position, BlockFields.SurfaceDescription.BaseColor }
                : new[] { BlockFields.SurfaceDescription.BaseColor };

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { target }, blocks);

            // outputNode stays null for a non-subgraph. A non-null one would make Generator swap in
            // PreviewTarget and drop the terrain subtarget entirely.
            var shaders = new Generator(graph, graph.outputNode, mode, "TerrainLitCodeGen").allGeneratedShaders
                .Select(shader => (shader.shaderName, shader.codeString))
                .ToList();

            Assert.IsNotEmpty(shaders, "TerrainLit generated no shaders");

            // The template preprocessor treats '$' as a command marker even inside a comment, and on a
            // malformed directive it splices a marker into the shader instead of failing. Without this
            // check a broken template would leave the assertions below passing on text that is wrong.
            // Every marker is a comment line carrying a caret under the offending column. Matching the
            // caret rather than the word ERROR also catches Expect(), which reports "^ Expected ')'".
            foreach (var (name, code) in shaders)
                StringAssert.DoesNotMatch(@"(?m)^\s*//\s*\^ ", code, $"template preprocessor error in {name}");

            return shaders;
        }

        // Asserting mere presence would be satisfied by a single pass out of fifteen, so a new terrain
        // pass whose template forgot the include would slip through. Every terrain pass emits exactly one
        // UNIVERSAL_TERRAIN_ENABLED define and should carry exactly one copy of the function.
        static void AssertDefinedInEveryTerrainPass(string function, VertexContext vertexContext, GenerationMode mode)
        {
            foreach (var (name, code) in GenerateTerrainShaders(vertexContext, mode))
            {
                var passes = Regex.Matches(code, @"#define UNIVERSAL_TERRAIN_ENABLED 1").Count;
                var definitions = Regex.Matches(code, Regex.Escape(function) + @"\(").Count;
                Assert.AreEqual(passes, definitions, $"{function} in {name}");
            }
        }

        // Preview generation emits the _AddPass shader with no passes at all, and a shader with no
        // terrain pass has nothing to define. Key off the define the terrain passes themselves add.
        static List<(string name, string code)> GenerateTerrainShaders(VertexContext vertexContext, GenerationMode mode)
        {
            var shaders = GenerateShaders(vertexContext, mode)
                .Where(shader => shader.code.Contains("UNIVERSAL_TERRAIN_ENABLED"))
                .ToList();

            Assert.IsNotEmpty(shaders, "no generated shader turns on the terrain path");
            return shaders;
        }

        // Varyings.hlsl calls ApplyTerrainInstancing outside of any FEATURES_GRAPH_VERTEX guard, so the
        // terrain passes have to emit it even when the graph's Vertex context is empty. That state is one
        // Delete away: TerrainLit's only active vertex block is Position, it is unconnected by default, and
        // removing it does not trigger the auto re-add. Gating the definition on graphVertex breaks every pass.
        [TestCase(VertexContext.WithPositionBlock, GenerationMode.ForReals)]
        [TestCase(VertexContext.Empty, GenerationMode.ForReals)]
        [TestCase(VertexContext.WithPositionBlock, GenerationMode.Preview)]
        [TestCase(VertexContext.Empty, GenerationMode.Preview)]
        public void TerrainShadersDefineApplyTerrainInstancing(VertexContext vertexContext, GenerationMode mode)
        {
            AssertDefinedInEveryTerrainPass("ApplyTerrainInstancing", vertexContext, mode);
        }

        // Same for ConstructTerrainTangent, which the forward and gbuffer passes call under
        // ENABLE_TERRAIN_PERPIXEL_NORMAL.
        [TestCase(VertexContext.WithPositionBlock, GenerationMode.ForReals)]
        [TestCase(VertexContext.Empty, GenerationMode.ForReals)]
        [TestCase(VertexContext.WithPositionBlock, GenerationMode.Preview)]
        [TestCase(VertexContext.Empty, GenerationMode.Preview)]
        public void TerrainShadersDefineConstructTerrainTangent(VertexContext vertexContext, GenerationMode mode)
        {
            AssertDefinedInEveryTerrainPass("ConstructTerrainTangent", vertexContext, mode);
        }

        // The splat UV plumbing this subtarget used to emit was guarded on UNIVERSAL_TERRAIN_SPLAT02, a
        // keyword no descriptor ever declared, so the guarded block compiled out in silence and shipped
        // that way for five release streams. A misspelled #if is invisible to the shader compiler and to
        // image comparison alike, which is why it survived that long. Assert the invariant instead: a
        // terrain pass may only test UNIVERSAL_TERRAIN_ keywords that the same pass defines or declares.
        //
        // The scope is one HLSLPROGRAM, not the whole shader, because preprocessor state does not cross
        // that boundary. The pre-fix tree had exactly that shape: every pixel pass tested
        // UNIVERSAL_TERRAIN_SPLAT01, but ShadowCaster, DepthOnly and the two picking passes never
        // defined it, and a shader-wide set difference is satisfied by the forward pass. A #pragma
        // multi_compile or shader_feature counts as a declaration, since those keywords never appear as
        // a #define and are legitimately tested.
        //
        // UNIVERSAL_TERRAIN_ENABLED is the only keyword left under this prefix, so this guards the next
        // one added rather than a live bug. The prefix stays narrow on purpose: terrain templates also
        // test keywords that core URP declares (_NORMALMAP), and a wider net would need a whitelist of
        // foreign declarations that would rot.
        [TestCase(VertexContext.WithPositionBlock, GenerationMode.ForReals)]
        [TestCase(VertexContext.Empty, GenerationMode.ForReals)]
        [TestCase(VertexContext.WithPositionBlock, GenerationMode.Preview)]
        [TestCase(VertexContext.Empty, GenerationMode.Preview)]
        public void TerrainPassesDeclareEveryUniversalTerrainKeywordTheyTest(VertexContext vertexContext, GenerationMode mode)
        {
            // Collect across shaders and passes before asserting, so one failure names every offender.
            var undeclared = new List<string>();
            foreach (var (name, code) in GenerateTerrainShaders(vertexContext, mode))
            {
                var programs = Regex.Matches(code, @"HLSLPROGRAM.*?ENDHLSL", RegexOptions.Singleline);
                Assert.IsNotEmpty(programs, $"no HLSLPROGRAM block in {name}");

                foreach (Match program in programs)
                {
                    var declared = new HashSet<string>(Regex.Matches(program.Value, @"#define\s+(UNIVERSAL_TERRAIN_\w+)")
                        .Cast<Match>().Select(match => match.Groups[1].Value));
                    foreach (Match pragma in Regex.Matches(program.Value, @"#pragma\s+(?:multi_compile|shader_feature|dynamic_branch)[^\n]*"))
                        declared.UnionWith(Regex.Matches(pragma.Value, @"UNIVERSAL_TERRAIN_\w+").Cast<Match>().Select(match => match.Value));

                    var referenced = new HashSet<string>(Regex.Matches(program.Value, @"UNIVERSAL_TERRAIN_\w+")
                        .Cast<Match>().Select(match => match.Value));
                    referenced.ExceptWith(declared);

                    if (referenced.Count > 0)
                        undeclared.Add($"{name} {PassName(code, program.Index)}: {string.Join(" ", referenced.OrderBy(keyword => keyword))}");
                }
            }

            CollectionAssert.IsEmpty(undeclared, "terrain keyword tested but never declared in the same pass");
        }

        // Regular passes carry Name "Universal Forward" from the PassName splice; the BaseMapGen passes
        // carry "Name" = "_MainTex" as a tag instead. Either way it is the last one before the program.
        static string PassName(string code, int programIndex)
        {
            var names = Regex.Matches(code.Substring(0, programIndex), @"""?Name""?\s*=?\s*""([^""]+)""");
            return names.Count > 0 ? names[names.Count - 1].Groups[1].Value : $"program at offset {programIndex}";
        }
    }
}
