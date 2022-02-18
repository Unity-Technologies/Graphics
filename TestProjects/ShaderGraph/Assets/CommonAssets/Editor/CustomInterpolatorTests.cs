using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class CustomInterpolatorTests
    {
        [Test]
        public void SimpleThresholdTest()
        {
            const string kGraphName = "Assets/CommonAssets/Graphs/CustomInterpolatorThreshold.shadergraph";
            const int kExpectedPadding = 4;

            GraphData graph;
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporterLegacy.GetShaderText(kGraphName, out lti, assetCollection, out graph);
            Assert.NotNull(graph, $"Invalid graph data found for {kGraphName}");

            graph.ValidateGraph();

            // setup the thresholds expected by this graph. We will add some buffer against the test target's padding in case the target for the test changes.
            int padding = kExpectedPadding - graph.activeTargets.First().padCustomInterpolatorLimit;
            int initialErrorThreshold = ShaderGraphProjectSettings.instance.customInterpolatorErrorThreshold;
            int initialwarningThreshold = ShaderGraphProjectSettings.instance.customInterpolatorWarningThreshold;
            ShaderGraphProjectSettings.instance.customInterpolatorErrorThreshold   = 13 - padding;
            ShaderGraphProjectSettings.instance.customInterpolatorWarningThreshold = 12 - padding;

            graph.ValidateCustomBlockLimit();
            var msgs = graph.messageManager.GetNodeMessages();

            // this is not a granular test- but the expected error/warning messages for this graph given the thresholds and padding should be 1 of each.
            int nWarnings = msgs.Count(nodeKey => nodeKey.Value.Any(item => item.severity == Rendering.ShaderCompilerMessageSeverity.Warning));
            int nErrors = msgs.Count(nodeKey => nodeKey.Value.Any(item => item.severity == Rendering.ShaderCompilerMessageSeverity.Error));

            // reset the thresholds.
            ShaderGraphProjectSettings.instance.customInterpolatorErrorThreshold = initialErrorThreshold;
            ShaderGraphProjectSettings.instance.customInterpolatorWarningThreshold = initialwarningThreshold;

            // actual tests.
            Assert.IsTrue(nErrors == 1);
            Assert.IsTrue(nWarnings == 1);
        }
    }
}
