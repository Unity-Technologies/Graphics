using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class HighlightTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        IVariableDeclarationModel m_IntVariableModel;
        IVariableDeclarationModel m_StringVariableModel;

        BlackboardView BlackboardView { get; set; }

        VariableNodeModel m_IntTokenModel1;
        VariableNodeModel m_IntTokenModel2;
        VariableNodeModel m_StringTokenModel1;
        VariableNodeModel m_StringTokenModel2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Make sure blackboard exists and its observer are run.
            BlackboardView = new BlackboardView(Window, GraphView);
            Window.rootVisualElement.Add(BlackboardView);

            m_IntVariableModel = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "int", ModifierFlags.None, false);

            m_StringVariableModel = GraphModel.CreateGraphVariableDeclaration(typeof(string).GenerateTypeHandle(), "string", ModifierFlags.None, false);

            //This will add the variables to their default section
            GraphModel.CheckGroupConsistency();

            m_IntTokenModel1 = GraphModel.CreateNode<VariableNodeModel>("int1", new Vector2(10, 10));
            m_IntTokenModel1.DeclarationModel = m_IntVariableModel;

            m_IntTokenModel2 = GraphModel.CreateNode<VariableNodeModel>("int2", new Vector2(210, 10));
            m_IntTokenModel2.DeclarationModel = m_IntVariableModel;

            m_StringTokenModel1 = GraphModel.CreateNode<VariableNodeModel>("string1", new Vector2(10, 50));
            m_StringTokenModel1.DeclarationModel = m_StringVariableModel;

            m_StringTokenModel2 = GraphModel.CreateNode<VariableNodeModel>("string2", new Vector2(210, 50));
            m_StringTokenModel2.DeclarationModel = m_StringVariableModel;
        }

        void GetUI(out TokenNode intToken1, out TokenNode intToken2, out TokenNode stringToken1, out TokenNode stringToken2,
            out BlackboardField intField, out BlackboardField stringField)
        {
            intToken1 = m_IntTokenModel1.GetView<TokenNode>(GraphView);
            Assert.IsNotNull(intToken1);

            intToken2 = m_IntTokenModel2.GetView<TokenNode>(GraphView);
            Assert.IsNotNull(intToken2);

            stringToken1 = m_StringTokenModel1.GetView<TokenNode>(GraphView);
            Assert.IsNotNull(stringToken1);

            stringToken2 = m_StringTokenModel2.GetView<TokenNode>(GraphView);
            Assert.IsNotNull(stringToken2);

            intField = m_IntVariableModel.GetView<BlackboardField>(BlackboardView, BlackboardCreationContext.VariableCreationContext);
            Assert.IsNotNull(intField);

            stringField = m_StringVariableModel.GetView<BlackboardField>(BlackboardView, BlackboardCreationContext.VariableCreationContext);
            Assert.IsNotNull(stringField);
        }

        [UnityTest]
        public IEnumerator TestHighlightTokenSelection()
        {
            MarkGraphModelStateDirty();
            yield return null;

            GetUI(out TokenNode intToken1, out TokenNode intToken2, out TokenNode stringToken1, out TokenNode stringToken2,
                out BlackboardField intField, out BlackboardField stringField);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, intToken1.GraphElementModel));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "1. intToken1.highlighted");
            Assert.IsTrue(intToken2.IsHighlighted(), "1. intToken2.highlighted");
            Assert.IsTrue(intField.IsHighlighted(), "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "1. stringToken1.highlighted");
            Assert.IsFalse(stringToken2.IsHighlighted(), "1. stringToken2.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "1. m_StringField.highlighted");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, intToken1.GraphElementModel));
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
            MarkGraphModelStateDirty();
            yield return null;

            GetUI(out var intToken1, out _, out var stringToken1, out _,
                out var intField, out var stringField);

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, intField.GraphElementModel));
            yield return null;

            Assert.IsTrue(intToken1.IsHighlighted(), "1. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "1. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "1. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "1. m_StringField.highlighted");

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, stringField.GraphElementModel));
            yield return null;

            Assert.IsTrue(intToken1.IsHighlighted(), "2. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "2. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.IsHighlighted(), "2. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "2. m_StringField.highlighted");

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, intField.GraphElementModel));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "3. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "3. m_IntField.highlighted");
            Assert.IsTrue(stringToken1.IsHighlighted(), "3. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "3. m_StringField.highlighted");

            BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, stringField.GraphElementModel));
            yield return null;

            Assert.IsFalse(intToken1.IsHighlighted(), "4. intToken1.highlighted");
            Assert.IsFalse(intField.IsHighlighted(), "4. m_IntField.highlighted");
            Assert.IsFalse(stringToken1.IsHighlighted(), "4. stringToken1.highlighted");
            Assert.IsFalse(stringField.IsHighlighted(), "4. m_StringField.highlighted");
        }
    }
}
