using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class VariableDeclarationModelTests
    {
        [Test]
        public void CloningAVariableClonesFields()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(ClassGraphAssetModel));
            graphAssetModel.CreateGraph("test");

            var variableDeclaration = graphAssetModel.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "asd", ModifierFlags.None, true);
            variableDeclaration.Tooltip = "asdasd";
            var clone = (variableDeclaration as VariableDeclarationModel).Clone();
            Assert.IsFalse(ReferenceEquals(variableDeclaration, clone));
            Assert.AreEqual(variableDeclaration.Tooltip, clone.Tooltip);
            Assert.AreEqual(variableDeclaration.DataType, clone.DataType);
            Assert.AreNotEqual(variableDeclaration.Guid, clone.Guid);
        }

        [Test]
        public void CanDuplicateVariableDeclarations()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(ClassGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            var variableDeclaration = graphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "asd", ModifierFlags.None, true);
            variableDeclaration.Tooltip = "asdasd";

            var duplicatedDeclaration = graphModel.DuplicateGraphVariableDeclaration(variableDeclaration);
            Assert.IsFalse(ReferenceEquals(variableDeclaration, duplicatedDeclaration));
            Assert.AreEqual(variableDeclaration.Tooltip, duplicatedDeclaration.Tooltip);
            Assert.AreEqual(variableDeclaration.DataType, duplicatedDeclaration.DataType);
            Assert.AreNotEqual(variableDeclaration.Guid, duplicatedDeclaration.Guid);
        }

        [Test]
        public void CanInstantiateCustomVariableDeclarations()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(ClassGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            const string tooltip = "asdasd";
            const int customValue = 42;
            var variableDeclaration = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(
                TypeHandle.Float, "asd", ModifierFlags.None, true,
                initializationCallback: (v, c) =>
                {
                    v.Tooltip = tooltip;
                    v.CustomValue = customValue;
                });

            Assert.AreEqual(tooltip, variableDeclaration.Tooltip, "Tooltip wasn't set properly");
            Assert.AreEqual(customValue, variableDeclaration.CustomValue, "Custom value wasn't set properly");
        }

        [Test]
        public void CanDuplicateCustomVariableDeclarations()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(ClassGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            const string tooltip = "asdasd";
            const int customValue = 42;
            var variableDeclaration = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(
                TypeHandle.Float, "asd", ModifierFlags.None, true,
                initializationCallback: (v, c) =>
                {
                    v.Tooltip = tooltip;
                    v.CustomValue = customValue;
                });

            var duplicatedDeclaration = graphModel.DuplicateGraphVariableDeclaration(variableDeclaration);
            Assert.IsFalse(ReferenceEquals(variableDeclaration, duplicatedDeclaration), "Duplicated declaration is the same as the original");
            Assert.AreEqual(variableDeclaration.Tooltip, duplicatedDeclaration.Tooltip, "Tooltip of duplicated declaration differs from original");
            Assert.AreEqual(variableDeclaration.DataType, duplicatedDeclaration.DataType, "DataType of duplicated declaration differs from original");
            Assert.AreEqual(variableDeclaration.CustomValue, duplicatedDeclaration.CustomValue, "CustomValue of duplicated declaration differs from original");
            Assert.AreNotEqual(variableDeclaration.Guid, duplicatedDeclaration.Guid, "Guid of duplicated declaration is not unique");
        }

        [Test]
        public void CanCreateVariableDeclarations()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(ClassGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            Assert.AreEqual(0, graphModel.VariableDeclarations.Count, "Unexpected presence of variable declarations after graph creation.");

            var variableDeclaration = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(TypeHandle.Float, "asd", ModifierFlags.None, true);
            Assert.IsNotNull(variableDeclaration, "Variable declaration was not created.");
            Assert.AreEqual(1, graphModel.VariableDeclarations.Count, "Variable declaration was not added to the graph.");
            Assert.IsTrue(graphModel.TryGetModelFromGuid(variableDeclaration.Guid, out _), "Variable declaration was not found by guid.");

            var customVariableDeclaration = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(TypeHandle.Float, "asd", ModifierFlags.None, true);
            Assert.IsNotNull(customVariableDeclaration, "Custom variable declaration was not created.");
            Assert.AreEqual(2, graphModel.VariableDeclarations.Count, "Custom variable declaration was not added to the graph.");
            Assert.IsTrue(graphModel.TryGetModelFromGuid(customVariableDeclaration.Guid, out _), "Custom variable declaration was not found by guid.");
        }

        [Test]
        public void DeleteVariableDeclarationCanDeleteReferencedVariables()
        {
            var graphAssetModel = IGraphAssetModelHelper.Create("test", "", typeof(ClassGraphAssetModel));
            graphAssetModel.CreateGraph("test");
            var graphModel = graphAssetModel.GraphModel;

            Assert.AreEqual(0, graphModel.NodeModels.OfType<VariableNodeModel>().Count(), "Unexpected presence of variable after graph creation.");

            var variableDeclaration = graphModel.CreateGraphVariableDeclaration<TestVariableDeclarationModel>(TypeHandle.Float, "asd", ModifierFlags.None, true);

            var var1 = graphModel.CreateVariableNode(variableDeclaration, new Vector2(0, 0));
            var var2 = graphModel.CreateVariableNode(variableDeclaration, new Vector2(314, 42));

            Assert.IsNotNull(var1, "First variable instance was not created.");
            Assert.IsNotNull(var2, "Second variable instance was not created.");
            Assert.AreEqual(2, graphModel.NodeModels.OfType<VariableNodeModel>().Count(), "Variables were not added to the graph.");
            Assert.IsTrue(graphModel.TryGetModelFromGuid(var1.Guid, out _), "First instance was not found");
            Assert.IsTrue(graphModel.TryGetModelFromGuid(var2.Guid, out _), "Second instance was not found");

            graphModel.DeleteVariableDeclarations(new[] { variableDeclaration });
            Assert.AreEqual(0, graphModel.VariableDeclarations.Count, "Variable declaration was not properly discarded from the graph");
            Assert.AreEqual(0, graphModel.NodeModels.OfType<VariableNodeModel>().Count(), "Variables were not properly discarded from the graph.");
        }
    }
}
