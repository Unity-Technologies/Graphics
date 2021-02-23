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
            foreach (AbstractShaderProperty property in m_Collector.properties)
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
                var blackboardFieldView = (BlackboardFieldView)visualElement;
                if (blackboardFieldView == null) continue;

                var shaderInput = (AbstractShaderProperty)blackboardFieldView.shaderInput;
                var originalReferenceName = shaderInput.referenceName;
                var propertyType = shaderInput.GetPropertyTypeString();
                var modifiedReferenceName = $"{propertyType}_Test";
                shaderInput.SetReferenceNameAndSanitizeForGraph(m_Graph, modifiedReferenceName);

                Assert.IsTrue(shaderInput.referenceName != originalReferenceName);

                // Needed so that the inspector gets triggered and the callbacks and triggers are initialized
                ShaderGraphUITestHelpers.SendMouseEventToVisualElement(blackboardFieldView, EventType.MouseDown);
                ShaderGraphUITestHelpers.SendMouseEventToVisualElement(blackboardFieldView, EventType.MouseUp);

                // Wait a frame for the inspector updates to trigger
                yield return null;

                // Cannot actually spawn the right click menu for testing due to ContextMenuManipulators spawning
                // an OS level menu that steals focus from Editor and preventing any future events from being processed
                // Instead, we trigger the action directly from code, for now...
                shaderInput.ResetReferenceName(m_Graph);

                if (shaderInput.referenceName != originalReferenceName)
                    Assert.Fail("Failed to reset reference name to original value.");
            }
        }

        [Test]
        public void DefaultNamePropertyTest()
        {
            var A = new Vector4ShaderProperty() { displayName = "A" };
            m_Graph.AddGraphInput(A);

            // check that default reference name gets set to match display name
            Assert.IsTrue(A.referenceName == "A");
        }

        [Test]
        public void DuplicatePropertyTest()
        {
            // public void AddGraphInput(ShaderInput input, int index = -1)
            // public void RemoveGraphInput(ShaderInput input)
            // public ShaderInput AddCopyOfShaderInput(ShaderInput source, int insertIndex = -1)

            var B = new Vector4ShaderProperty() { displayName = "B" };

            // add it twice!
            m_Graph.AddGraphInput(B);
            var B2 = m_Graph.AddCopyOfShaderInput(B);
            var B3 = m_Graph.AddCopyOfShaderInput(B);

            // check that both names have been made unique
            Assert.IsTrue(B.displayName != B2.displayName);
            Assert.IsTrue(B.displayName != B3.displayName);
            Assert.IsTrue(B2.displayName != B3.displayName);

            Assert.IsTrue(B.referenceName != B2.referenceName);
            Assert.IsTrue(B.referenceName != B3.referenceName);
            Assert.IsTrue(B2.referenceName != B3.referenceName);

            // set overrides, so that B3 is now called "B"
            B.SetDisplayNameAndSanitizeForGraph(m_Graph, "Q");          // display name "Q"   reference name default "Q"
            B3.SetDisplayNameAndSanitizeForGraph(m_Graph, "B");         // display name "B"   reference name default "B"

            // since reference names should still be using default behavior of tracking display names,
            // B3 ref name should now be called "B"
            Assert.IsTrue(B.referenceName == "Q");
            Assert.IsTrue(B3.referenceName == "B");

            // now let's try overriding the reference names
            B3.SetReferenceNameAndSanitizeForGraph(m_Graph, "B3");       // display name "B"  reference name "B3"
            B.SetReferenceNameAndSanitizeForGraph(m_Graph, "B");        // display name "Q"   reference name "B"
            Assert.IsTrue(B.referenceName == "B");
            Assert.IsTrue(B3.referenceName == "B3");

            // now let's try resetting the reference name
            B3.ResetReferenceName(m_Graph);                     // display name "B",  reference name default (can't be "B" because of collisions)
            Assert.IsTrue(B3.referenceName != "B");

            // let's check everything is still unique
            Assert.IsTrue(B.displayName != B2.displayName);
            Assert.IsTrue(B.displayName != B3.displayName);
            Assert.IsTrue(B2.displayName != B3.displayName);

            Assert.IsTrue(B.referenceName != B2.referenceName);
            Assert.IsTrue(B.referenceName != B3.referenceName);
            Assert.IsTrue(B2.referenceName != B3.referenceName);
        }

        [Test]
        public void SanitizePropertyTest()
        {
            var C = new Vector4ShaderProperty() { displayName = "C" };

            m_Graph.AddGraphInput(C);

            // numbers not allowed
            C.SetReferenceNameAndSanitizeForGraph(m_Graph, "3");
            Assert.IsTrue(C.referenceName == "_3");

            // shaderlab reserved words not allowed (case insensitive)
            C.SetReferenceNameAndSanitizeForGraph(m_Graph, "ColOrMasK");
            Assert.IsTrue(string.Compare(C.referenceName, "colormask", true) != 0);

            // whitespace not allowed
            C.SetReferenceNameAndSanitizeForGraph(m_Graph, " my float value ");
            Assert.IsFalse(C.referenceName.Contains(" "));
        }
    }
}
