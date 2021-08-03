using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Views.Blackboard;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class PropertyTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/Properties.shadergraph";
        GraphData m_Graph;

        PropertyCollector m_Collector;

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        Dictionary<string, PreviewNode> m_TestNodes = new Dictionary<string, PreviewNode>();

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

            m_Collector = new PropertyCollector();
            m_Graph.CollectShaderProperties(m_Collector, GenerationMode.ForReals);

            // Open up the window
            if (!ShaderGraphImporterEditor.ShowGraphEditWindow(kGraphName))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow could not open " + kGraphName);
            }

            m_Window = EditorWindow.GetWindow<MaterialGraphEditWindow>();

            if (m_Window == null)
            {
                Assert.Fail("Could not open window");
            }

            // EditorWindow.GetWindow will return a new window if one is not found. A new window will have graphObject == null.
            if (m_Window.graphObject == null)
            {
                Assert.Fail("Existing Shader Graph window of " + kGraphName + " not found.");
            }

            m_GraphEditorView = m_Window.graphEditorView;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            // Don't spawn ask-to-save dialog
            m_Window.graphObject = null;
            m_Window.Close();
        }

        [Test]
        public void SliderPropertyRangeMinLesserThanMax()
        {
            foreach(AbstractShaderProperty property in m_Collector.properties)
            {
                if (property is Vector1ShaderProperty vector1ShaderProperty && vector1ShaderProperty.floatType == FloatType.Slider)
                {
                    Assert.IsTrue(vector1ShaderProperty.rangeValues.x < vector1ShaderProperty.rangeValues.y,
                        "Slider property cannot have min be greater than max!");
                }
            }
        }

        [UnityTest]
        public IEnumerator ResetPropertyReference()
        {
            var fieldViewElements = m_GraphEditorView.Query("blackboardFieldView");
            foreach (var visualElement in fieldViewElements.ToList())
            {
                var blackboardFieldView = (BlackboardFieldView) visualElement;
                if (blackboardFieldView == null) continue;

                var shaderInput = (AbstractShaderProperty)blackboardFieldView.shaderInput;
                var originalReferenceName = shaderInput.referenceName;
                var propertyType = shaderInput.GetPropertyTypeString();
                var modifiedReferenceName = $"{propertyType}_Test";
                m_Graph.SanitizeGraphInputReferenceName(shaderInput, modifiedReferenceName);

                // Needed so that the inspector gets triggered and the callbacks and triggers are initialized
                ShaderGraphUITestHelpers.SendMouseEventToVisualElement(blackboardFieldView, EventType.MouseDown);
                ShaderGraphUITestHelpers.SendMouseEventToVisualElement(blackboardFieldView, EventType.MouseUp);

                // Update menu so reset reference option is available
                blackboardFieldView.UpdateRightClickMenu();

                // Wait a frame for the inspector updates to trigger
                yield return null;

                // Cannot actually spawn the right click menu for testing due to ContextMenuManipulators spawning
                // an OS level menu that steals focus from Editor and preventing any future events from being processed

                // Instead, we trigger the action directly from code, for now...
                blackboardFieldView.ResetReferenceAction();

                if (shaderInput.referenceName != originalReferenceName)
                    Assert.Fail("Failed to reset reference name to original value.");
            }
        }
    }
}
