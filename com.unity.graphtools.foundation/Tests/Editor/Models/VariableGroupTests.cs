using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class VariableGroupTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void TestVariableGroupCreation()
        {
            GroupModel model = new GroupModel();

            Assert.IsTrue(model.IsCollapsible());
            Assert.IsTrue(model.IsSelectable());
            Assert.IsTrue(model.IsDeletable());
            Assert.IsTrue(model.IsDroppable());
            Assert.IsTrue(model.IsRenamable());
        }

        [Test]
        public void TestSectionCreation()
        {
            SectionModel model = new SectionModel();

            Assert.IsTrue(model.IsCollapsible());
            Assert.IsTrue(!model.IsSelectable());
            Assert.IsTrue(!model.IsDeletable());
            Assert.IsTrue(!model.IsDroppable());
            Assert.IsTrue(!model.IsRenamable());
        }

        [Test]
        public void TestVariableGroupInsertion()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(TestGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            GroupModel parent = new GroupModel();

            var child1 = new GroupModel();
            var child2 = new GroupModel();
            var child3 = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(TypeHandle.Float, "asd", ModifierFlags.None, true);

            parent.InsertItem(child2);
            parent.InsertItem(child1, 0);
            parent.InsertItem(child3);

            Assert.AreEqual(3, parent.Items.Count);

            Assert.AreEqual(child1, parent.Items[0]);
            Assert.AreEqual(child2, parent.Items[1]);
            Assert.AreEqual(child3, parent.Items[2]);
        }

        [Test]
        public void TestVariableGroupRemoval()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(TestGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            GroupModel parent = new GroupModel();

            var child1 = new GroupModel();
            var child2 = new GroupModel();
            var child3 = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(
                TypeHandle.Float, "asd", ModifierFlags.None, true);

            parent.InsertItem(child2);
            parent.InsertItem(child1, 0);
            parent.InsertItem(child3);

            parent.RemoveItem(child2);

            Assert.AreEqual(2, parent.Items.Count);

            Assert.AreEqual(child1, parent.Items[0]);
            Assert.AreEqual(child3, parent.Items[1]);
        }

        [Test]
        public void Test_CreateGroupCommand([Values] TestingMode mode)
        {
            (GraphModel as GraphModel)?.CheckGroupConsistency();

            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    var rootGroup = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
                    Assert.AreEqual(1, rootGroup.Items.Count); // The variable declaration
                    Assert.AreEqual(declaration, rootGroup.Items[0]);
                    return new BlackboardGroupCreateCommand
                        (rootGroup, null, "Toto", new[] {declaration});
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    var rootGroup = GraphModel.GetSectionModel(GraphModel.Stencil.SectionNames.First());
                    Assert.AreEqual(1, rootGroup.Items.Count); // The group

                    var newGroup = rootGroup.Items[0] as IGroupModel;
                    Assert.NotNull(newGroup);
                    Assert.AreEqual("Toto", newGroup.Title);
                    Assert.AreEqual(1, newGroup.Items.Count); // The variable declaration
                    Assert.AreEqual(declaration, newGroup.Items[0]);
                });
        }

        //Note : MoveItemsAfter is thoroughly tested by Test_ReorderGraphVariableDeclarationCommand
    }
}
