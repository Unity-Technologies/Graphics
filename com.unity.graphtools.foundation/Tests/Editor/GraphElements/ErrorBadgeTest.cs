using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    class TestableErrorBadge : ErrorBadge
    {
        public new void ShowText()
        {
            base.ShowText();
        }

        public VisualElement GetTextElement()
        {
            return m_TextElement;
        }
    };

    public class TestErrorModel : IErrorBadgeModel
    {
        /// <inheritdoc />
        public SerializableGUID Guid { get; set; }

        /// <inheritdoc />
        public void AssignNewGuid()
        {
        }

        /// <inheritdoc />
        public IGraphModel GraphModel { get; set; }

        /// <inheritdoc />
        public IGraphElementContainer Container => null;

        /// <inheritdoc />
        public IReadOnlyList<Capabilities> Capabilities { get; } = new List<Capabilities>();

        /// <inheritdoc />
        public Color Color { get; set; }

        /// <inheritdoc />
        public bool HasUserColor => false;

        /// <inheritdoc />
        public void ResetColor()
        {
        }

        /// <inheritdoc />
        public IGraphElementModel ParentModel => this;

        /// <inheritdoc />
        public LogType ErrorType => LogType.Error;

        /// <inheritdoc />
        public string ErrorMessage => "Test error.";
    }

    [GraphElementsExtensionMethodsCache(typeof(GraphView))]
    public static class ErrorBadgeTestFactoryExtensions
    {
        public static IModelView CreateErrorBadgeModelView(this ElementBuilder elementBuilder, TestErrorModel model)
        {
            var badge = new TestableErrorBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return badge;
        }
    }

    [TestFixture]
    public class ErrorBadgeTest : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator ErrorTextBackgroundIsNotTransparent()
        {
            var badgeModel = new TestErrorModel();
            GraphModel.AddBadge(badgeModel);
            MarkGraphModelStateDirty();
            yield return null;

            var badge = badgeModel.GetView<TestableErrorBadge>(GraphView);
            Assert.IsNotNull(badge);
            badge.ShowText();
            yield return null;

            var textElement = badge.GetTextElement();
            var bgColor = textElement.resolvedStyle.backgroundColor;
            Assert.AreNotEqual(0.0f, bgColor.a);
        }
    }
}
