using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class HighlightTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        IVariableDeclarationModel m_IntVariableModel;
        IVariableDeclarationModel m_StringVariableModel;

        GraphViewBlackboardWindow m_BlackboardWindow;

        VariableNodeModel m_IntTokenModel1;
        VariableNodeModel m_IntTokenModel2;
        VariableNodeModel m_StringTokenModel1;
        VariableNodeModel m_StringTokenModel2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Make sure blackboard exists and its observer are run.
            var blackboard = GraphView.GetBlackboard();
            Window.rootVisualElement.Add(blackboard);

            m_IntVariableModel = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "int", ModifierFlags.None, false);

            m_StringVariableModel = GraphModel.CreateGraphVariableDeclaration(typeof(string).GenerateTypeHandle(), "string", ModifierFlags.None, false);

            //This will add the variables to their default section
            GraphModel.CheckGroupConsistency();

            //This will build the section ui
            GraphView.GetBlackboard().Query<BlackboardSection>().ForEach(t => t.UpdateFromModel());

            m_IntTokenModel1 = GraphModel.CreateNode<VariableNodeModel>();
            m_IntTokenModel1.DeclarationModel = m_IntVariableModel;

            m_IntTokenModel2 = GraphModel.CreateNode<VariableNodeModel>();
            m_IntTokenModel2.DeclarationModel = m_IntVariableModel;

            m_StringTokenModel1 = GraphModel.CreateNode<VariableNodeModel>();
            m_StringTokenModel1.DeclarationModel = m_StringVariableModel;

            m_StringTokenModel2 = GraphModel.CreateNode<VariableNodeModel>();
            m_StringTokenModel2.DeclarationModel = m_StringVariableModel;
        }

        [TearDown]
        public override void TearDown()
        {
            if (m_BlackboardWindow != null)
                m_BlackboardWindow.Close();

            base.TearDown();
        }

        void GetUI(out TokenNode intToken1, out TokenNode intToken2, out TokenNode stringToken1, out TokenNode stringToken2,
            out BlackboardField intField, out BlackboardField stringField)
        {
            intToken1 = m_IntTokenModel1.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(intToken1);

            intToken2 = m_IntTokenModel2.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(intToken2);

            stringToken1 = m_StringTokenModel1.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(stringToken1);

            stringToken2 = m_StringTokenModel2.GetUI<TokenNode>(GraphView);
            Assert.IsNotNull(stringToken2);

            intField = m_IntVariableModel.GetUI<BlackboardField>(GraphView, BlackboardCreationContext.VariableCreationContext);
            Assert.IsNotNull(intField);

            stringField = m_StringVariableModel.GetUI<BlackboardField>(GraphView, BlackboardCreationContext.VariableCreationContext);
            Assert.IsNotNull(stringField);
        }

        [UnityTest]
        public IEnumerator TestHighlightTokenSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GetUI(out TokenNode intToken1, out TokenNode intToken2, out TokenNode stringToken1, out TokenNode stringToken2,
                out BlackboardField intField, out BlackboardField stringField);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, intToken1.Model));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "1. intToken1.highlighted");
            Assert.IsTrue(intToken2.IsHighlighted(), "1. intToken2.highlighted");
            Assert.IsTrue(intField.IsHighlighted(), "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "1. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.IsHighlighted(), "1. stringToken2.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "1. m_StringField.highlighted");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, intToken1.Model));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "2. intToken1.highlighted");
            Assert.IsFalse(intToken2.IsHighlighted(), "2. intToken2.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "2. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "2. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.IsHighlighted(), "2. stringToken2.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "2. m_StringField.highlighted");
        }

        [UnityTest]
        public IEnumerator TestHighlightFieldSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GetUI(out var intToken1, out _, out var stringToken1, out _,
                out var intField, out var stringField);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, intField.Model));
            yield return null;

            Assert.IsTrue(intToken1.IsHighlighted(), "1. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "1. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "1. m_StringField.highlighted");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, stringField.Model));
            yield return null;

            Assert.IsTrue(intToken1.IsHighlighted(), "2. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "2. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.IsHighlighted(), "2. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "2. m_StringField.highlighted");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, intField.Model));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "3. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "3. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.IsHighlighted(), "3. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "3. m_StringField.highlighted");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, stringField.Model));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "4. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "4. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "4. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "4. m_StringField.highlighted");
        }
    }
}
