using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.VFX;
using UnityEngine.UIElements;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXProfilingBoard : GraphElement, IControlledElement<VFXViewController>, IVFXMovable, IVFXResizable
    {
        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        public bool hasExpandedPanel
        {
            get
            {
                foreach (var panel in m_AnchoredProfilerPanels)
                {
                    if (!panel.isCollapsed)
                        return true;
                }

                return false;
            }
        }

        VFXView m_View;
        IVisualElementScheduledItem m_UpdateItem;
        IVisualElementScheduledItem m_TimingCollectionUpdateItem;
        VisualEffect m_AttachedComponent;
        private bool m_Enabled;
        private bool m_SupportsGPURecorder;

        private readonly int kDisplayUpdatePeriodMs = 300;
        private readonly int kCollectUpdatePeriodMs = 10;

        public VFXProfilingBoard(VFXView view)
        {
            m_View = view;
            var tpl = VFXView.LoadUXML("VFXProfilingBoard");

            tpl.CloneTree(contentContainer);

            contentContainer.AddStyleSheetPath("VFXProfilingBoard");

            m_RootElement = this.Query<VisualElement>("component-container");
            m_SubtitleIcon = this.Query<Image>("subTitle-icon");
            m_Subtitle = this.Query<Label>("subTitleLabel");
            m_WarningIconTexture = EditorGUIUtility.LoadIcon(EditorResources.iconsPath + "console.warnicon.sml.png");
            m_SubtitleIcon.image = m_WarningIconTexture;
            m_GeneralInfoContainer = this.Query<VisualElement>("general-info-container");
            m_CPUInfoContainer = this.Query<Foldout>("cpu-timings-foldout");
            m_GPUInfoContainer = this.Query<Foldout>("gpu-timings-foldout");
            m_HeatmapSettingsContainer = this.Query<Foldout>("heatmap-foldout");
            m_TextureUsageFoldout = this.Query<Foldout>("texture-info-foldout");
            m_TextureHashSet = new HashSet<Texture>();
            m_TopExecutionTimeField = this.Query<FloatField>("exec-time");

            m_TextureInfosLabelsPerSystem = new List<List<VFXContextProfilerUI.SlotLabel>>();
            m_TextureSystemNameLabels = new List<Label>();
            InitializeContextualMenu();

            Detach();
            this.AddManipulator(new Dragger { clampToParentEdges = true });

            capabilities |= Capabilities.Movable;

            RegisterCallback<MouseDownEvent>(OnMouseClick);
            RegisterCallback<MouseUpEvent>(e=> e.StopPropagation());
            // Prevent graphview from zooming in/out when using the mouse wheel over the component board
            RegisterCallback<WheelEvent>(e => e.StopPropagation());

            style.position = PositionType.Absolute;

            SetPosition(BoardPreferenceHelper.LoadPosition(BoardPreferenceHelper.Board.profilingBoard, defaultRect));

            m_SupportsGPURecorder = SystemInfo.supportsGpuRecorder;
            SetupWarnings();
        }

        private void SetupWarnings()
        {
            if (!m_SupportsGPURecorder)
            {
                m_WarningGpuRecorder = new Label(VFXAnchoredProfilerUI.kGpuRecorderNotSupportedMsg);
                m_GeneralInfoContainer.Add(m_WarningGpuRecorder);
            }
            m_WarningNoSystems = new Label("No valid system is running in this effect.");
            m_GeneralInfoContainer.Add(m_WarningNoSystems);
            m_WarningNoSystems.style.display = DisplayStyle.None;
        }
        private void InitializeContextualMenu()
        {
            m_ShortcutWindowButton = this.Query<Button>("shortcut-windows");
            m_ShortcutWindowButton.focusable = false;

            m_ShortcutWindowMenu = new GenericMenu();
            m_ShortcutWindowMenu.AddItem(new GUIContent("Open Profiler Window"), false,
                () => EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler"));
            m_ShortcutWindowMenu.AddItem(new GUIContent("Open Rendering Debugger"), false, () =>
            {
                EditorApplication.ExecuteMenuItem("Window/Analysis/Rendering Debugger");
            });

            m_ShortcutWindowButton.clickable.clicked += () => { m_ShortcutWindowMenu.ShowAsContext(); };
        }

        internal void TogglePanelsVisibility()
        {
            bool? expand = null;
            foreach (var panel in m_AnchoredProfilerPanels)
            {
                expand ??= panel.isCollapsed;
                if (expand.Value) panel.ForceExpand();
                else panel.ForceClose();
            }
        }

        public void ValidatePosition()
        {
            BoardPreferenceHelper.ValidatePosition(this, m_View, defaultRect);
        }

        static readonly Rect defaultRect = new Rect(200, 100, 300, 300);

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        public override void SetPosition(Rect newPos)
        {
            style.left = newPos.xMin;
            style.top = newPos.yMin;
            style.width = newPos.width;
            style.height = newPos.height;
        }

        void OnMouseClick(MouseDownEvent e)
        {
            m_View.SetBoardToFront(this);
            e.StopPropagation();
        }

        public void Detach()
        {
            m_RootElement.SetEnabled(false);
            m_Subtitle.text = "Select a Game Object running this VFX";
            m_SubtitleIcon.style.display = DisplayStyle.Flex;

            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.UnregisterForProfiling();
            }
            m_AttachedComponent = null;

            PauseUpdateItems();

            DisableProfilerPanels();
            m_CPUInfoContainer.Clear();
            m_GPUInfoContainer.Clear();
            m_HeatmapSettingsContainer.Clear();
            m_TextureUsageFoldout.Clear();
        }

        private void PauseUpdateItems()
        {
            if (m_UpdateItem != null)
            {
                m_UpdateItem.Pause();
            }

            if (m_TimingCollectionUpdateItem != null)
            {
                m_TimingCollectionUpdateItem.Pause();
            }
        }

        private bool IsValidEffect()
        {
            bool valid = false;
            foreach (var systemController in m_View.controller.systems)
            {
                if (systemController.contexts[0].model.CanBeCompiled())
                {
                    valid = true;
                    break;
                }
            }
            return valid;
        }

        private void ClearUI()
        {
            if (m_AnchoredProfilerPanels != null)
            {
                foreach (var anchoredPanel in m_AnchoredProfilerPanels)
                {
                    anchoredPanel.Detach();
                }
                m_AnchoredProfilerPanels.Clear();
            }
            m_CPUInfoContainer.Clear();
            m_GPUInfoContainer.Clear();
            m_TextureUsageFoldout.Clear();
            m_HeatmapSettingsContainer.Clear();
        }
        private void SetupUI()
        {
            if (!IsValidEffect())
            {
                ClearUI();
                m_WarningNoSystems.style.display = DisplayStyle.Flex;
                return;
            }
            m_WarningNoSystems.style.display = DisplayStyle.None;

            SetupCpuData();
            SetupGpuData();
            SetupHeatMapSettings();
            CreateOrResumeUpdateItems();
            EnableProfilerPanels();
            SetupTextureUsageSection();
            UpdateDynamicValues();
        }

        public bool Attach(VisualEffect effect)
        {
            VisualEffect target = effect != null ? effect : Selection.activeGameObject?.GetComponent<VisualEffect>();
            if (target != null && m_View.controller?.graph != null && m_AttachedComponent != target)
            {
                if (m_AttachedComponent != null)
                {
                    m_AttachedComponent.UnregisterForProfiling();
                }

                m_AttachedComponent = target;
                if (m_Enabled)
                {
                    m_AttachedComponent.RegisterForProfiling();
                    SetupUI();
                }

                m_Subtitle.text = m_AttachedComponent.name;

                m_RootElement.SetEnabled(true);
                m_SubtitleIcon.style.display = DisplayStyle.None;

                return true;
            }

            return false;
        }

        public void Enable()
        {
            m_Enabled = true;
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.profilingBoard, true);
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.RegisterForProfiling();
                SetupUI();
            }
        }

        public void Disable()
        {
            m_Enabled = false;
            RemoveFromHierarchy();
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.profilingBoard, false);
            DisableProfilerPanels();
            if (m_AttachedComponent != null)
            {
                m_AttachedComponent.UnregisterForProfiling();
            }
        }

        public void EnableProfilerPanels()
        {
            if (m_AnchoredProfilerPanels != null)
            {
                foreach (var anchoredPanel in m_AnchoredProfilerPanels)
                {
                    anchoredPanel.Detach();
                }
                m_AnchoredProfilerPanels.Clear();
            }
            else
            {
                m_AnchoredProfilerPanels = new List<VFXAnchoredProfilerUI>();
                m_ContextsUIToContextPanel = new Dictionary<VFXContextUI, VFXContextProfilerUI>();
            }

            List<VFXContextProfilerUI> systemContexts = new List<VFXContextProfilerUI>();
            foreach (var systemBorder in m_View.systems)
            {
                if(systemBorder.contexts.Length == 0 || !systemBorder.contexts[0].controller.model.GetData().CanBeCompiled())
                    continue;
                VFXContextUI initContextUI = null;
                foreach(var contextUI in systemBorder.contexts)
                {
                    if((contextUI.controller.model.contextType & VFXContextType.InitAndUpdateAndOutput) != 0)
                    {
                        VFXContextProfilerUI contextPanel;
                        if (m_ContextsUIToContextPanel.TryGetValue(contextUI, out contextPanel))
                            contextPanel.AnchorTo(contextUI);
                        else
                            contextPanel = new VFXContextProfilerUI(contextUI);

                        contextPanel.attachedComponent = m_AttachedComponent;
                        m_AnchoredProfilerPanels.Add(contextPanel);
                        systemContexts.Add(contextPanel);
                    }

                    if (contextUI.controller.model.contextType == VFXContextType.Init)
                        initContextUI = contextUI;
                }
                var systemPanel = new VFXSystemProfilerUI(initContextUI, systemContexts);
                systemContexts.Clear();
                systemPanel.attachedComponent = m_AttachedComponent;
                m_AnchoredProfilerPanels.Add(systemPanel);
            }

            //Add spawners contexts
            var contextUIs = m_View.GetAllContexts();
            foreach (var contextUI in contextUIs)
            {
                if (contextUI.controller.model.contextType == VFXContextType.Spawner)
                {
                    var contextPanel = new VFXContextProfilerUI(contextUI);
                    contextPanel.attachedComponent = m_AttachedComponent;
                    m_AnchoredProfilerPanels.Add(contextPanel);
                }
            }
            m_ContextsUIToContextPanel.Clear();
            foreach (var anchoredPanel in m_AnchoredProfilerPanels)
            {
                if(anchoredPanel is VFXContextProfilerUI contextPanel)
                    m_ContextsUIToContextPanel.Add(contextPanel.contextUI, contextPanel);
            }
        }

        public void DisableProfilerPanels()
        {
            if (m_AnchoredProfilerPanels != null)
            {
                foreach (var anchoredPanel in m_AnchoredProfilerPanels)
                    anchoredPanel.Detach();
                m_AnchoredProfilerPanels.Clear();
            }
        }

        private void CreateOrResumeUpdateItems()
        {
            if (m_UpdateItem == null)
                m_UpdateItem = schedule.Execute(Update).Every(kDisplayUpdatePeriodMs);
            else
                m_UpdateItem.Resume();

            if (m_TimingCollectionUpdateItem == null)
                m_TimingCollectionUpdateItem = schedule.Execute(CollectStats).Every(kCollectUpdatePeriodMs);
            else
                m_TimingCollectionUpdateItem.Resume();
        }

        void Update()
        {
            if (m_AttachedComponent == null)
            {
                Detach();
                return;
            }

            if (m_Enabled)
            {
                if(!m_AttachedComponent.IsRegisteredForProfiling() && IsValidEffect())
                    m_AttachedComponent.RegisterForProfiling();
                UpdateDynamicValues();
            }

            string path = m_AttachedComponent.name;

            UnityEngine.Transform current = m_AttachedComponent.transform.parent;
            while (current != null)
            {
                path = current.name + " > " + path;
                current = current.parent;
            }

            if (UnityEngine.SceneManagement.SceneManager.loadedSceneCount > 1)
            {
                path = m_AttachedComponent.gameObject.scene.name + " : " + path;
            }

            if (m_Subtitle.text != path)
                m_Subtitle.text = path;
        }



        public class ProfilingItemLeaf
        {
            public ProfilingItemLeaf(Recorder recorder, Label label)
            {
                this.recorder = recorder;
                this.label = label;
                maxTiming = 0;
                minTiming = 0;
                average = 0;
            }
            long LongMax(long a, long b)
            {
                return a > b ? a : b;
            }
            long LongMin(long a, long b)
            {
                return a <= b ? a : b;
            }
            public void UpdateTimingStats(long time)
            {
                maxTiming = LongMax(maxTiming, time);
                minTiming = LongMin(minTiming, time);
                average = (long)(kAverageAlpha * average) + (long)((1.0f - kAverageAlpha) * time);
            }
            public void ResetTimingStats()
            {
                maxTiming = 0;
                minTiming = long.MaxValue;
            }
            public Recorder recorder;
            public Label label;
            public long maxTiming;
            public long minTiming;
            public long average;
            private const float kAverageAlpha = 0.9f;

        }

        List<ProfilingItemLeaf> m_CpuProfilingItemLeaves;

        void SetupCpuData()
        {
            m_CPUInfoContainer.Clear();
            if(m_CpuProfilingItemLeaves != null)
                m_CpuProfilingItemLeaves.Clear();
            else
                m_CpuProfilingItemLeaves = new List<ProfilingItemLeaf>();

            AddEffectCpuLeaf(VisualEffect.VFXCPUEffectMarkers.FullUpdate, "Full Update", "Update time of the entire effect on the CPU.");
            //AddEffectCpuLeaf(CPUMarkers.ProcessUpdate, "  Process Update");
            AddEffectCpuLeaf(VisualEffect.VFXCPUEffectMarkers.EvaluateExpressions, "  Evaluate Expressions", "");
            List<string> systemNamesFromAsset = new List<string>();
            m_View.controller.graph.visualEffectResource.asset.GetParticleSystemNames(systemNamesFromAsset);
            foreach (var systemName in systemNamesFromAsset)
            {
                AddSystemCpuLeaf(systemName);
            }
        }

        void SetupGpuData()
        {
            m_GPUInfoContainer.Clear();
            m_GPUTimeLabel = AddDataRowWithDynamicLabel(m_GPUInfoContainer, "GPU time");
            m_GPUTimeLabel.tooltip = "Execution time of the system on the GPU";
            if (!m_SupportsGPURecorder)
            {
                m_GPUTimeLabel.text = "-";
                m_GPUInfoContainer.tooltip = VFXAnchoredProfilerUI.kGpuRecorderNotSupportedMsg;
            }
            m_GPUMemoryLabel = AddDataRowWithDynamicLabel(m_GPUInfoContainer, "GPU memory");
            m_GPUMemoryLabel.tooltip = "GPU Memory usage of the entire effect.";
        }

        void SetupHeatMapSettings()
        {
            m_HeatmapSettingsContainer.Clear();
            m_TopExecutionTimeField = new FloatField("GPU Time Threshold (ms)")
            {
                name = "exec-time",
                tooltip = "Heatmap for visual feedback for execution time values in the graph, using a color gradient. Controls the value above which the timings will be red.",
                value = 0.1f,
            };

            m_HeatmapSettingsContainer.Add(m_TopExecutionTimeField);
            m_TopExecutionTimeField.RegisterValueChangedCallback(evt =>
            {
                var value = Mathf.Max(0, evt.newValue);
                m_TopExecutionTimeField.SetValueWithoutNotify(value);
                foreach (var anchoredPanel in m_AnchoredProfilerPanels)
                {
                    anchoredPanel.SetHeatmapRefValue(value);
                }
            });
        }

        void SetupTextureUsageSection()
        {
            if(m_TextureUsageFoldout != null)
            {
                m_TextureInfosLabelsPerSystem.Clear();
                m_TextureUsageFoldout.Clear();
                m_TextureSystemNameLabels.Clear();
                foreach (var anchoredPanel in m_AnchoredProfilerPanels)
                {
                    if (anchoredPanel is VFXSystemProfilerUI systemPanel)
                    {
                        var systemNameLabel = new Label(systemPanel.GetSystemName());
                        m_TextureSystemNameLabels.Add(systemNameLabel);
                        m_TextureUsageFoldout.Add(systemNameLabel);
                        m_TextureInfosLabelsPerSystem.Add(new List<VFXContextProfilerUI.SlotLabel>());
                        foreach (var textureSlot in systemPanel.GetTextureSlotsFromContexts())
                        {
                            var slotLabelGlobal = new VFXContextProfilerUI.SlotLabel(textureSlot, null);
                            m_TextureInfosLabelsPerSystem[^1].Add(slotLabelGlobal);
                            m_TextureUsageFoldout.Add(slotLabelGlobal.label);
                        }
                    }
                }
            }
        }

        Label AddDataRowWithDynamicLabel(VisualElement container, string labelText)
        {
            var labelContainer = new VisualElement();
            labelContainer.AddToClassList("dynamic-label");
            var mainLabel = new Label(labelText);
            mainLabel.AddToClassList("main");
            var dynamicLabel = new Label();
            dynamicLabel.AddToClassList("dynamic");
            labelContainer.Add(mainLabel);
            labelContainer.Add(dynamicLabel);
            container.Add(labelContainer);

            return dynamicLabel;
        }

        void AddEffectCpuLeaf(VisualEffect.VFXCPUEffectMarkers markerType, string displayName, string tooltip)
        {
            var markerName = m_AttachedComponent.GetCPUEffectMarkerName(markerType);
            var recorder = Recorder.Get(markerName);
            Label timingLabel = AddDataRowWithDynamicLabel(m_CPUInfoContainer, displayName);
            timingLabel.tooltip = tooltip;
            ProfilingItemLeaf leaf = new ProfilingItemLeaf(recorder, timingLabel);
            m_CpuProfilingItemLeaves.Add(leaf);
        }

        void AddSystemCpuLeaf(string systemName)
        {
            var markerName = m_AttachedComponent.GetCPUSystemMarkerName(systemName);
            var recorder = Recorder.Get(markerName);
            Label timingLabel = AddDataRowWithDynamicLabel(m_CPUInfoContainer, systemName);
            ProfilingItemLeaf leaf = new ProfilingItemLeaf(recorder, timingLabel);
            m_CpuProfilingItemLeaves.Add(leaf);
        }

        public long GetEffectAggregatedGPUTime()
        {
            long aggregate = 0L;
            foreach (var contextPanel in m_AnchoredProfilerPanels)
            {
                if(contextPanel is VFXSystemProfilerUI systemPanel)
                    aggregate += systemPanel.GetSystemAggregatedGPUTime();
            }
            return aggregate;
        }

        void UpdateDynamicValues()
        {
            const float kNanoToMilli =  1e-6f;

            foreach (var anchoredPanel in m_AnchoredProfilerPanels)
            {
                anchoredPanel.UpdateDynamicValues();
            }

            foreach (var leaf in m_CpuProfilingItemLeaves)
            {
                var timingLabel = leaf.label;
                if (timingLabel != null)
                {
                    timingLabel.text = $"{(leaf.average * kNanoToMilli):F3} ms";
                }
                leaf.ResetTimingStats();
            }

            if (m_GPUMemoryLabel != null & m_GPUTimeLabel != null)
            {
                var batchInfo = UnityEngine.VFX.VFXManager.GetBatchedEffectInfo(m_AttachedComponent.visualEffectAsset);

                m_GPUMemoryLabel.text = $"{ EditorUtility.FormatBytes((long)batchInfo.totalGPUSizeInBytes / batchInfo.totalInstanceCapacity)}";
                if(m_SupportsGPURecorder)
                    m_GPUTimeLabel.text = $"{GetEffectAggregatedGPUTime() * kNanoToMilli:F3} ms";
            }

            for (int i = 0; i < m_TextureSystemNameLabels.Count; i++)
            {
                var systemSlotLabel = m_TextureInfosLabelsPerSystem[i];
                m_TextureHashSet.Clear();
                foreach (var slotLabel in systemSlotLabel)
                {
                    Texture texture = slotLabel.slot.value as Texture;
                    if (m_TextureHashSet.Contains(texture) || texture == null)
                    {
                        slotLabel.label.style.display = DisplayStyle.None;
                        continue;
                    }

                    slotLabel.label.style.display = DisplayStyle.Flex;
                    string textureLabelText = VFXContextProfilerUI.GetTextureInformationString(texture);
                    m_TextureHashSet.Add(texture);
                    if (slotLabel.label.text != textureLabelText)
                    {
                        slotLabel.label.text = $"    {textureLabelText}";
                    }
                }
                m_TextureSystemNameLabels[i].style.display = m_TextureHashSet.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }

        }

        void CollectStats()
        {
            bool hasVfxUpdate = true;
            foreach (var leaf in m_CpuProfilingItemLeaves)
            {
                if (leaf.recorder.sampleBlockCount == 0)
                {
                    hasVfxUpdate = false;
                    break;
                }
                leaf.UpdateTimingStats(leaf.recorder.elapsedNanoseconds);
            }

            if (hasVfxUpdate)
            {
                foreach (var anchoredPanel in m_AnchoredProfilerPanels)
                    anchoredPanel.CollectStats();
            }
        }

        Button m_ShortcutWindowButton;
        GenericMenu m_ShortcutWindowMenu;

        VisualElement m_RootElement;
        VisualElement m_CPUInfoContainer;
        VisualElement m_GPUInfoContainer;
        VisualElement m_HeatmapSettingsContainer;
        VisualElement m_TextureUsageFoldout;
        VisualElement m_GeneralInfoContainer;

        private List<Label> m_TextureSystemNameLabels;
        private List<List<VFXContextProfilerUI.SlotLabel>> m_TextureInfosLabelsPerSystem;

        HashSet<Texture> m_TextureHashSet;
        FloatField m_TopExecutionTimeField;
        Label m_GPUTimeLabel;
        Label m_GPUMemoryLabel;
        Label m_Subtitle;
        Image m_SubtitleIcon;
        Texture2D m_WarningIconTexture;
        VisualElement m_WarningGpuRecorder;
        VisualElement m_WarningNoSystems;

        private List<VFXAnchoredProfilerUI> m_AnchoredProfilerPanels;
        private Dictionary<VFXContextUI, VFXContextProfilerUI> m_ContextsUIToContextPanel;

        public new void Clear()
        {
            Detach();
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.change != VFXViewController.Change.ui)
            {
                if (m_AttachedComponent != null)
                {
                    if (m_Enabled)
                    {
                        m_AttachedComponent.RegisterForProfiling();
                        SetupUI();
                    }
                }
            }
        }

        public override void UpdatePresenterPosition()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.profilingBoard, GetPosition());
        }

        public void OnMoved()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.profilingBoard, GetPosition());
        }

        void IVFXResizable.OnStartResize() { }
        public void OnResized()
        {
            BoardPreferenceHelper.SavePosition(BoardPreferenceHelper.Board.profilingBoard, GetPosition());
        }
    }

}
