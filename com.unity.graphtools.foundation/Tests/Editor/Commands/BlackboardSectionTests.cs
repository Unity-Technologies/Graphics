using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class BlackboardSectionTests : BlackboardSharedTestClasses
    {

        [UnityTest]
        public IEnumerator TestSingleVariableConversion()
        {
            yield return null;

            var outputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Output]);
            var inputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Input]);

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("input", false, TypeHandle.Float,
                typeof(BlackboardInputVariableDeclarationModel)));

            yield return null;

            Assert.IsFalse(outputSection.Items.Any());
            Assert.IsTrue(inputSection.Items.Any());

            m_BlackboardView.Dispatch(new ReorderGroupItemsCommand(outputSection,
                null, inputSection.Items.First()));

            yield return null;
            Assert.IsFalse(inputSection.Items.Any());
            Assert.IsTrue(outputSection.Items.Any());
            Assert.IsTrue(outputSection.Items.First() is BlackboardOutputVariableDeclarationModel);
        }

        [UnityTest]
        public IEnumerator TestOnlyGroupConversion()
        {
            yield return null;

            var outputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Output]);
            var inputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Input]);

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(inputSection));
            yield return null;

            var group1 = inputSection.Items.OfType<IGroupModel>().FirstOrDefault();

            Assert.NotNull(group1);

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(group1));

            yield return null;
            var group2 = group1.Items.OfType<IGroupModel>().FirstOrDefault();

            Assert.NotNull(group2);

            m_BlackboardView.Dispatch(new ReorderGroupItemsCommand(outputSection,
                null, group1));

            yield return null;
            Assert.AreEqual(outputSection, group1.ParentGroup);
            Assert.AreEqual(group1, group2.ParentGroup);
        }

        [UnityTest]
        public IEnumerator TestVariableWithinGroupConversion()
        {
            yield return null;

            var outputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Output]);
            var inputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Input]);

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(inputSection));

            yield return null;
            var group1 = inputSection.Items.OfType<IGroupModel>().FirstOrDefault();

            Assert.NotNull(group1);

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("input", false, TypeHandle.Float,
                typeof(BlackboardInputVariableDeclarationModel), group1));

            yield return null;
            var input = m_GraphAssetModel.GraphModel.VariableDeclarations.First();

            Assert.AreEqual(group1, input.ParentGroup);

            m_BlackboardView.Dispatch(new ReorderGroupItemsCommand(outputSection,
                null, group1));

            var newGroup1 = outputSection.Items.OfType<IGroupModel>().First();

            Assert.AreEqual(group1.Title, newGroup1.Title);

            Assert.IsTrue(newGroup1.Items.Any());

            var newOutput = newGroup1.Items.OfType<IVariableDeclarationModel>().FirstOrDefault();

            Assert.IsNotNull(newOutput);

            Assert.IsFalse(inputSection.Items.Any());
        }

        [UnityTest]
        public IEnumerator TestVariablePartialConversion()
        {
            yield return null;

            var outputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Output]);
            var inputSection =
                m_GraphAssetModel.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Input]);

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(outputSection));
            yield return null;

            var group1 = outputSection.Items.OfType<IGroupModel>().FirstOrDefault();

            Assert.NotNull(group1);

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("output", false, TypeHandle.Float,
                typeof(BlackboardOutputVariableDeclarationModel), group1));
            yield return null;

            var output = m_GraphAssetModel.GraphModel.VariableDeclarations.First();

            Assert.AreEqual(group1,output.ParentGroup);

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("output 2", false, TypeHandle.Float,
                typeof(BlackboardOutputVariableDeclarationModel), group1));
            yield return null;

            var output2 = (BlackboardOutputVariableDeclarationModel)m_GraphAssetModel.GraphModel.VariableDeclarations.Skip(1).First();

            Assert.AreNotEqual(output, output2);
            Assert.AreEqual(group1, output2.ParentGroup);

            output2.someToggle = true;

            m_BlackboardView.Dispatch(new ReorderGroupItemsCommand(inputSection,
                null, group1));
            yield return null;

            var newGroup1 = inputSection.Items.OfType<IGroupModel>().First();

            Assert.AreEqual(group1.Title, newGroup1.Title);

            Assert.IsTrue(newGroup1.Items.Any());
            Assert.IsTrue(group1.Items.Any());

            var newOutput2 = newGroup1.Items.OfType<IVariableDeclarationModel>().FirstOrDefault();

            Assert.IsNotNull(newOutput2);

            Assert.IsTrue(inputSection.Items.Any());
            Assert.IsTrue(outputSection.Items.Any());

            Assert.AreEqual(outputSection, group1.ParentGroup);

            Assert.IsTrue(group1.Items.Contains(output));
            Assert.IsFalse(group1.Items.Contains(output2));
            Assert.IsFalse(group1.Items.Contains(newOutput2));

            Assert.IsFalse(newGroup1.Items.Contains(output));
        }


        [UnityTest]
        public IEnumerator AddingAVariableUIWorksWithUndo()
        {
            yield return null;
            Undo.IncrementCurrentGroup();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("toto", true, typeof(float).GenerateTypeHandle(), typeof(BlackboardVariableDeclarationModel)));

            //We need the blackboard window open to have the blackboard updater
            var blackboardWindow = ConsoleWindowBridge.SpawnAttachedViewToolWindow<GraphViewBlackboardWindow>(m_Window, m_Window.GraphView);

            yield return null;

            var rows = m_BlackboardView.Blackboard.Query<BlackboardRow>().ToList();

            Assert.That(rows.Count, Is.EqualTo(1));

            yield return null;

            Undo.IncrementCurrentGroup();

            Undo.PerformUndo();

            yield return null;
            yield return null;

            rows = m_BlackboardView.Blackboard.Query<BlackboardRow>().ToList();

            Assert.That(rows.Count, Is.EqualTo(0));

            yield return null;

            Undo.PerformRedo();

            yield return null;

            rows = m_BlackboardView.Blackboard.Query<BlackboardRow>().ToList();

            Assert.That(rows.Count, Is.EqualTo(1));

            blackboardWindow.Close();
        }
    }
}
