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
using UnityEngine.Events;

namespace UnityEngine.Experimental.Rendering
{
    public class TestResultWindow : EditorWindow
    {
        private Texture2D templateImage;
        private Texture2D resultImage;
        private GUIContent diffContent;

        private Material diffMaterial;
        private float minDiff = 0.45f;
        private float maxDiff = 0.55f;
        private int diffStyle = 5;
        private string[] diffStylesList = new string[] {"Red", "Green", "Blue", "RGB", "Greyscale", "Heatmap"};
        private int[] diffStylesValues = new int[] {0, 1, 2, 3, 4, 5};

        private int topBarHeight = 20;
        private int leftBarWidth = 300;

        private bool testOKOrNotRun = false;

        private UnityEngine.Object sceneAsset;
        private string templateLocation;
        private string misMatchLocationResult;
        private string misMatchLocationTemplate;

        private GUIContent reloadContent = new GUIContent() {text = "Reload 🗘", tooltip = "Reload results."};
        private GUIContent wipeResultContent = new GUIContent() {text = "Wipe ⎚", tooltip = "Wipe results."};
        private GUIContent deleteTemplateContent = new GUIContent() {text = "Delete 🗑", tooltip = "Delete template."};

        private TestResultTreeView testResultTreeView;
        private TestResultViewItem testResultViewItem;

        [MenuItem("Internal/GraphicTest Tools/Result Window")]
        public static void OpenWindow()
        {
            OpenWindow(null);
        }

        public static void OpenWindow(SceneAsset _sceneAsset)
        {
            TestResultWindow window = GetWindow<TestResultWindow>();
            window.minSize = new Vector2(800f, 800f);

            window.testResultTreeView = new TestResultTreeView(new TreeViewState());
            window.testResultTreeView.onSceneSelect += window.Reload;
            window.Reload(_sceneAsset);
        }

        private void Reload(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name.Remove(name.Length-6, 6));
            if (guids.Length < 1)
            {
                Reload();
            }
            else
            {
                SceneAsset scene = null;
                int i = 0;

                while ((scene == null) && (i < guids.Length))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    i++;
                }
                
