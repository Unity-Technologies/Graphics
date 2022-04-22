using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class HighlightTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        IEnumerator RunTestFor<TModel, TGraphElement>(TypeHandle typeHandle, Func<IVariableDeclarationModel, Vector2, TModel> creator)
            where TModel : IGraphElementModel
            where TGraphElement : GraphElement
        {
            var declarationModel = GraphModel.CreateGraphVariableDeclaration(typeHandle, "Foo", ModifierFlags.None, true);
            var model1 = creator(declarationModel, Vector2.zero);
            var model2 = creator(declarationModel, Vector2.one * 50);

            MarkGraphModelStateDirty();
            yield return null;

            var token1 = model1.GetView<TGraphElement>(GraphView);
            var token2 = model2.GetView<TGraphElement>(GraphView);

            Assert.IsNotNull(token1);
            Assert.IsNotNull(token2);

            var border1 = token1.SafeQ<DynamicBorder>();
            var border2 = token2.SafeQ<DynamicBorder>();

            Assert.IsNotNull(border1);
            Assert.IsNotNull(border2);

            // There should be no selection at this point.
            Assert.AreEqual(Color.clear, border1.ComputedColor);
            Assert.AreEqual(Color.clear, border2.ComputedColor);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, model1));
            yield return null;

            token1 = model1.GetView<TGraphElement>(GraphView);
            token2 = model2.GetView<TGraphElement>(GraphView);

            border1 = token1.SafeQ<DynamicBorder>();
            border2 = token2.SafeQ<DynamicBorder>();

            // There should be a selection at this point.
            // The borders should not be black and should be different from one another (one selected, one highlighted).
            Assert.AreNotEqual(Color.clear, border1.ComputedColor);
            Assert.AreNotEqual(Color.clear, border2.ComputedColor);
            Assert.AreNotEqual(border1.ComputedColor, border2.ComputedColor);
        }

        [UnityTest]
        public IEnumerator HighlightIsAppliedToVariables()
        {
            var actions = RunTestFor<VariableNodeModel, TokenNode>(TypeHandle.Int,
                (m, p) => (VariableNodeModel)GraphModel.CreateVariableNode(m, p));

            while (actions.MoveNext())
                yield return null;
        }

        [UnityTest]
        public IEnumerator HighlightIsAppliedToPortals()
        {
            var actions = RunTestFor<ExecutionEdgePortalEntryModel, TokenNode>(TypeHandle.Unknown,
                (m, p) =>
                {
                    var portal = GraphModel.CreateNode<ExecutionEdgePortalEntryModel>("foo", p);
                    portal.DeclarationModel = m;
                    return portal;
                });

            while (actions.MoveNext())
                yield return null;
        }
    }
}
