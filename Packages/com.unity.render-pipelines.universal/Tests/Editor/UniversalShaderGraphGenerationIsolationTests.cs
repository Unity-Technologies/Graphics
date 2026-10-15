using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.Tests
{
    // A pass builder that assigns a shared static collection to a PassDescriptor and then calls Add on
    // it writes into the static itself. Every graph generated later in the same domain inherits the
    // addition, so the same .shadergraph imports to different code depending on what was imported
    // before it, and AssetBundles built from identical sources get different hashes (IN-155496).
    [TestFixture]
    class UniversalShaderGraphGenerationIsolationTests
    {
        const string k_LODCrossFadeInclude = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl";

        static UniversalTarget CreateTarget(Type subTargetType, bool supportsLodCrossFade)
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(subTargetType), $"{subTargetType.Name} is not registered on UniversalTarget");
            target.supportsLodCrossFade = supportsLodCrossFade;
            return target;
        }

        static string GenerateShader(Type subTargetType, bool supportsLodCrossFade)
        {
            var target = CreateTarget(subTargetType, supportsLodCrossFade);

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { target }, new[] { BlockFields.VertexDescription.Position, BlockFields.SurfaceDescription.BaseColor });

            return new Generator(graph, graph.outputNode, GenerationMode.ForReals, "GenerationIsolation").generatedShader;
        }

        static HashSet<object> StaticFieldValues(Type fieldType)
        {
            var values = typeof(UniversalTarget).Assembly.GetTypes()
                .Where(type => !type.ContainsGenericParameters)
                .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(field => field.FieldType == fieldType)
                .Select(field => field.GetValue(null))
                .Where(value => value != null);

            // The collection types do not override Equals, so the set compares by reference.
            return new HashSet<object>(values);
        }

        static IEnumerable<PassDescriptor> BuildPasses(UniversalTarget target)
        {
            var context = new TargetSetupContext();
            target.Setup(ref context);

            var passes = context.subShaders
                .SelectMany(subShader => subShader.passes)
                .Select(pass => pass.descriptor)
                .ToList();

            // Generation only builds this pass when Spacewarp is supported, so build it directly to cover it everywhere.
            passes.Add(CorePasses.XRMotionVectors(target));
            return passes;
        }

        [TestCase(typeof(UniversalUnlitSubTarget))]
        [TestCase(typeof(UniversalLitSubTarget))]
        public void PlainGraphOutputDoesNotDependOnPreviouslyGeneratedCrossFadeGraph(Type subTargetType)
        {
            var plainBefore = GenerateShader(subTargetType, supportsLodCrossFade: false);
            var crossFade = GenerateShader(subTargetType, supportsLodCrossFade: true);
            var plainAfter = GenerateShader(subTargetType, supportsLodCrossFade: false);

            StringAssert.Contains(k_LODCrossFadeInclude, crossFade, "the cross-fade graph no longer includes LODCrossFade.hlsl, so this test checks nothing");
            StringAssert.DoesNotContain(k_LODCrossFadeInclude, plainAfter);
            Assert.AreEqual(plainBefore, plainAfter, "generating a cross-fade graph changed the code of a plain graph generated after it");
        }

        // The test above only sees the passes that generation builds on this machine, and XRMotionVectors
        // needs Spacewarp support. This checks the cause on every pass instead and names the offender: a
        // pass that the cross-fade setting adds to has to own the collection it adds to.
        [TestCase(typeof(UniversalUnlitSubTarget))]
        [TestCase(typeof(UniversalLitSubTarget))]
        public void CrossFadePassesDoNotAddToSharedStaticCollections(Type subTargetType)
        {
            var staticIncludes = StaticFieldValues(typeof(IncludeCollection));
            var staticKeywords = StaticFieldValues(typeof(KeywordCollection));
            var staticDefines = StaticFieldValues(typeof(DefineCollection));

            var target = CreateTarget(subTargetType, supportsLodCrossFade: true);
            var sharedCollections = new List<string>();
            var crossFadePassCount = 0;

            foreach (var pass in BuildPasses(target))
            {
                var hasCrossFadeInclude = pass.includes != null && pass.includes.Any(include => include.path == k_LODCrossFadeInclude);
                if (!hasCrossFadeInclude)
                    continue;

                crossFadePassCount++;

                if (staticIncludes.Contains(pass.includes))
                    sharedCollections.Add($"{pass.displayName}: includes");
                if (pass.keywords != null && staticKeywords.Contains(pass.keywords))
                    sharedCollections.Add($"{pass.displayName}: keywords");
                if (pass.defines != null && staticDefines.Contains(pass.defines))
                    sharedCollections.Add($"{pass.displayName}: defines");
            }

            Assert.Greater(crossFadePassCount, 0, "no pass includes LODCrossFade.hlsl, so this test checks nothing");
            CollectionAssert.IsEmpty(sharedCollections, "a pass added the cross-fade setup to a shared static collection");
        }
    }
}
