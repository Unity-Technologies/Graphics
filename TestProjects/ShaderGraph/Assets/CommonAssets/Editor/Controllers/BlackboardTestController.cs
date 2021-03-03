using System.Collections;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.UnitTests.Controllers
{
    class BlackboardTestController : BlackboardController
    {
        EditorWindow m_EditorWindow;
        internal VisualElement m_AddButton;

        internal BlackboardTestController(
            EditorWindow associatedEditorWindow,
            GraphData model,
            BlackboardViewModel inViewModel,
            DataStore<GraphData> graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            m_AddButton = blackboard.Q<Button>("addButton");
            Assert.IsNotNull(m_AddButton);

            m_EditorWindow = associatedEditorWindow;
        }
    }
}
