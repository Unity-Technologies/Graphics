using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BlackboardOnDiskTests : BlackboardTestsBase
    {
        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.Disk;

        [UnityTest]
        public IEnumerator TestPropertyLoadsWithCorrectFieldType()
        {
            void ValidateCreatedField(string createItemName, Type fieldType, int itemIndex)
            {
                var views = new List<ModelView>();
                var decl = GraphModel.VariableDeclarations[itemIndex];
                decl.GetAllViews(m_BlackboardView, v => v is SGBlackboardVariablePropertyView, views);
                var view = views.FirstOrDefault();
                Assert.IsNotNull(view, "View for created property was not found");

                var field = view.Q<BaseModelPropertyField>(className: "ge-inline-value-editor");
                if (fieldType is null)
                {
                    Assert.IsNull(field, "Created blackboard item should not have an Initialization field");
                }
                else
                {
                    Assert.IsNotNull(field, "Created blackboard item should have an Initialization field");
                    var firstChild = field.Children().First();
                    Assert.IsTrue(firstChild.GetType().IsAssignableFrom(fieldType), $"Property created with \"{createItemName}\" should have field of type {fieldType.Name}");
                }
            }

            {
                var stencil = (ShaderGraphStencil)GraphModel.Stencil;

                var createMenu = new List<Stencil.MenuItem>();
                stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

                var itemIndex = 0;
                foreach (var (createItemName, fieldType) in k_ExpectedFieldTypes)
                {
                    var floatItem = createMenu.FirstOrDefault(i => i.name == createItemName);
                    Assert.IsNotNull(floatItem, $"\"{createItemName}\" item from test case was not found in Blackboard create menu. Are the test cases up-to-date?");

                    floatItem.action.Invoke();
                    yield return null;

                    ValidateCreatedField(createItemName, fieldType, itemIndex);
                    itemIndex++;
                }
            }

            yield return SaveAndReopenGraph();

            {
                m_BlackboardView = FindBlackboardView(m_MainWindow);

                var itemIndex = 0;
                foreach (var (createItemName, fieldType) in k_ExpectedFieldTypes)
                {
                    ValidateCreatedField(createItemName, fieldType, itemIndex);
                    itemIndex++;
                }
            }
        }
    }
}
