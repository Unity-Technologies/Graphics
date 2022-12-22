using NUnit.Framework;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class SGGraphModelTest : BaseGraphAssetTest
    {
        [Test]
        public void TestCreateGraphVariableDeclaration_CreatesContextEntry()
        {
            var decl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "variable", ModifierFlags.None, true);
            Assert.IsInstanceOf<SGVariableDeclarationModel>(decl, "Created variable declaration should be SGVariableDeclarationModel");

            var sgDecl = (SGVariableDeclarationModel)decl;
            var context = GraphModel.GraphHandler.GetNode(sgDecl.contextNodeName);
            var contextEntry = context.GetPort(sgDecl.graphDataName);
            Assert.IsNotNull(contextEntry, "Created variable declaration should have new context entry");
        }

        [Test]
        public void TestDeleteGraphVariableDeclaration_RemovesContextEntry()
        {
            var sgDecl = (SGVariableDeclarationModel)GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "variable", ModifierFlags.None, true);

            var entryName = sgDecl.graphDataName;
            var contextName = sgDecl.contextNodeName;

            GraphModel.DeleteVariableDeclarations(new [] { sgDecl });

            Assert.IsNull(GraphModel.GraphHandler.GetNode(contextName).GetPort(entryName), "Deleting variable declaration should remove associated context entry");
        }

        [Test]
        public void TestDuplicateGraphVariableDeclaration_CreatesNewContextEntry()
        {
            var decl1 = (SGVariableDeclarationModel)GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "variable", ModifierFlags.None, true);
            var decl2 = GraphModel.DuplicateGraphVariableDeclaration(decl1);

            Assert.AreNotEqual(decl1.graphDataName, decl2.graphDataName, "Duplicated variable declaration should have own name");
            Assert.AreEqual(decl1.contextNodeName, decl2.contextNodeName, "Duplicated variable declaration should belong to same context node as original");
            Assert.IsNotNull(GraphModel.GraphHandler.GetNode(decl2.contextNodeName).GetPort(decl2.graphDataName), "Duplicated variable declaration should have new context entry");
        }
    }
}
