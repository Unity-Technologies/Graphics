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

namespace UnityEngine.Experimental.Rendering
{
    public class TestResultWindow : EditorWindow
    {
        private Texture2D templateImage;
        private Texture2D resultImage;
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
        private GUIContent deleteTemplateContent = new GUIContent() {text = "Delete Reference 🗑", tooltip = "Delete reference."};
        private GUIContent updateTemplateContent = new GUIContent() {text = "Update Reference", tooltip = "Update reference with current result."};

        // pouet

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

        [MenuItem("Tests/Result Window")]
        public static void OpenWindow()
        {
            OpenWindow( null );
        }

        public static void OpenWindow( GraphicsTestCase _testCase )
        {
            TestResultWindow window = GetWindow<TestResultWindow>();
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

            //Debug.Log("Show result for : " + _testCase.ScenePath);

            GetImages();

            if (templateImage == null || resultImage == null )
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
            //EditorGUILayout.ObjectField( displayMaterial, typeof(Material), false );

            // tree view
            testResultTreeView.OnGUI(new Rect(0, 0, leftBarWidth, position.height));

            if (testCase == null)
            {
                GUI.Label(new Rect(leftBarWidth, 0, position.width - leftBarWidth, position.height), "Select a test to display");
            }
            else
            {
                // result view
                GUILayout.BeginArea(new Rect(leftBarWidth, 0, position.width - leftBarWidth, position.height));
                {
                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal(GUILayout.Height(topBarHeight));
                    {
                        if (GUILayout.Button(reloadContent))
                            Reload( testCase );

                        if (GUILayout.Button(wipeResultContent))
                        {
                            DeleteResults();
                        }

                        if (GUILayout.Button(deleteTemplateContent))
                        {
                            AssetDatabase.DeleteAsset( AssetDatabase.GetAssetPath(templateImage) );
                        }

                        if (GUILayout.Button(updateTemplateContent))
                        {
                            UpdateReference();
                        }

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

                    Rect rect1 = new Rect(0, topBarHeight * 2, w * minDiff, topBarHeight);
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

                Rect textureRect = new Rect(leftBarWidth, topBarHeight * 3, position.width - leftBarWidth,position.height - topBarHeight * 3);
                GUI.enabled = true;

                CheckDataObjects();

                if (templateImage != null)
                    EditorGUI.DrawPreviewTexture(textureRect, templateImage, displayMaterial, ScaleMode.ScaleToFit, 0, 0);
            }
        }

        private void ApplyValues()
        {
            float resultSplit = maxDiff - minDiff;
            float split = (minDiff + maxDiff) / 2f;
            split = (split - 0.5f * resultSplit) / (1 - resultSplit); //  inverse the lerp used in the shader

            displayMaterial.SetTexture("_ResultTex", resultImage);
            displayMaterial.SetTexture("_DiffTex", diffImage);

            displayMaterial.SetFloat("_DiffA", minDiff);
            displayMaterial.SetFloat("_DiffB", maxDiff);
        }

        private void DeleteResults()
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(resultImage));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(diffImage));
        }

        private void UpdateReference()
        {
            if(templateImage == null || resultImage == null)
                return;

            EditorUtility.CopySerialized( resultImage, templateImage );
            AssetDatabase.SaveAssets();

            DeleteResults();
        }
        
        public const string ActualImagesRoot = "Assets/ActualImages";

        public bool GetImages( GraphicsTestCase _testCase = null )
        {
            GraphicsTestCase tCase = ( _testCase == null )? testCase : _testCase ;

            if (tCase == null)
            {
                templateImage = null;
                resultImage = null;
                diffImage = null;
                return false;
            }

            if ( tCase.ReferenceImage == null )
            {
                resultImage = null;
                diffImage = null;
                return false; // No reference image found
            }

            var colorSpace = UseGraphicsTestCasesAttribute.Provider.ColorSpace;
            var platform = UseGraphicsTestCasesAttribute.Provider.Platform;
            var graphicsDevice = UseGraphicsTestCasesAttribute.Provider.GraphicsDevice;

            var actualImagesDir = Path.Combine(ActualImagesRoot, string.Format("{0}/{1}/{2}", colorSpace, platform, graphicsDevice));

            var sceneName = Path.GetFileNameWithoutExtension( tCase.ScenePath );

            templateImage = tCase.ReferenceImage;
            resultImage = AssetDatabase.LoadMainAssetAtPath( Path.Combine(actualImagesDir, sceneName + ".png") ) as Texture2D;
            diffImage = AssetDatabase.LoadMainAssetAtPath( Path.Combine(actualImagesDir, sceneName + ".diff.png") ) as Texture2D;

            foreach( Texture2D image in new Texture2D[]{templateImage, resultImage, diffImage})
            {
                if (image == null) continue;
                image.filterMode = FilterMode.Point;
                image.mipMapBias = -10;
                image.hideFlags = HideFlags.HideAndDontSave;
            }

            if (resultImage == null && diffImage == null)
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

                //TestResultViewItem testItem = (TestResultViewItem)item;

                //if (testItem!=null) Debug.Log(item.displayName+" : "+testItem.sceneObject);

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
            }
        }
    }
}
