using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class BlackboardCreateTests : BlackboardSharedTestClasses
    {
        [UnityTest]
        public IEnumerator CreateVariableFocusTextField()
        {
            yield return null;

            var inputSection = m_GraphAsset.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Input]);

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("input", false, TypeHandle.Float, typeof(BlackboardInputVariableDeclarationModel)));

            yield return null;

            var inputVariable = inputSection.Items.Last();

            yield return null;
            yield return null;

            List<ModelView> views = new List<ModelView>();
            inputVariable.GetAllViews(m_BlackboardView, t => t is BlackboardField, views);

            Assert.AreEqual(1, views.Count);

            var textField = views.First().Query<EditableLabel>().First();

            Assert.IsTrue(textField.focusController.focusedElement == textField);

        }

        [UnityTest]
        public IEnumerator CreateGroupFocusTextField()
        {
            yield return null;

            var inputSection = m_GraphAsset.GraphModel.GetSectionModel(Stencil.sections[(int)VariableType.Input]);

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(inputSection));
            yield return null;

            yield return null;

            var inputGroup = inputSection.Items.Last();

            yield return null;
            yield return null;

            List<ModelView> views = new List<ModelView>();
            inputGroup.GetAllViews(m_BlackboardView, t => t is BlackboardGroup, views);

            Assert.AreEqual(1, views.Count);

            var textField = views.First().Query<EditableLabel>().First();

            Assert.IsTrue(textField.focusController.focusedElement == textField);

        }

    }
}
