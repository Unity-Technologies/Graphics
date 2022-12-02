using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    internal class LightBatchingDebugger : EditorWindow
    {
        private const string ResourcePath = "Packages/com.unity.render-pipelines.universal/Editor/2D/LightBatchingDebugger/";

        private class LayerBatch
        {
            public List<string> LayerNames = new List<string>();
            public List<UnityEngine.Object> Lights = new List<UnityEngine.Object>();
            public List<UnityEngine.Object> Shadows = new List<UnityEngine.Object>();
            public int batchId;
        }

        [MenuItem("Window/2D/Light Batching Debugger")]
        public static void ShowExample()
        {
            // Open Game View
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            LightBatchingDebugger wnd = GetWindow<LightBatchingDebugger>();
            wnd.titleContent = new GUIContent("Light Batching Debugger");
        }

        VisualElement root => rootVisualElement;

        private static Color[] batchColors = new Color[10];
        private List<LayerBatch> batchList = new List<LayerBatch>();
        private List<int> selectedIndices = new List<int>();
        private ListView batchListView;
        private int lightCount = 0;
        private int shadowCount = 0;

        // Variables used for refresh view
        private bool doRefresh;
        private int cachedSceneHandle;
        private int totalLightCount;
        private int totalShadowCount;
        private Vector3 cachedCamPos;

        ILight2DCullResult lightCullResult
        {
            get
            {
                // Game view main camera
                var renderer = Camera.main?.GetUniversalAdditionalCameraData().scriptableRenderer as Renderer2D;
                var data = renderer?.GetRenderer2DData();
                if (data != null && data.lightCullResult.IsGameView())
                    return data?.lightCullResult;

                return null;
            }
        }

        private bool PopulateData()
        {
            if (lightCullResult == null)
                return false;

            batchList.Clear();

            var layers = Light2DManager.GetCachedSortingLayer();
            var batches = LayerUtility.CalculateBatches(lightCullResult, out var batchCount);

            for (var i = 0; i < batchCount; i++)
            {
                var batchInfo = new LayerBatch
                {
                    batchId = i
                };

                var batch = batches[i];

                // Get the lights
                foreach (var light in lightCullResult.visibleLights)
                {
                    // If the lit layers are different, or if they are lit but this is a shadow casting light then don't batch.
                    if (light.IsLitLayer(batch.startLayerID))
                    {
                        batchInfo.Lights.Add(light);
                    }
                }

                // Get the shadows
                var visibleShadows = lightCullResult.visibleShadows.SelectMany(x => x.GetShadowCasters());
                foreach (var shadowCaster in visibleShadows)
                {
                    if (shadowCaster.IsShadowedLayer(batch.startLayerID))
                        batchInfo.Shadows.Add(shadowCaster);
                }

                for (var batchIndex = batch.startIndex; batchIndex <= batch.endIndex; batchIndex++)
                {
                    batchInfo.LayerNames.Add(layers[batchIndex].name);
                }

                batchList.Add(batchInfo);
            }

            return true;
        }

        private VisualElement MakePill(UnityEngine.Object obj)
        {
            var bubble = new Button();
            bubble.AddToClassList("Pill");
            bubble.text = obj.name;

            bubble.clicked += () =>
            {
                Selection.activeObject = obj;
            };

            return bubble;
        }

        private VisualElement GetInfoView()
        {
            // Hide initial prompt
            DisplayInitialPrompt(false);

            return root.Query<VisualElement>("InfoView").First();
        }

        private void ViewBatch(int index)
        {
            if (index >= batchList.Count())
                return;

            var infoView = GetInfoView();

            var batch1 = batchList[index];

            var title = root.Query<Label>("InfoTitle").First();
            title.text = $"<b>Batch {batch1.batchId}</b>" + " selected. Select any two adjacent batches to compare.";

            var title2 = root.Query<Label>("InfoTitle2").First();
            title2.text = "";

            // Add Light Pill VisualElements
            var lightLabel1 = infoView.Query<Label>("LightLabel1").First();
            lightLabel1.text = $"Lights in <b>Batch {batch1.batchId}:</b>";

            if (batch1.Lights.Count() == 0)
                lightLabel1.text += "\n\nNo lights found.";

            var lightBubble1 = infoView.Query<VisualElement>("LightBubble1").First();
            lightBubble1.Clear();

            foreach (var obj in batch1.Lights)
            {
                if(obj != null)
                    lightBubble1.Add(MakePill(obj));
            }

            var lightLabel2 = infoView.Query<Label>("LightLabel2").First();
            lightLabel2.text = "";

            var lightBubble2 = infoView.Query<VisualElement>("LightBubble2").First();
            lightBubble2.Clear();

            // Add Shadow Caster Pill VisualElements
            var shadowLabel1 = infoView.Query<Label>("ShadowLabel1").First();
            shadowLabel1.text = $"Shadow Casters in <b>Batch {batch1.batchId}:</b>";

            if (batch1.Shadows.Count() == 0)
                shadowLabel1.text += "\n\nNo shadow casters found.";

            var shadowBubble1 = infoView.Query<VisualElement>("ShadowBubble1").First();
            shadowBubble1.Clear();

            foreach (var obj in batch1.Shadows)
            {
                if (obj != null)
                    shadowBubble1.Add(MakePill(obj));
            }

            var shadowLabel2 = infoView.Query<Label>("ShadowLabel2").First();
            shadowLabel2.text = "";

            var shadowBubble2 = infoView.Query<VisualElement>("ShadowBubble2").First();
            shadowBubble2.Clear();

            lightCount = batch1.Lights.Count;
            shadowCount = batch1.Shadows.Count;
        }

        private void CompareBatch(int index1, int index2)
        {
            // Each editor window contains a root VisualElement object
            var infoView = GetInfoView();

            LayerBatch batch1;
            LayerBatch batch2;

            if (batchList[index1].batchId < batchList[index2].batchId)
            {
                batch1 = batchList[index1];
                batch2 = batchList[index2];
            }
            else
            {
                batch1 = batchList[index2];
                batch2 = batchList[index1];
            }

            // Do batch comparisons
            var lightSet1 = batch1.Lights.Except(batch2.Lights);
            var lightSet2 = batch2.Lights.Except(batch1.Lights);
            var shadowSet1 = batch1.Shadows.Except(batch2.Shadows);
            var shadowSet2 = batch2.Shadows.Except(batch1.Shadows);

            // Change InfoTitle description when comparing batches
            var title = root.Query<Label>("InfoTitle").First();
            title.text = $"Comparing <b>Batch {batch1.batchId}</b> and <b>Batch {batch2.batchId}</b>.";

            var title2 = root.Query<Label>("InfoTitle2").First();
            title2.text = $"To batch <b>Batch {batch1.batchId}</b> and <b>Batch {batch2.batchId}</b>, ensure that the Sorting Layers in both batches share the same set of Lights and Shadow Casters.";

            // Light batch comparison
            var lightLabel1 = infoView.Query<Label>("LightLabel1").First();
            lightLabel1.text = $"Lights only in <b>Batch {batch1.batchId}:</b>";

            if (lightSet1.Count() == 0)
                lightLabel1.text += "\n\nNo lights found.";

            var lightBubble1 = infoView.Query<VisualElement>("LightBubble1").First();
            lightBubble1.Clear();
            foreach (var obj in lightSet1)
            {
                if(obj != null)
                    lightBubble1.Add(MakePill(obj));
            }

            var lightLabel2 = infoView.Query<Label>("LightLabel2").First();
            lightLabel2.text = $"Lights only in <b>Batch {batch2.batchId}:</b>";

            if (lightSet2.Count() == 0)
                lightLabel2.text += "\n\nNo lights found.";

            var lightBubble2 = infoView.Query<VisualElement>("LightBubble2").First();
            lightBubble2.Clear();
            foreach (var obj in lightSet2)
            {
                if(obj != null)
                    lightBubble2.Add(MakePill(obj));
            }

            // Shadow caster batch comparison
            var shadowLabel1 = infoView.Query<Label>("ShadowLabel1").First();
            shadowLabel1.text = $"Shadow Casters only in <b>Batch {batch1.batchId}:</b>";

            if (shadowSet1.Count() == 0)
                shadowLabel1.text += "\n\nNo shadow casters found.";

            var shadowBubble1 = infoView.Query<VisualElement>("ShadowBubble1").First();
            shadowBubble1.Clear();
            foreach (var obj in shadowSet1)
            {
                if (obj != null)
                    shadowBubble1.Add(MakePill(obj));
            }

            var shadowLabel2 = infoView.Query<Label>("ShadowLabel2").First();
            shadowLabel2.text = $"Shadow Casters only in <b>Batch {batch2.batchId}:</b>";

            if (shadowSet2.Count() == 0)
                shadowLabel2.text += "\n\nNo shadow casters found.";

            var shadowBubble2 = infoView.Query<VisualElement>("ShadowBubble2").First();
            shadowBubble2.Clear();
            foreach (var obj in shadowSet2)
            {
                if (obj != null)
                    shadowBubble2.Add(MakePill(obj));
            }

            lightCount = lightSet1.Count() + lightSet2.Count();
            shadowCount = shadowSet1.Count() + shadowSet2.Count();
        }

        // Create once, initialize
        private void CreateGUI()
        {
            // Generate color-blind friendly colors
            VisionUtility.GetColorBlindSafePalette(batchColors, 0.51f, 1.0f);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePath + "LightBatchingDebugger.uxml");
            var templateRoot = visualTree.Instantiate();
            templateRoot.style.flexGrow = 1;
            templateRoot.Q("ParentElement").StretchToParentSize();
            root.Add(templateRoot);

            var batchElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePath + "LayerBatch.uxml");
            Func<VisualElement> makeItem = () => batchElement.Instantiate();

            Action<VisualElement, int> bindItem = (e, i) =>
            {
                if (i >= batchList.Count())
                    return;

                // This is required to make the child of the ListView vary in heights
                e.style.height = StyleKeyword.Auto;

                var batch = batchList[i];
                var batchIndex = e.Query<Label>("BatchIndex").First();
                batchIndex.text = batch.batchId.ToString();

                var layers = e.Query<VisualElement>("LayerNames").First();
                layers.Clear();
                foreach (var layerName in batchList[i].LayerNames)
                {
                    var label = new Label { text = layerName };
                    label.AddToClassList("LayerNameLabel");
                    layers.Add(label);
                }

                var color = e.Query<VisualElement>("BatchColor").First();
                color.style.backgroundColor = new StyleColor(batchColors[i % batchColors.Length]);
            };

            DisplayInitialPrompt(true);

            batchListView = root.Query<ListView>("BatchList").First();
            batchListView.itemsSource = batchList;
            batchListView.makeItem = makeItem;
            batchListView.bindItem = bindItem;
            batchListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            batchListView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            batchListView.selectionType = SelectionType.Multiple;

            batchListView.selectionChanged += objects =>
            {
                OnSelectionChanged();
            };
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (PlayModeStateChange.EnteredEditMode == playModeState)
                QueueRefresh();
        }

        void DisplayInitialPrompt(bool display)
        {
            var initialPrompt = root.Query<Label>("InitialPrompt").First();
            initialPrompt.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;

            var infoView = root.Query<VisualElement>("InfoView").First();
            infoView.style.display = display ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnSelectionChanged()
        {
            if (batchListView == null)
                return;

            switch (batchListView.selectedIndices.Count())
            {
                case 1:
                    selectedIndices.Clear();
                    selectedIndices.Add(batchListView.selectedIndex);
                    ViewBatch(batchListView.selectedIndex);
                    break;

                case 2:
                    selectedIndices.Clear();
                    var firstIndex = batchListView.selectedIndices.First();
                    var secondIndex = batchListView.selectedIndices.Last();

                    if(secondIndex > firstIndex + 1 || secondIndex < firstIndex - 1)
                    {
                        // Clamp since we do adjacent batch comparisons
                        secondIndex = Mathf.Clamp(secondIndex, firstIndex - 1, firstIndex + 1);
                        selectedIndices.Add(firstIndex);
                        selectedIndices.Add(secondIndex);
                        batchListView.SetSelection(selectedIndices);
                    }
                    else
                    {
                        CompareBatch(firstIndex, secondIndex);
                        selectedIndices.AddRange(batchListView.selectedIndices);
                    }
                    break;

                default:
                    // Account for multiple select either with shift or ctrl keys
                    if(batchListView.selectedIndices.Count() > 2)
                    {
                        if (selectedIndices.Count == 1)
                        {
                            firstIndex = secondIndex = selectedIndices.First();

                            if (batchListView.selectedIndices.First() > firstIndex)
                                secondIndex = firstIndex + 1;
                            else if (batchListView.selectedIndices.First() < firstIndex)
                                secondIndex = firstIndex - 1;

                            selectedIndices.Add(secondIndex);
                            batchListView.SetSelection(selectedIndices);
                        }
                        else if (selectedIndices.Count == 2)
                        {
                            batchListView.SetSelection(selectedIndices);
                        }
                    }
                    break;
            }

            // Update counts
            Label lightHeader = root.Query<Label>("LightHeader");
            lightHeader.text = $"Lights ({lightCount})";
            Label shadowHeader = root.Query<Label>("ShadowHeader");
            shadowHeader.text = $"Shadow Casters ({shadowCount})";
        }

        private void RefreshView()
        {
            PopulateData();
            batchListView.RefreshItems();
            OnSelectionChanged();

            ResetDirty();
        }

        private void Update()
        {
            if (IsDirty())
                QueueRefresh();

            if (doRefresh)
                RefreshView();
        }

        private bool IsDirty()
        {
            bool isDirty = false;

            // Refresh if layers are added or removed
            isDirty |= Light2DManager.GetCachedSortingLayer().Count() != batchList.Sum(x => x.LayerNames.Count());
            isDirty |= cachedSceneHandle != SceneManager.GetActiveScene().handle;
            isDirty |= cachedCamPos != Camera.main?.transform.position;

            if (lightCullResult != null)
            {
                isDirty |= totalLightCount != lightCullResult.visibleLights.Count();
                isDirty |= totalShadowCount != lightCullResult.visibleShadows.Count();
            }

            return isDirty;
        }

        private void ResetDirty()
        {
            cachedSceneHandle = SceneManager.GetActiveScene().handle;

            if (Camera.main != null)
                cachedCamPos = Camera.main.transform.position;

            if (lightCullResult != null)
            {
                totalLightCount = lightCullResult.visibleLights.Count();
                totalShadowCount = lightCullResult.visibleShadows.Count();
            }

            doRefresh = false;
        }

        public void QueueRefresh()
        {
            doRefresh = true;
        }
    }
}
