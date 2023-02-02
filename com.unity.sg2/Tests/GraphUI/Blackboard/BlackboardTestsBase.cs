using System;
using System.Reflection;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BlackboardTestsBase : BaseGraphWindowTest
    {
        protected static readonly (string, Type)[] k_ExpectedFieldTypes =
        {
            // name of item in blackboard create menu, type of initialization field (or null if it should not exist)
            ("Create Integer", typeof(IntegerField)),
            ("Create Float", typeof(FloatField)),
            ("Create Boolean", typeof(Toggle)),
            ("Create Vector 2", typeof(Vector2Field)),
            ("Create Vector 3", typeof(Vector3Field)),
            ("Create Vector 4", typeof(Vector4Field)),
            ("Create Color", typeof(ColorField)),
            ("Create Matrix 2", typeof(MatrixField)),
            ("Create Matrix 3", typeof(MatrixField)),
            ("Create Matrix 4", typeof(MatrixField)),
            ("Create Texture2D", typeof(ObjectField)),
            ("Create Texture2DArray", typeof(ObjectField)),
            ("Create Texture3D", typeof(ObjectField)),
            ("Create Cubemap", typeof(ObjectField)),
            ("Create SamplerStateData", null),
        };

        protected override bool hideOverlayWindows => false;

        protected BlackboardView m_BlackboardView;

        public override void SetUp()
        {
            base.SetUp();
            m_BlackboardView = FindBlackboardView(m_MainWindow);
        }

        protected static BlackboardView FindBlackboardView(TestEditorWindow window)
        {
            const string viewFieldName = "m_BlackboardView";

            var found = window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay);
            Assert.IsTrue(found, "Blackboard overlay was not found");

            var blackboardView = (BlackboardView)blackboardOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(blackboardOverlay);
            Assert.IsNotNull(blackboardView, "Blackboard view was not found");
            return blackboardView;
        }
    }
}