                Reload(scene);
            }
        }

        private void Reload(SceneAsset _sceneAsset = null)
        {
            sceneAsset = _sceneAsset;

            if (sceneAsset == null) return;

            if (templateImage != null) DestroyImmediate(templateImage);
            string tmp = "";
            if (sceneAsset != null)
            {
                templateImage = TestFrameworkTools.GetTemplateImage(sceneAsset, ref tmp);
                templateImage.filterMode = FilterMode.Point;
                templateImage.mipMapBias = -10;
                templateImage.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(templateImage);
            }


            string templatePath = Path.Combine( TestFrameworkTools.s_RootPath, "ImageTemplates");
            templateLocation = Path.Combine(templatePath, string.Format("{0}.{1}", tmp, "png"));

            if (diffContent == null) diffContent = new GUIContent();

            diffContent.image = templateImage;

            if (diffMaterial == null)
            {
                diffMaterial = new Material(Shader.Find("GraphicTests/ComparerShader"));
                diffMaterial.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(diffMaterial);
                diffMaterial.SetFloat("_CorrectGamma", 1f);
                diffMaterial.SetFloat("_FlipV2", 0f);
            }

            // Search for fail image if it exists.
            var failedPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "SRP_Failed");
            misMatchLocationResult = Path.Combine(failedPath, string.Format("{0}.unity.png", sceneAsset.name));
            misMatchLocationTemplate = misMatchLocationResult.Insert(misMatchLocationResult.Length - 10, ".template");

            if (resultImage != null && resultImage != Texture2D.blackTexture) DestroyImmediate(resultImage);
            if (File.Exists(misMatchLocationResult))
            {
                resultImage = new Texture2D(templateImage.width, templateImage.height, TextureFormat.ARGB32, false);
                resultImage.filterMode = FilterMode.Point;
                resultImage.mipMapBias = -10;
                resultImage.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(resultImage);

                byte[] resultBytes = File.ReadAllBytes(misMatchLocationResult);
                resultImage.LoadImage(resultBytes);
                testOKOrNotRun = false;

                minDiff = 0.45f;
                maxDiff = 0.55f;
            }
            else
            {
                testOKOrNotRun = true;
                resultImage = Texture2D.blackTexture;

                minDiff = 1f;
                maxDiff = 1f;
            }

            ApplyValues();

            diffMaterial.SetTexture("_CompareTex", resultImage);

            testResultTreeView.Reload();
        }

        private void OnDisable()
        {
            DestroyImmediate(templateImage);
            DestroyImmediate(resultImage);
            DestroyImmediate(diffMaterial);
        }

        private void OnGUI()
        {
            // tree view
            testResultTreeView.OnGUI(new Rect(0, 0, leftBarWidth, position.height));

            if (sceneAsset == null)
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
                            Reload(sceneAsset.name+".unity");

                        if (GUILayout.Button(wipeResultContent))
                        {
                            if (File.Exists(misMatchLocationResult))
                                File.Delete(misMatchLocationResult);
                            if (File.Exists(misMatchLocationTemplate))
                                File.Delete(misMatchLocationTemplate);
                        }

                        if (GUILayout.Button(deleteTemplateContent))
                        {
                            if (File.Exists(templateLocation))
                            {
                                File.Delete(templateLocation);
                                AssetDatabase.Refresh();
                            }
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

                        GUILayout.Label("Diff. type: ");
                        diffStyle = EditorGUILayout.IntPopup(diffStyle, diffStylesList, diffStylesValues,
                            GUILayout.Width(200f));
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

                Rect textureRect = new Rect(leftBarWidth, topBarHeight * 3, position.width - leftBarWidth,
                    position.height - topBarHeight * 3);
                EditorGUI.DrawPreviewTexture(textureRect, templateImage, diffMaterial, ScaleMode.ScaleToFit, 0, 0);
            }
        }

        private void ApplyValues()
        {
            float resultSplit = maxDiff - minDiff;
            float split = (minDiff + maxDiff) / 2f;
            split = (split - 0.5f * resultSplit) / (1 - resultSplit); //  inverse the lerp used in the shader

            diffMaterial.SetFloat("_Split", split);
            diffMaterial.SetFloat("_ResultSplit", resultSplit);
            diffMaterial.SetInt("_Mode", diffStyle);
        }

        public class TestResultTreeView : TreeView
        {
            public delegate void OnSceneSelect(string name);
            public OnSceneSelect onSceneSelect;

            delegate void OnNewSceneSelect(SceneAsset newScene);

            public TestResultTreeView(TreeViewState state) : base(state)
            {
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem(0, -1, "Root");

                int nextID = 1;

                TreeViewItem hdrpParent = new TreeViewItem(nextID, 0, "HDRP");
                ++nextID;
                root.AddChild(hdrpParent);
                TreeViewItem lwrpParent = new TreeViewItem(nextID, 0, "LWRP");
                ++nextID;
                root.AddChild(lwrpParent);

                Dictionary<string, TreeViewItem> hdrpFolders = new Dictionary<string, TreeViewItem>();

                foreach (TestFrameworkTools.TestInfo info in TestFrameworkTools.CollectScenes.HDRP)
                {
                    TreeViewItem parent = hdrpParent;

                    string folder = Path.GetDirectoryName( info.templatePath ).Split("\\"[0]).Last();
                    if (hdrpFolders.ContainsKey(folder))
                    {
                        parent = hdrpFolders[folder];
                    }
                    else
                    {
                        parent = new TreeViewItem(nextID, 0, folder);
                        nextID++;

                        hdrpParent.AddChild(parent);
                        hdrpFolders.Add(folder, parent);
                    }

                    var prjRelativeGraphsPath = TestFrameworkTools.s_Path.Aggregate(TestFrameworkTools.s_RootPath, Path.Combine);
                    var filePath = Path.Combine(prjRelativeGraphsPath, info.relativePath);

                    filePath = string.Format("Assets{0}", filePath.Replace(Application.dataPath, "") );

                    SceneAsset sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(filePath);

                    TestResultViewItem item = new TestResultViewItem(nextID, 0, info.name, sceneObject);
                    nextID++;
                    parent.AddChild(item);
                }

                Dictionary<string, TreeViewItem> lwrpFolders = new Dictionary<string, TreeViewItem>();

                foreach (TestFrameworkTools.TestInfo info in TestFrameworkTools.CollectScenes.LWRP)
                {
                    TreeViewItem parent = lwrpParent;

                    string folder = Path.GetDirectoryName( info.templatePath ).Split("\\"[0]).Last();
                    if (lwrpFolders.ContainsKey(folder))
                    {
                        parent = lwrpFolders[folder];
                    }
                    else
                    {
                        parent = new TreeViewItem(nextID, 0, folder);
                        nextID++;

                        lwrpParent.AddChild(parent);
                        lwrpFolders.Add(folder, parent);
                    }

                    var prjRelativeGraphsPath = TestFrameworkTools.s_Path.Aggregate(TestFrameworkTools.s_RootPath, Path.Combine);
                    var filePath = Path.Combine(prjRelativeGraphsPath, info.relativePath);

                    filePath = string.Format("Assets{0}", filePath.Replace(Application.dataPath, ""));

                    SceneAsset sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(filePath);

                    TestResultViewItem item = new TestResultViewItem(nextID, 0, info.name, sceneObject);
                    nextID++;
                    parent.AddChild(item);
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

                onSceneSelect(item.displayName);
            }
        }

        [Serializable]
        public class TestResultViewItem : TreeViewItem
        {
            public SceneAsset sceneObject;

            public TestResultViewItem(int id, int depth, string displayName, SceneAsset sceneObject)
            {
                this.id = id;
                this.depth = depth;
                this.displayName = displayName;
                this.sceneObject = sceneObject;
            }
        }
    }
}
