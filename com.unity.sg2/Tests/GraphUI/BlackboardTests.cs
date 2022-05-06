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

            Assert.IsTrue(m_Window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay), "Blackboard overlay must be present for blackboard tests");
            m_BlackboardView = (BlackboardView)blackboardOverlay
                .GetType()
                .GetField("m_BlackboardView", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(blackboardOverlay);
        }

        [UnityTest]
        public IEnumerator TestBlackboardLoadsWithCorrectFieldType()
        {
            void VerifyFloatDeclarationView()
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
                yield return null;

                var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

                var createMenu = new List<Stencil.MenuItem>();
                stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

                var floatItem = createMenu.FirstOrDefault(i => i.name == "Create Float");
                Assert.IsNotNull(floatItem, "Blackboard create menu must contain a \"Create Float\" item");

                floatItem.action.Invoke();
                yield return null;

                VerifyFloatDeclarationView();
            }

            GraphAssetUtils.SaveOpenGraphAsset(m_Window.GraphTool);
            CloseWindow();
            yield return null;

            var graphAsset = ShaderGraphAsset.HandleLoad(testAssetPath);
            CreateWindow();
            m_Window.Show();
            m_Window.Focus();
            m_Window.SetCurrentSelection(graphAsset, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            yield return null;

            Assert.IsTrue(m_Window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay), "Blackboard overlay must be present for blackboard tests");
            m_BlackboardView = (BlackboardView)blackboardOverlay
                .GetType()
                .GetField("m_BlackboardView", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(blackboardOverlay);

            VerifyFloatDeclarationView();
        }

        [UnityTest]
        public IEnumerator TestBlackboardAttempt2()
        {
            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;
            var model = (ShaderGraphModel)m_GraphView.GraphModel;

            var items = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", items, m_BlackboardView);

            var floatItem = items.FirstOrDefault(i => i.name == "Create Float");
            Assert.IsNotNull(floatItem, "Blackboard create menu must contain a \"Create Float\" item");

            floatItem.action.Invoke();
            yield return null;
        }
    }
}
