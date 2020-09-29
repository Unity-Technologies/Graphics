using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class TargetTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void CanCreateBlankGraph()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(0, graph.activeTargets.Count());
        }

        public static bool s_ForceVFXFakeTargetVisible = false;
#if !VFX_GRAPH_10_0_0_OR_NEWER
        //A barebone VFXTarget for testing coverage.
        sealed class VFXTarget : UnityEditor.ShaderGraph.Target
        {
            public VFXTarget()
            {
                displayName = "Fake VFX Target"; //Should not be displayed outside the test runner.
                isHidden = !s_ForceVFXFakeTargetVisible;
            }
            public override void GetActiveBlocks(ref UnityEditor.ShaderGraph.TargetActiveBlockContext context)
            {
                context.AddBlock(ShaderGraph.BlockFields.SurfaceDescription.BaseColor);
                context.AddBlock(ShaderGraph.BlockFields.SurfaceDescription.Alpha);
            }
            public override void GetFields(ref TargetFieldContext context)
            {
            }
            public override void Setup(ref TargetSetupContext context)
            {
            }
            public override bool IsActive() => false;
            public override bool WorksWithSRP(UnityEngine.Rendering.RenderPipelineAsset scriptableRenderPipeline)
            {
                return false;
            }
            public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, System.Action onChange, System.Action<System.String> registerUndo)
            {
            }
        }
#endif

        [Test]
        public void CanInitializeOutputTargets()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new [] { new VFXTarget() }, null);

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(1, graph.activeTargets.Count());
            Assert.AreEqual(typeof(VFXTarget), graph.activeTargets.ElementAt(0).GetType());
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanAddTarget()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();

            var vfxTarget = graph.allPotentialTargets.FirstOrDefault(x => x is VFXTarget);
            graph.SetTargetActive(vfxTarget);

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(1, graph.activeTargets.Count());
            Assert.AreEqual(vfxTarget, graph.activeTargets.ElementAt(0));
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void ActiveTargetsArePotentialTargets()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();

            var vfxTarget = new VFXTarget();
            graph.SetTargetActive(vfxTarget);
            Assert.IsTrue(graph.allPotentialTargets.Contains(vfxTarget));

            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void GetTargetIndexWorks()
        {
            s_ForceVFXFakeTargetVisible = true;

            GraphData graph = new GraphData();
            graph.AddContexts();

            int targetIndex = graph.GetTargetIndexByKnownType(typeof(VFXTarget));
            Assert.IsTrue(targetIndex >= 0);

            var vfxTarget = new VFXTarget();
            graph.SetTargetActive(vfxTarget);

            var targetIndex2 = graph.GetTargetIndex(vfxTarget);
            Assert.AreEqual(targetIndex, targetIndex2);
           
            var nonActiveVFXTarget = new VFXTarget();
            Assert.AreEqual(-1, graph.GetTargetIndex(nonActiveVFXTarget));

            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanRemoveTarget()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();

            var vfxTarget = new VFXTarget();
            graph.InitializeOutputs(new [] { vfxTarget }, null);

            graph.SetTargetInactive(vfxTarget);

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(0, graph.activeTargets.Count());
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanSetBlockActive()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new [] { new VFXTarget() }, new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.BaseColor, BlockFields.SurfaceDescription.NormalTS } );

            // Block active state should match VFX Target's default GetActiveBlocks
            var blocks = graph.GetNodes<BlockNode>().ToList();
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(BlockFields.SurfaceDescription.BaseColor, blocks[0].descriptor);
            Assert.AreEqual(true, blocks[0].isActive);
            Assert.AreEqual(BlockFields.SurfaceDescription.NormalTS, blocks[1].descriptor);
            Assert.AreEqual(false, blocks[1].isActive);
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanUpdateBlockActiveState()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new [] { new VFXTarget() }, new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.BaseColor, BlockFields.SurfaceDescription.NormalTS } );

            // Remove VFX target
            var vfxTarget = graph.allPotentialTargets.FirstOrDefault(x => x is VFXTarget);
            graph.SetTargetInactive(vfxTarget);

            var activeBlocks = graph.GetActiveBlocksForAllActiveTargets();
            graph.UpdateActiveBlocks(activeBlocks);

            // All blocks should be inactive as there are no active targets
            var blocks = graph.GetNodes<BlockNode>().ToList();
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(BlockFields.SurfaceDescription.BaseColor, blocks[0].descriptor);
            Assert.AreEqual(false, blocks[0].isActive);
            Assert.AreEqual(BlockFields.SurfaceDescription.NormalTS, blocks[1].descriptor);
            Assert.AreEqual(false, blocks[1].isActive);
            s_ForceVFXFakeTargetVisible = false;
        }
    }
}
