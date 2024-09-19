using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;


namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class GroupTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/Group.shadergraph";
        GraphData m_Graph;

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();
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

        class TestExecuteCommandEvent : ExecuteCommandEvent
        {
            public void SetCommandName(string command)
            {
                commandName = command;
            }
        }

        [UnityTest]
        public IEnumerator TestEmptyGroupPastePosition()
        {
            var materialGraphView = m_GraphEditorView.graphView;
			var materialGraphViewType = typeof(MaterialGraphView);

            var cutMethodInfo = materialGraphViewType.GetMethod("CutSelectionCallback",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            var pasteMethodInfo = materialGraphViewType.GetMethod("PasteCallback",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			
            var mousePositionPropertyInfo = materialGraphViewType.GetProperty("cachedMousePosition", 
                    typeof(Vector2));

            Assert.NotNull(cutMethodInfo, "CutSelectionCallback method not found.");
            Assert.NotNull(pasteMethodInfo, "PasteCallback method not found.");
            Assert.NotNull(mousePositionPropertyInfo, "cachedMousePosition property not found.");

            var groupList = materialGraphView.Query<ShaderGroup>()
                .Where(x => x.userData.title == "Empty Group")
                .ToList();

            Assert.AreEqual(1, groupList.Count());    

                
            
            materialGraphView.ClearSelection();
            materialGraphView.AddToSelection(groupList[0]);

            cutMethodInfo.Invoke(materialGraphView, null);
            yield return null;

            groupList = materialGraphView.Query<ShaderGroup>()
                .Where(x => x.userData.title == "Empty Group")
                .ToList();

            Assert.AreEqual(0, groupList.Count());

            mousePositionPropertyInfo.SetValue(materialGraphView, new Vector2(100,100));
            pasteMethodInfo.Invoke(materialGraphView, null);
            yield return null;

            mousePositionPropertyInfo.SetValue(materialGraphView, new Vector2(100,200));
            pasteMethodInfo.Invoke(materialGraphView, null);
            yield return null;

            groupList = materialGraphView.Query<ShaderGroup>()
                .Where(x => x.userData.title == "Empty Group")
                .ToList();
            
            Assert.AreEqual(2, groupList.Count());

            Assert.AreNotEqual(groupList[0].GetPosition().position, groupList[1].GetPosition().position, 
                "When the cursor position changes, the pasted group's position should be adjusted accordingly");
        }

    }
}
