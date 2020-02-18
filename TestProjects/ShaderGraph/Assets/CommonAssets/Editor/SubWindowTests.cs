using System.Collections;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/* Changes:
 * Made ShaderGraphImporterEditor.ShowGraphEditWindow public
 * Made MaterialGraphEditWindow.graphEditorView public
 * Altered MasterPreviewView.OnMouseDragPreviewMesh slightly
 */

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class SubWindowTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/SubWindow.shadergraph";

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        public void OpenGraphWindow()
        {
            // TODO: Make this its own test
//            if (ShaderGraphImporterEditor.ShowGraphEditWindow("HelloWorld"))
//            {
//                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow return true on a Shader Graph that does not exists");
//            }

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

            // TODO: Probably don't want to make this public
            m_GraphEditorView = m_Window.graphEditorView;
        }

        [SetUp]
        public void OpenWindow()
        {
            OpenGraphWindow();
        }

        [TearDown]
        public void CloseWindow()
        {
            m_Window.graphObject = null; // Don't spawn ask-to-save dialog
            m_Window.Close();
        }

        private void ToggleSubWindows(bool showBlackboard, bool showPreview)
        {
            m_GraphEditorView.viewSettings.isBlackboardVisible = showBlackboard;
            m_GraphEditorView.viewSettings.isPreviewVisible = showPreview;

            m_GraphEditorView.UserViewSettingsChangeCheck(0);

            // TODO: As a team decide if we are going to make functions internal or use reflection inside of tests.
            // m_GraphEditorView.UpdateSubWindowsVisibility(); // Needs to be non-private
            MethodInfo updateVisibility = typeof(GraphEditorView).GetMethod("UpdateSubWindowsVisibility",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            updateVisibility.Invoke(m_GraphEditorView, null);
        }

        [UnityTest]
        public IEnumerator ToggleSubWindows()
        {
            Blackboard blackboard;
            MasterPreviewView masterPreviewView;

            // Both
            ToggleSubWindows(true, true);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(blackboard, Is.Not.Null, "Blackboard is not visible when it should be.");
            Assert.That(masterPreviewView, Is.Not.Null, "MasterPreviewView is not visible when it should be.");

            // Neither
            ToggleSubWindows(false, false);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(blackboard, Is.Null, "Blackboard remained visible when it should not be.");
            Assert.That(masterPreviewView, Is.Null, "MasterPreviewView remained visible when it should not be.");

            // Blackboard Only
            ToggleSubWindows(true, false);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(blackboard, Is.Not.Null, "Blackboard is not visible when it should be.");
            Assert.That(masterPreviewView, Is.Null, "MasterPreviewView remained visible when it should not be.");

            // Preview Only
            ToggleSubWindows(false, true);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(masterPreviewView, Is.Not.Null, "MasterPreviewView is not visible when it should be.");
            Assert.That(blackboard, Is.Null, "Blackboard remained visible when it should not be.");
        }

        private IEnumerator ToggleSubWindowsThenCloseThenReopen(bool showBlackboard, bool showPreview)
        {
            ToggleSubWindows(showBlackboard, showPreview);
            yield return null;

            CloseWindow();
            yield return null;

            OpenGraphWindow();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ToggleSubWindowsRememberedAfterAfterCloseAndReopen()
        {
            yield return ToggleSubWindowsThenCloseThenReopen(true, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "0: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "0: MasterPreviewView is not visible when it should be.");

            yield return ToggleSubWindowsThenCloseThenReopen(true, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "1: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "1: MasterPreviewView is not visible when it should be.");

            yield return ToggleSubWindowsThenCloseThenReopen(true, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "2: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "2: MasterPreviewView is visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(true, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "3: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "3: MasterPreviewView is visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "4: Blackboard is visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "4: MasterPreviewView is not visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "5: Blackboard is visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "5: MasterPreviewView is not visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "6: Blackboard is visible when it should not be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "6: MasterPreviewView is visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "7: Blackboard is visible when it should not be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "7: MasterPreviewView is visible when it should not be.");
        }

        [UnityTest]
        public IEnumerator SubWindowLocationRememberedAfterCloseAndReopen()
        {
            yield return TestBlackboardLocation(new Rect(50.0f, 50.0f, 160.0f, 160.0f));
            yield return TestBlackboardLocation(new Rect(80050.0f, 50.0f, 220.0f, 240.0f));
            yield return TestBlackboardLocation(new Rect(50.0f, 90050.0f, 220.0f, 240.0f));
            yield return TestBlackboardLocation(new Rect(80050.0f, 90050.0f, 220.0f, 240.0f));
            yield return TestBlackboardLocation(new Rect(50.0f, -50.0f, 230.0f, 230.0f));
        }

        // Only works for Blackboard... for now. (Plan is to make Internal Inspector, Blackboard 2.0, and MasterPreview use the same SubWindow class someday)
        private IEnumerator TestBlackboardLocation(Rect blackboardRect)
        {
            ToggleSubWindows(true, true);

            Blackboard blackboard = m_GraphEditorView.blackboardProvider.blackboard;
            MasterPreviewView masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            blackboard.SetPosition(blackboardRect);
            yield return null;

            CloseWindow();
            yield return null;
            OpenGraphWindow();
            yield return null;

            blackboard = m_GraphEditorView.blackboardProvider.blackboard;

            // Keep inside the GraphEditor in the same way as expected
            Rect editorViewContainer = m_GraphEditorView.graphView.contentContainer.layout;
            if (blackboardRect.x + blackboardRect.width > editorViewContainer.width)
                blackboardRect.x = editorViewContainer.width - blackboardRect.width;
            if (blackboardRect.y + blackboardRect.height > editorViewContainer.height)
                blackboardRect.y = editorViewContainer.height - blackboardRect.height;
            if (blackboardRect.x < 0)
                blackboardRect.x = 0;
            if (blackboardRect.y < 0)
                blackboardRect.y = 0;

            // Using approximately instead of exact comparisons, which is why we don't use (blackboard.layout == blackboardRect)
            Assert.That(Mathf.Approximately(blackboard.layout.x, blackboardRect.x), "Blackboard did not remember location, x differs: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
            Assert.That(Mathf.Approximately(blackboard.layout.y, blackboardRect.y), "Blackboard did not remember location, y differs: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
            Assert.That(Mathf.Approximately(blackboard.layout.width, blackboardRect.width), "Blackboard did not remember width: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
            Assert.That(Mathf.Approximately(blackboard.layout.height, blackboardRect.height), "Blackboard did not remember height: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
        }





        /*
        private Texture2D ConvertTextureFromRenderTo2D(RenderTexture renderTexture)
        {
            // texRef is your Texture2D
            // You can also reduice your texture 2D that way
            RenderTexture newRenderTexture = new RenderTexture(renderTexture.width, renderTexture.height, 0);
            RenderTexture.active = newRenderTexture;

            // Copy your texture ref to the render texture
            Graphics.Blit(renderTexture, newRenderTexture);

            // Now you can read it back to a Texture2D if you care
            Texture2D texture2D = new Texture2D(newRenderTexture.width, newRenderTexture.height, TextureFormat.RGB24, true);
            texture2D.ReadPixels(new Rect(0, 0, newRenderTexture.width, newRenderTexture.height), 0, 0, false);

            texture2D.Apply();

            return texture2D;
        }

        private void ChangePreviewMesh(MasterPreviewView masterPreviewView, string meshName)
        {
            MethodInfo methodInfo = typeof(MasterPreviewView).GetMethod("ChangePrimitiveMesh",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            object[] parms = { meshName };
            methodInfo.Invoke(masterPreviewView, parms);
        }

        private void ChangePreviewRotation(MasterPreviewView masterPreviewView, Vector2 mouseDrag)
        {
            MethodInfo methodInfo = typeof(MasterPreviewView).GetMethod("OnMouseDragPreviewMesh",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            object[] parms = { mouseDrag };
            methodInfo.Invoke(masterPreviewView, parms);
        }

        [UnityTest]
        public IEnumerator MasterPreviewImageComparison_Default()
        {
            return MasterPreviewImageComparison("Sphere");
        }

        [UnityTest]
        public IEnumerator MasterPreviewImageComparison_Cube_Rotated()
        {
            return MasterPreviewImageComparison("Cube", new Vector2(45.0f, 35.0f));
        }

        private IEnumerator MasterPreviewImageComparison(string mesh = null, Vector2? mouseDrag = null)
        {
            ToggleSubWindows(true, true);
            yield return null;

            MasterPreviewView masterPreview = m_GraphEditorView.Q<MasterPreviewView>();
            Assert.That(masterPreview, Is.Not.Null, "MasterPreviewView is not visible when it should be");

            Image image = masterPreview.Q<Image>();
            Assert.That(masterPreview, Is.Not.Null, "Could not find MasterPreview Image");

            // Change the mesh, rotation, scale, ect.
            bool nonDefaultPreview = false;
            if (mesh != null)
            {
                ChangePreviewMesh(masterPreview, mesh);

                nonDefaultPreview = true;
            }
            if (mouseDrag.HasValue)
            {
                yield return null;
                ChangePreviewRotation(masterPreview, mouseDrag.Value);

                nonDefaultPreview = true;
            }
            if (nonDefaultPreview)
            {
                yield return null;
            }

            // Get the current render texture
            RenderTexture renderTexture = (RenderTexture)image.image;
            Assert.That(renderTexture, Is.Not.Null, "Master Preview's Image's Texture (as RenderTexture) is null");

            // Assert.That(renderTexture.width >= 30, "RenderTexture is very small: has incorrect size");

            Texture2D texture2D = ConvertTextureFromRenderTo2D(renderTexture);
            Assert.That(texture2D, Is.Not.Null, "Master Preview's Image's Texture could not be converted to Texture2D");

            // Get the desired file name
            string fileName = "MasterPreview";
            if (mesh != null)
            {
                fileName += "_" + mesh;
            }
            if (mouseDrag.HasValue)
            {
                fileName += "_" + mouseDrag.Value.x + "_" + mouseDrag.Value.y;
            }
            fileName += ".png";

            Texture2D referenceImage = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/ReferenceImages/" + fileName, typeof(Texture2D));

            if (referenceImage == null)
            {
                // Create the file before failing
                byte[] bytes;
                bytes = texture2D.EncodeToPNG();
                string finalPath = Application.dataPath + "/ActualImages/" + fileName;
                System.IO.File.WriteAllBytes(finalPath, bytes);

                yield return null;
                yield return null;

                Assert.Fail("ReferenceImage ReferenceImages/" + fileName + " not found, creating one at " + finalPath);
            }

            // Compare
            ImageAssert.AreEqual(texture2D, referenceImage);
        }

        // TODO: Can [UnityTest] not have TestCase(s)? Would be preferable.
        // TODO: This could fail if a Shader Graph opens with an asterisk (which it shouldn't...)
        [Test]
        [TestCase(1.0)]
        [TestCase(1.3)] // TODO: this shouldn't be passing!
        [TestCase(0.5)]
        [TestCase(4.435345939839333450222342)]
        [TestCase(-1.0)]
        public void ChangeDefaultValueSubNodeValueTriggersTitleAsterisk(double newValue)
        {
            // OpenGraphWindow();

            MaterialGraphView graphView = m_GraphEditorView.Q<MaterialGraphView>();
            Assert.That(graphView, Is.Not.Null);

            MaterialNodeView masterNodeView = graphView.Q<MaterialNodeView>();
            Assert.That(masterNodeView, Is.Not.Null);

            // TODO: Make sure that we find the right field, and not just the first found.
            FloatField floatField = masterNodeView.Q<FloatField>();
            Assert.That(floatField, Is.Not.Null);

            double oldValue = floatField.value;

            // This means that the test failed to open up a fresh window
            Assert.That(oldValue == 1.0, "Test did not open up a fresh graph, or couldn't find Alpha's Default-Value-Sub-Node.");

            floatField.value = newValue;

            // yield return null;
            UpdateWindow();

            if (oldValue != newValue)
            {
                Assert.That(m_Window.titleContent.text.Contains("*"), "Graph-Is-Dirty-Title-Asterisk expected, yet was absent");
            }
            else
            {
                Assert.That(!m_Window.titleContent.text.Contains("*"), "Graph-Is-Dirty-Title-Asterisk not expected, yet was present");
            }

            // TODO: This should be in TearDown (after moving to another class)
            // This is a (hacky or acceptable?) means to stop the "Do-You-Want-To-Save" Dialog from coming up.
            m_Window.graphObject = null;
        }
        */

    }
}
