using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// Guards the blocks the Light2D SubTarget reports as active against the blocks its Create menu seeds.
    /// Switching the Material in the Shader Graph Inspector re-adds every active block that is missing
    /// (GraphData.AddRemoveBlocksFromActiveList), so any extra active block shows up on the graph (UUM-153138).
    /// </summary>
    [TestFixture]
    class Light2DSubTargetBlockTests
    {
        // The Create menu seeds UniversalLight2DSubTarget.kActiveBlocks. If UniversalTarget adds core
        // blocks on top, the first Material change adds them to the graph.
        [Test]
        public void ActiveBlocksMatchTheCreateMenuSeededBlocks()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalLight2DSubTarget)));

            var blockContext = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), null);
            target.GetActiveBlocks(ref blockContext);

            CollectionAssert.AreEquivalent(UniversalLight2DSubTarget.kActiveBlocks, blockContext.activeBlocks,
                "Light2D active blocks: " + string.Join(", ", blockContext.activeBlocks.Select(b => b.name)));
        }

        // The Light2D opt-out must not leak: a SubTarget that keeps the core blocks still gets them.
        [Test]
        public void LitStillReceivesTheCoreBlocks()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalLitSubTarget)));

            var blockContext = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), null);
            target.GetActiveBlocks(ref blockContext);

            CollectionAssert.IsSubsetOf(new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
            }, blockContext.activeBlocks);
        }

        // Mirrors MaterialGraphView.ChangeTargetSettings: Light2D -> Shadow Caster 2D -> Light2D.
        [Test]
        public void SwitchingMaterialToShadowCaster2DAndBackKeepsTheLight2DBlocks()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalLight2DSubTarget)));

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { target }, UniversalLight2DSubTarget.kActiveBlocks);

            void ChangeMaterial(System.Type subTargetType)
            {
                Assert.IsTrue(target.TrySetActiveSubTarget(subTargetType));
                var activeBlocks = graph.GetActiveBlocksForAllActiveTargets();
                graph.AddRemoveBlocksFromActiveList(activeBlocks);
                graph.UpdateActiveBlocks(activeBlocks);
            }

            ChangeMaterial(typeof(UniversalShadowCaster2DSubTarget));
            ChangeMaterial(typeof(UniversalLight2DSubTarget));

            var blocks = graph.vertexContext.blocks.Concat(graph.fragmentContext.blocks)
                .Select(b => b.value.descriptor).ToList();
            CollectionAssert.AreEquivalent(UniversalLight2DSubTarget.kActiveBlocks, blocks,
                "Blocks after the round trip: " + string.Join(", ", blocks.Select(b => b.name)));
        }
    }
}
