using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    namespace  LODTests
    {
        class MyNodeModel : NodeModel
        {

        }
        class MyBlockNodeModel : BlockNodeModel
        {

        }

        class MyNode : Node
        {
            protected override void BuildPartList()
            {
                base.BuildPartList();

                PartList.AppendPart(new MyNodePart());
            }

            public bool Updated { get; private set; }

            public override void SetElementLevelOfDetail(float zoom)
            {
                base.SetElementLevelOfDetail(zoom);

                Updated = true;
            }
        }

        class MyBlockNode : BlockNode
        {
            protected override void BuildPartList()
            {
                base.BuildPartList();

                PartList.AppendPart(new MyNodePart());
            }

            public bool Updated { get; private set; }
            public override void SetElementLevelOfDetail(float zoom)
            {
                base.SetElementLevelOfDetail(zoom);

                Updated = true;
            }
        }

        class MyNodePart : IGraphElementPart
        {
            VisualElement m_Root;

            public static readonly string Name ="MyNodePart";
            public string PartName => Name;
            public VisualElement Root => m_Root;

            public bool Updated { get; private set; }
            public void BuildUI(VisualElement parent)
            {
                m_Root = new VisualElement();

                parent.Add(m_Root);
            }

            public void PostBuildUI()
            {
            }

            public void UpdateFromModel()
            {
            }

            public void OwnerAddedToView()
            {
            }

            public void OwnerRemovedFromView()
            {
            }

            public void SetLevelOfDetail(float zoom)
            {
                Updated = true;
            }
        }

        [GraphElementsExtensionMethodsCache(typeof(GraphView))]
        static class LODTestsFactoryExtensions
        {
            public static IModelView CreateMyNode(this ElementBuilder elementBuilder, MyNodeModel model)
            {
                var ui = new MyNode();

                ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
                return ui;
            }
            public static IModelView CreateMyBlock(this ElementBuilder elementBuilder, MyBlockNodeModel model)
            {
                var ui = new MyBlockNode();

                ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
                return ui;
            }
        }

        class LODTests : GraphViewTester
        {

            [UnityTest]
            public IEnumerator LODNotificationToNodesWork()
            {
                var node = GraphModel.CreateNode<MyNodeModel>("zoomzoom");

                GraphView.RebuildUI();
                yield return null;

                var ui = node.GetView<MyNode>(GraphView);

                var scale = GraphView.ViewTransform.scale;

                Helpers.ScrollWheelEvent(10,Vector2.one * 100);

                yield return null;
                var newScale = GraphView.ViewTransform.scale;
                Assert.AreNotEqual(scale,newScale);
                Assert.IsNotNull(ui);
                Assert.IsTrue(ui.Updated);
            }

            [UnityTest]
            public IEnumerator LODNotificationToPartsWork()
            {
                var node = GraphModel.CreateNode<MyNodeModel>("zoomzoom");

                GraphView.RebuildUI();
                yield return null;

                var ui = node.GetView<MyNode>(GraphView);

                var scale = GraphView.ViewTransform.scale;

                Helpers.ScrollWheelEvent(10,Vector2.one * 100);

                yield return null;
                var newScale = GraphView.ViewTransform.scale;
                Assert.AreNotEqual(scale,newScale);
                Assert.IsNotNull(ui);
                var part = (MyNodePart)ui.PartList.GetPart(MyNodePart.Name);
                Assert.IsTrue(part.Updated);
            }

            [UnityTest]
            public IEnumerator LODNotificationToNodeContainerContentsWork()
            {
                var context = GraphModel.CreateNode<ContextNodeModel>("zoomzoomctx");

                var block = context.CreateAndInsertBlock<MyBlockNodeModel>();

                GraphView.RebuildUI();
                yield return null;

                var ui = block.GetView<MyBlockNode>(GraphView);

                var scale = GraphView.ViewTransform.scale;

                Helpers.ScrollWheelEvent(10,Vector2.one * 100);

                yield return null;
                var newScale = GraphView.ViewTransform.scale;
                Assert.AreNotEqual(scale,newScale);
                Assert.IsNotNull(ui);
                Assert.IsTrue(ui.Updated);

                var part = ((MyNodePart)ui.PartList.GetPart(MyNodePart.Name));
                Assert.IsTrue(part.Updated);
            }
        }
    }
}
