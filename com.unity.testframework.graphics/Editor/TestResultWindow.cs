using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.TestTools.Graphics;
using UnityEditor.TestTools.Graphics;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering
{
    public class TestResultWindow : EditorWindow
    {
        private Texture2D expectedImage;
        private Texture2D actualImage;
        private Texture2D diffImage;

        private Material m_displayMaterial;
        private Material displayMaterial
        {
            get
            {
                if (m_displayMaterial == null)
                {
                    m_displayMaterial = new Material(Shader.Find("Hidden/GraphicTests/ResultDisplay"));
                }
                return m_displayMaterial;
            }
        }

        private string tmpPath;

        private float minDiff = 0.45f;
        private float maxDiff = 0.55f;

        private int topBarHeight = 20;
        private int leftBarWidth = 300;

        private bool testOKOrNotRun = false;

        private GraphicsTestCase testCase;

        private GUIContent reloadContent = new GUIContent() {text = "Reload Results 🗘", tooltip = "Reload results."};
        private GUIContent wipeResultContent = new GUIContent() {text = "Wipe Results ⎚", tooltip = "Wipe results."};
        private GUIContent deleteTemplateContent = new GUIContent() {text = "Delete Expected 🗑", tooltip = "Delete expected."};
        private GUIContent updateTemplateContent = new GUIContent() {text = "Update expected", tooltip = "Update expected with current result."};

        private TestResultTreeView _testResultTreeView;

        private TestResultTreeView testResultTreeView
        {
            get
            {
                if (_testResultTreeView == null)
                {
                    _testResultTreeView = new TestResultTreeView(new TreeViewState());
                    _testResultTreeView.onSceneSelect += Reload;
                }

                return _testResultTreeView;
            }
        }

        [MenuItem("Tests/Graphics Test Image Viewer")]
        public static void OpenWindow()
        {
            OpenWindow( null );
        }

        public static void OpenWindow( GraphicsTestCase _testCase )
        {
            TestResultWindow window = GetWindow<TestResultWindow>(false, "Gfx Image Viewer");
            window.minSize = new Vector2(800f, 800f);

            window.Reload( _testCase );
        }

        private void CheckDataObjects()
        {
            GetImages();
        }

        private void Reload( GraphicsTestCase _testCase = null)
        {
            testCase = _testCase;

            if (testCase == null) return;

            GetImages();

            if (expectedImage == null || actualImage == null )
            {
                testOKOrNotRun = true;
                minDiff = maxDiff = 1f;
            }
            else
            {
                testOKOrNotRun = false;
                minDiff = .45f;
                maxDiff = .55f;
            }

            ApplyValues();

            testResultTreeView.Reload();
        }

        private void OnDisable()
        {
        }

        private void OnGUI()
        {
            // tree view
            testResultTreeView.OnGUI(new Rect(0, 0, leftBarWidth, position.height));

            //Reload(testCase);
            GetImages(testCase);

            if (testCase == null)
            {
                GUI.Label(new Rect(leftBarWidth, 0, position.width - leftBarWidth, position.height), "Select a test to display");
            }
            else if (testCase.ExpectedImage) {
                // result view
                GUILayout.BeginArea(new Rect(leftBarWidth, 0, position.width - leftBarWidth, position.height));
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal(GUILayout.Height(topBarHeight));
                    {
                        if (GUILayout.Button(reloadContent))
                            Reload(testCase);

                        if (GUILayout.Button(wipeResultContent)) {
                            DeleteResults();
                        }

                        if (GUILayout.Button(deleteTemplateContent)) {
                            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(expectedImage));
                        }

                        if (GUILayout.Button(updateTemplateContent)) {
                            UpdateExpected();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(GUILayout.Height(topBarHeight));
                    {
                        GUILayout.FlexibleSpace();
                        if (testOKOrNotRun)
                        {
                            GUI.enabled = false;
                            GUI.color = Color.green;
                            GUILayout.Label("Test OK or not run.");
                            GUI.color = Color.white;
                            GUILayout.FlexibleSpace();
                        }

                        if (GUILayout.Button("Quick Switch"))
                        {
                            if (maxDiff > 0f)
                            {
                                minDiff = 0f;
                                maxDiff = 0f;
                            }
                            else
                            {
                                minDiff = 1f;
                                maxDiff = 1f;
                            }
                            ApplyValues();
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            minDiff = 0.45f;
                            maxDiff = 0.55f;
                            ApplyValues();
                        }

                        GUILayout.FlexibleSpace();

                        bool b = GUI.enabled;
                        GUI.enabled = true;
                        if (GUILayout.Button("Open Scene"))
                        {
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            EditorSceneManager.OpenScene( testCase.ScenePath , OpenSceneMode.Single);
                        }

                        GUI.enabled = b;

                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.MinMaxSlider(ref minDiff, ref maxDiff, 0f, 1f, GUILayout.Height(topBarHeight));

                    if (EditorGUI.EndChangeCheck()) ApplyValues();

                    // template / diff / result visualisation
                    float w = position.width - leftBarWidth;
                    Color c = GUI.color;

                    Rect rect1 = new Rect(0, topBarHeight * 3, w * minDiff, topBarHeight);
                    Rect rect2 = new Rect(rect1.max.x, rect1.y, w * (maxDiff - minDiff), topBarHeight);
                    Rect rect3 = new Rect(rect2.max.x, rect2.y, w * (1f - maxDiff), topBarHeight);

                    GUI.color = Color.green;
                    if (rect1.width > 0) GUI.Box(rect1, "Template");
                    GUI.color = Color.black;
                    if (rect2.width > 0) GUI.Box(rect2,  "Diff" );
                    GUI.color = Color.blue;
                    if (rect3.width > 0) GUI.Box(rect3, "Result");

                    GUI.color = c;
                }
                GUILayout.EndArea();

                Rect textureRect = new Rect(leftBarWidth, topBarHeight * 4, position.width - leftBarWidth,position.height - topBarHeight * 3);
                GUI.enabled = true;

                CheckDataObjects();

                if (expectedImage != null)
                    EditorGUI.DrawPreviewTexture(textureRect, expectedImage, displayMaterial, ScaleMode.ScaleToFit, 0, 0);
            }
            else if (!actualImage){
                GUILayout.BeginArea(new Rect(leftBarWidth, 0, position.width - leftBarWidth, position.height));
                {
                    if (GUILayout.Button("Generate Expected Image")) {
                        GenerateExpectedImage(testCase);
                        Reload(testCase);
                    }
                }
                GUILayout.EndArea();
            } else {
                GUILayout.BeginArea(new Rect(leftBarWidth, 0, position.width - leftBarWidth, position.height));
                {
                    if (GUILayout.Button("Set expected image")) {
                        SetExpectedImage(actualImage);
                    }
                    Rect textureRect = new Rect(0, topBarHeight * 4, position.width - leftBarWidth, position.height - topBarHeight * 3);
                    GUI.enabled = true;

                    EditorGUI.DrawPreviewTexture(textureRect, actualImage, displayMaterial, ScaleMode.ScaleToFit, 0, 0);
                }
                GUILayout.EndArea();
            }
        }

        private void SetExpectedImage(Texture2D actualImage) {
            var dirName = Path.Combine("Assets/ExpectedImages", string.Format("{0}/{1}/{2}", UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice));
            var path = Path.Combine(dirName, actualImage.name + ".png");

            var bytes = actualImage.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.Refresh();
            Utils.SetupExpectedImageImportSettings(new string[] { path });

            Reload(testCase);
            Repaint();
            OnGUI();
        }

        private Texture2D GetActualImage(GraphicsTestCase testCase) {
            var dirName = Path.Combine("Assets/ActualImages", string.Format("{0}/{1}/{2}", UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice));
            int lastSlashIndex = testCase.ScenePath.LastIndexOf('/') + 1;
            var path = Path.Combine(dirName, testCase.ScenePath.Substring(lastSlashIndex,
                testCase.ScenePath.LastIndexOf(".unity") - lastSlashIndex) + ".png");

            Texture2D res =  AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            return res;
        }

        private void GenerateExpectedImage(GraphicsTestCase testCase) {
            string curScene = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(testCase.ScenePath);

            IEnumerable<Camera> cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
            GraphicsTestSettings settings = FindObjectOfType<GraphicsTestSettings>();
            Texture2D referenceTexture = ImageAssert.GetRenderTextureFromCameras(cameras, TextureFormat.ARGB32, settings.ImageComparisonSettings);

            var dirName = Path.Combine("Assets/ActualImages", string.Format("{0}/{1}/{2}", UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice));
            var path = Path.Combine(dirName, SceneManager.GetActiveScene().name + ".png");

            var bytes = referenceTexture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.Refresh();
            Utils.SetupExpectedImageImportSettings(new string[] { path });

            EditorSceneManager.OpenScene(curScene);

            Reload(testCase);
            Repaint();
            OnGUI();
        }

        private void SaveTempExpectedImage() {

        }

        private void ApplyValues()
        {
            float resultSplit = maxDiff - minDiff;
            float split = (minDiff + maxDiff) / 2f;
            split = (split - 0.5f * resultSplit) / (1 - resultSplit); //  inverse the lerp used in the shader

            displayMaterial.SetTexture("_ResultTex", actualImage);
            displayMaterial.SetTexture("_DiffTex", diffImage);

            displayMaterial.SetFloat("_DiffA", minDiff);
            displayMaterial.SetFloat("_DiffB", maxDiff);
        }

        private void DeleteResults()
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(actualImage));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(diffImage));
        }

        private void UpdateExpected()
        {
            if(expectedImage == null || actualImage == null)
                return;


            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(actualImage), AssetDatabase.GetAssetPath(expectedImage));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DeleteResults();
        }

        public const string ActualImagesRoot = "Assets/ActualImages";

        public bool GetImages( GraphicsTestCase _testCase = null )
        {
            GraphicsTestCase tCase = ( _testCase == null )? testCase : _testCase ;

            if (tCase == null)
            {
                expectedImage = null;
                actualImage = null;
                diffImage = null;
                return false;
            }

            if ( tCase.ExpectedImage == null )
            {
                actualImage = null;
                diffImage = null;
                //return false; // No reference image found
            }

            var colorSpace = UseGraphicsTestCasesAttribute.ColorSpace;
            var platform = UseGraphicsTestCasesAttribute.Platform;
            var graphicsDevice = UseGraphicsTestCasesAttribute.GraphicsDevice;

            var actualImagesDir = Path.Combine(ActualImagesRoot, string.Format("{0}/{1}/{2}", colorSpace, platform, graphicsDevice));

            var sceneName = Path.GetFileNameWithoutExtension( tCase.ScenePath );

            expectedImage = tCase.ExpectedImage;
            actualImage = AssetDatabase.LoadMainAssetAtPath( Path.Combine(actualImagesDir, sceneName + ".png") ) as Texture2D;
            diffImage = AssetDatabase.LoadMainAssetAtPath( Path.Combine(actualImagesDir, sceneName + ".diff.png") ) as Texture2D;

            foreach( Texture2D image in new Texture2D[]{expectedImage, actualImage, diffImage})
            {
                if (image == null) continue;
                image.filterMode = FilterMode.Point;
                image.mipMapBias = -10;
                image.hideFlags = HideFlags.HideAndDontSave;
            }

            if (actualImage == null && diffImage == null)
                return true;
            else
                return false;
        }

        public class TestResultTreeView : TreeView
        {
            public delegate void OnSceneSelect( GraphicsTestCase testCase );
            public OnSceneSelect onSceneSelect;

            public TestResultTreeView(TreeViewState state) : base(state)
            {
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem(0, -1, "Root");

                int nextID = 1;

                IEnumerable<GraphicsTestCase> testCases = new EditorGraphicsTestCaseProvider().GetTestCases();

                foreach ( var i_testCase in testCases )
                {
                    TestResultViewItem item = new TestResultViewItem(nextID, 0, Path.GetFileNameWithoutExtension( i_testCase.ScenePath ) , i_testCase);
                    nextID++;
                    root.AddChild(item);
                }

                SetupDepthsFromParentsAndChildren(root);

                return root;
            }

            protected override bool CanMultiSelect(TreeViewItem item) { return false; }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (selectedIds.Count < 1 ) return;

                TreeViewItem item = FindItem(selectedIds[0], rootItem);

                if ( item.hasChildren ) return; // not a scene (final) item

                onSceneSelect( ( item as TestResultViewItem ).testCase );
            }

            protected override void DoubleClickedItem(int id)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene( ( FindItem(id, rootItem) as TestResultViewItem ).testCase.ScenePath , OpenSceneMode.Single);
            }
        }


        [Serializable]
        public class TestResultViewItem : TreeViewItem
        {
            public GraphicsTestCase testCase;

            public TestResultViewItem(int id, int depth, string displayName, GraphicsTestCase testCase)
            {
                this.id = id;
                this.depth = depth;
                this.displayName = displayName;
                this.testCase = testCase;
                if (!this.testCase.ExpectedImage)
                    icon = Resources.Load <Texture2D>("X");
            }
        }
    }
}
