using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    public class BlackboardTests : BaseGraphWindowTest
    {
        protected override bool hideOverlayWindows => false;
        BlackboardView m_BlackboardView;

        public override void SetUp()
        {
            base.SetUp();
            FindBlackboardView();
        }

        private void FindBlackboardView()
        {
            const string viewFieldName = "m_BlackboardView";

            var found = m_Window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay);
            Assert.IsTrue(found, "Blackboard overlay was not found");

            m_BlackboardView = (BlackboardView)blackboardOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(blackboardOverlay);
            Assert.IsNotNull(m_BlackboardView, "Blackboard view was not found");
        }

        [UnityTest]
        public IEnumerator TestBlackboardLoadsWithCorrectFieldType()
        {
            void ValidateCreatedField()
            {
                var model = (ShaderGraphModel)m_GraphView.GraphModel;
                var decl = model.VariableDeclarations.FirstOrDefault();
                Assert.IsNotNull(decl, "Menu item should have created underlying variable declaration");

                var views = new List<ModelView>();
                decl.GetAllViews(m_BlackboardView, v => v is GraphDataBlackboardVariablePropertyView, views);

                if (views.FirstOrDefault() is not GraphDataBlackboardVariablePropertyView view)
                {
                    Assert.IsFalse(true);
                    throw new Exception("Unreachable");
                }

                var field = view.Q<ConstantField>(className: "ge-inline-value-editor");
                Assert.IsNotNull(field, "Created blackboard item should contain an Initialization field");

                var firstChild = field.Children().First();
                Assert.IsTrue(firstChild is FloatField, "Float property should have a float field");
            }

            {
                var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

                var createMenu = new List<Stencil.MenuItem>();
                stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

                var floatItem = createMenu.FirstOrDefault(i => i.name == "Create Float");
                Assert.IsNotNull(floatItem, "Blackboard create menu must contain a \"Create Float\" item");

                floatItem.action.Invoke();
                yield return null;

                ValidateCreatedField();
            }

            yield return SaveAndReopenGraph();

            {
                FindBlackboardView();
                ValidateCreatedField();
            }
        }
    }
}
