using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIToolkitTests
{
    public class EditableLabelTests : GtfTestFixture
    {
        static readonly string k_SomeText = "Some text";

        [Test]
        public void SetValueWithoutNotifyDoesNotTriggerChangeCallback()
        {
            var editableLabel = new EditableLabel();
            bool called = false;
            editableLabel.RegisterCallback<ChangeEvent<string>>(_ => called = true);
            Window.rootVisualElement.Add(editableLabel);
            editableLabel.SetValueWithoutNotify("Blah");

            Assert.IsFalse(called, "CollapsedButton called our callback.");
        }

        [Test]
        public void SetValueWithoutNotifyDoesNothingWhenFieldIsInEditMode()
        {
            var editableLabel = new EditableLabel();
            var label = editableLabel.SafeQ<Label>(EditableLabel.labelName);
            Window.rootVisualElement.Add(editableLabel);
            editableLabel.SetValueWithoutNotify("42");

            editableLabel.BeginEditing();
            editableLabel.SetValueWithoutNotify("Blah");
            editableLabel.Blur();

            Assert.AreEqual("42", label.text);
        }

        [UnityTest]
        public IEnumerator SingleClickOnEditableLabelDoesNotShowTextField()
        {
            var editableLabel = new EditableLabel();
            Window.rootVisualElement.Add(editableLabel);
            yield return null;

            var label = editableLabel.SafeQ(EditableLabel.labelName);
            var textField = editableLabel.SafeQ(EditableLabel.textFieldName);

            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);

            var center = label.parent.LocalToWorld(label.layout.center);
            Helpers.Click(center);
            yield return null;

            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator DoubleClickOnEditableLabelShowsTextField()
        {
            var editableLabel = new EditableLabel();
            Window.rootVisualElement.Add(editableLabel);
            yield return null;

            var label = editableLabel.SafeQ(EditableLabel.labelName);
            var textField = editableLabel.SafeQ(EditableLabel.textFieldName);
            var center = label.parent.LocalToWorld(label.layout.center);

            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);

            Helpers.Click(center, clickCount: 2);
            yield return null;

            Assert.AreEqual(DisplayStyle.None, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, textField.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator EscapeCancelsEditing()
        {
            var editableLabel = new EditableLabel();
            editableLabel.SetValueWithoutNotify("My Text");
            string newValue = null;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => newValue = e.newValue);
            Window.rootVisualElement.Add(editableLabel);
            // Compute layout
            yield return null;

            var label = editableLabel.SafeQ(EditableLabel.labelName);
            var textField = editableLabel.SafeQ(EditableLabel.textFieldName);
            var center = label.parent.LocalToWorld(label.layout.center);

            // Activate text field
            Helpers.Click(center, clickCount: 2);

            // Type some text
            Helpers.Type(k_SomeText);

            // Type Escape
            Helpers.KeyPressed(KeyCode.Escape);
            yield return null;

            Assert.IsNull(newValue);
            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator ReturnCommitsEditing()
        {
            var editableLabel = new EditableLabel();
            editableLabel.SetValueWithoutNotify("My Text");
            string newValue = null;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => newValue = e.newValue);
            Window.rootVisualElement.Add(editableLabel);
            // Compute layout
            yield return null;

            var label = editableLabel.SafeQ(EditableLabel.labelName);
            var center = label.parent.LocalToWorld(label.layout.center);

            // Activate text field
            Helpers.Click(center, clickCount: 2);

            // Type some text
            Helpers.Type(k_SomeText);

            Helpers.KeyPressed(KeyCode.Return);
            yield return null;

            Assert.AreEqual(k_SomeText, newValue);
        }

        [UnityTest]
        public IEnumerator BlurCommitsEditing()
        {
            var editableLabel = new EditableLabel();
            editableLabel.SetValueWithoutNotify("My Text");
            string newValue = null;
            editableLabel.RegisterCallback<ChangeEvent<string>>(e => newValue = e.newValue);
            Window.rootVisualElement.Add(editableLabel);
            // Compute layout
            yield return null;

            var label = editableLabel.SafeQ(EditableLabel.labelName);
            var textField = editableLabel.SafeQ(EditableLabel.textFieldName);
            var center = label.parent.LocalToWorld(label.layout.center);

            // Activate text field
            Helpers.Click(center, clickCount: 2);
            yield return null;

            // Type some text
            Helpers.Type(k_SomeText);
            yield return null;

            // Blur the field
            Helpers.Click(Vector2.zero);
            yield return null;

            Assert.AreEqual(k_SomeText, newValue);
            Assert.AreEqual(DisplayStyle.Flex, label.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, textField.resolvedStyle.display);
        }
    }
}
