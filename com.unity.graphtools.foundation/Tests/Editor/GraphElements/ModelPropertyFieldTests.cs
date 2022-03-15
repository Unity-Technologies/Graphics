using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class ModelPropertyFieldTests : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void ChangingValueOnModelUpdatesDisplayedValue()
        {
            const string newTitle = "New Title";
            var model = new NodeModel { Title = "" };
            var field = new ModelPropertyField<string>(null, model, nameof(NodeModel.Title), null, "");

            var inputField = field.SafeQ<BaseField<string>>(null, BaseField<string>.ussClassName);

            Assert.AreNotEqual(newTitle, model.Title);
            Assert.AreNotEqual(newTitle, inputField.value);

            model.Title = newTitle;
            field.UpdateDisplayedValue();

            Assert.AreEqual(model.Title, inputField.value);
        }

        [Test]
        public void ChangingValueOnModelDoesNotTriggerChangeCallback()
        {
            const string newTitle = "New Title";
            var model = new NodeModel { Title = "" };
            bool callbackCalled = false;
            var field = new ModelPropertyField<string>(null, model, nameof(NodeModel.Title), null, "", (_, __) => callbackCalled = true);

            model.Title = newTitle;
            field.UpdateDisplayedValue();
            Assert.IsFalse(callbackCalled);
        }

        [Test]
        public void ProvidedValueGetterIsUsed()
        {
            const string newTitle = "New Title";
            const string overriddenTitle = "42";
            var model = new NodeModel { Title = "" };
            var field = new ModelPropertyField<string>(null, model, nameof(NodeModel.Title), null, "", (Action<string, ModelPropertyField<string>>)null, _ => overriddenTitle);

            var inputField = field.SafeQ<BaseField<string>>(null, BaseField<string>.ussClassName);

            Assert.AreNotEqual(newTitle, model.Title);
            Assert.AreNotEqual(newTitle, inputField.value);
            Assert.AreNotEqual(overriddenTitle, model.Title);
            Assert.AreNotEqual(overriddenTitle, inputField.value);

            model.Title = newTitle;
            field.UpdateDisplayedValue();

            Assert.AreEqual(overriddenTitle, inputField.value);
        }

        [Test]
        public void SettingValueOnFieldTriggersOnValueChanged()
        {
            const string newTitle = "New Title";
            var model = new NodeModel { Title = "" };
            bool callbackCalled = false;
            var field = new ModelPropertyField<string>(null, model, nameof(NodeModel.Title), null, "", (_, __) => callbackCalled = true);
            Window.rootVisualElement.Add(field);

            var inputField = field.SafeQ<BaseField<string>>(null, BaseField<string>.ussClassName);

            Assert.AreNotEqual(newTitle, model.Title);
            Assert.AreNotEqual(newTitle, inputField.value);

            inputField.value = newTitle;

            Assert.IsTrue(callbackCalled);
        }

        class TestCommand : ModelCommand<INodeModel, string>
        {
            /// <inheritdoc />
            public TestCommand(string value, params INodeModel[] models)
                : base("", "", value, models)
            {
            }
        }

        class FakeDispatcher : Dispatcher
        {
            public Type LastDispatchedCommandType { get; private set; }

            /// <inheritdoc />
            public override void Dispatch(ICommand command, Diagnostics diagnosticFlags = Diagnostics.None)
            {
                LastDispatchedCommandType = command.GetType();
            }
        }

        class FakeCommandTarget : ICommandTarget
        {
            FakeDispatcher m_Dispatcher = new FakeDispatcher();

            public Type LastDispatchedCommandType => m_Dispatcher.LastDispatchedCommandType;

            /// <inheritdoc />
            public ICommandTarget Parent => null;

            /// <inheritdoc />
            public void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
            {
                m_Dispatcher.Dispatch(command, diagnosticsFlags);
            }
        }

        [Test]
        public void SettingValueOnFieldDispatchesCommand()
        {
            const string newTitle = "New Title";
            var model = new NodeModel { Title = "" };

            var commandTarget = new FakeCommandTarget();

            var field = new ModelPropertyField<string>(commandTarget, model, nameof(NodeModel.Title), null, "", typeof(TestCommand));
            Window.rootVisualElement.Add(field);

            var inputField = field.SafeQ<BaseField<string>>(null, BaseField<string>.ussClassName);

            Assert.AreNotEqual(newTitle, model.Title);
            Assert.AreNotEqual(newTitle, inputField.value);
            Assert.AreNotEqual(typeof(TestCommand), commandTarget.LastDispatchedCommandType);

            inputField.value = newTitle;

            Assert.AreEqual(typeof(TestCommand), commandTarget.LastDispatchedCommandType);
        }
    }
}
