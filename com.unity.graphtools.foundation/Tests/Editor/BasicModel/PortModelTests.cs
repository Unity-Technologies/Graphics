using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests
{
    public class PortModelTests
    {
        IGraphAsset m_GraphAsset;

        [SetUp]
        public void SetUp()
        {
            m_GraphAsset = GraphAssetCreationHelpers<ClassGraphAsset>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test");
            m_GraphAsset.CreateGraph();
        }

        [Test]
        public void PortTooltipReturnsExpectedDefaultValues()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();

            Assert.AreEqual("Input execution flow", node1.ExeInput0.ToolTip);
            Assert.AreEqual("Output execution flow", node1.ExeOutput0.ToolTip);
            Assert.AreEqual("Input of type Integer", node1.Input0.ToolTip);
            Assert.AreEqual("Output of type Integer", node1.Output0.ToolTip);
        }

        [Test]
        public void PortTooltipReturnsExpectedOverriddenValues()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();

            const string newTooltip1 = "foo";
            const string newTooltip2 = "Bar";
            const string newTooltip3 = "baZ";
            const string newTooltip4 = "";

            node1.ExeInput0.ToolTip = newTooltip1;
            node1.ExeOutput0.ToolTip = newTooltip2;
            node1.Input0.ToolTip = newTooltip3;
            node1.Output0.ToolTip = newTooltip4;

            Assert.AreEqual(newTooltip1, node1.ExeInput0.ToolTip);
            Assert.AreEqual(newTooltip2, node1.ExeOutput0.ToolTip);
            Assert.AreEqual(newTooltip3, node1.Input0.ToolTip);
            Assert.AreEqual(newTooltip4, node1.Output0.ToolTip);
        }

        [Test]
        public void PortTooltipReturnsDefaultValueWhenAssignedNull()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();

            const string newTooltip1 = "foo";

            node1.ExeInput0.ToolTip = newTooltip1;
            Assert.AreEqual(newTooltip1, node1.ExeInput0.ToolTip);

            node1.ExeInput0.ToolTip = null;
            Assert.AreEqual("Input execution flow", node1.ExeInput0.ToolTip);
        }

        [Test]
        public void PortDisplayTitleReturnsExpectedValues()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var portWithTitle1 = ((IHasTitle) node1.ExeInput0);
            var portWithTitle2 = ((IHasTitle) node1.ExeOutput0);

            const string newTitle1 = "this is my title";
            const string newTitle2 = "42";

            portWithTitle1.Title = newTitle1;
            portWithTitle2.Title = newTitle2;

            Assert.AreEqual(newTitle1, portWithTitle1.DisplayTitle);
            Assert.AreEqual(newTitle2, portWithTitle2.DisplayTitle);
        }
    }
}
