using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXAnchoredProfilerUI : GraphElement, IControlledElement<VFXContextController>
    {
        public VFXAnchoredProfilerUI()
        {
            capabilities |=  Capabilities.Movable | Capabilities.Deletable | Capabilities.Collapsible;

            string path = VisualEffectAssetEditorUtility.editorResourcesPath + "/uxml/VFXAnchoredProfiler.uxml";
            var tpl = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            tpl.CloneTree(this);

            m_Foldout = this.Q<Foldout>();
            m_Foldout.Q<Toggle>().focusable = false;
            m_Foldout.RegisterCallback<ChangeEvent<bool>>(OnCollapseChanged);
            m_LockButton = this.Q("lock-button");
            m_LockButton.AddManipulator(new Clickable(ToggleLockState));

            styleSheets.Add(VFXView.LoadStyleSheet("VFXAnchoredProfiler"));
            styleSheets.Add(VFXView.LoadStyleSheet("VFXPanelBadge"));
            AddToClassList("VFXAnchoredProfiler");
            RegisterCallback<MouseDownEvent>(OnMouseClick);
            m_SupportsGPURecorder = SystemInfo.supportsGpuRecorder;
        }

        protected Foldout m_Foldout;
        protected string m_SystemName;
        protected bool m_SupportsGPURecorder;

        protected List<VFXProfilingBoard.ProfilingItemLeaf> m_ProfilingGPUItemLeaves;
        protected List<VFXProfilingBoard.ProfilingItemLeaf> m_ProfilingCPUItemLeaves;

        protected const string k_CollapsedStyleClass = "collapsed";
        protected const string k_HiddenStyleClass = "hidden";
        protected const string k_LockedStyleClass = "locked";
        protected const string k_HotStyleClass = "hot";
        protected const string k_MediumStyleClass = "medium";
        protected const string k_ColdStyleClass = "cold";

        protected readonly VisualElement m_CollapseButtonExpanded;
        protected readonly VisualElement m_CollapseButtonCollapsed;
        protected readonly VisualElement m_LockButton;
        protected float m_HeatmapRefValue = 0.1f;

        public const string kGpuRecorderNotSupportedMsg = "GPU Recorder is not supported on this platform";


        public void SetHeatmapRefValue(float value)
        {
            m_HeatmapRefValue = value;
        }
        protected VisualEffect m_AttachedComponent;
        public VisualEffect attachedComponent
        {
            get => m_AttachedComponent;
            set
            {
                m_AttachedComponent = value;
                SetupStatsLayout();
            }
        }

        public virtual bool isCollapsed { get; }

        protected bool m_Locked = false;
        protected const float kAnchorHorizontalOffset = 50.0f;
        protected float m_AnchorVerticalOffset = 0.0f;

        protected internal void SetVerticalOffset(float offset)
        {
            m_AnchorVerticalOffset = offset;
        }

        public override string title
        {
            set => m_Foldout.text = value;
            get => m_Foldout.text;
        }

        protected void AnchorPositioning(Rect anchorRect)
        {
            style.left = anchorRect.xMax + kAnchorHorizontalOffset ;
            style.top = anchorRect.y + m_AnchorVerticalOffset;
        }

        protected void ClearContent()
        {
            m_Foldout.Clear();
        }

        protected void AddContent(VisualElement content)
        {
            m_Foldout.Add(content);
        }

        protected void RepositionPanel(VFXContextUI target)
        {
            Rect targetRect = target.contentRect;
            var transformedRect = target.ChangeCoordinatesTo(hierarchy.parent, targetRect);
            style.position = UnityEngine.UIElements.Position.Absolute;
            AnchorPositioning(transformedRect);
        }

        public virtual void AnchorTo(VFXContextUI target)
        {
            Detach();
            target.Add(this);
            RepositionPanel(target);
        }

        void OnMouseClick(MouseDownEvent e)
        {
            if (hierarchy.parent == null)
                return;

            hierarchy.parent.BringToFront();
        }

        protected virtual void OnCollapseChanged(ChangeEvent<bool> evt)
        {
            if (evt.target == m_Foldout)
            {
                this.ToggleInClassList(k_CollapsedStyleClass);
            }
        }

        public virtual void Detach()
        {
            RemoveFromHierarchy();
        }

        public void ToggleLockState()
        {
            if (m_Foldout.value) //Lock/Unlock can only be done in expanded state
            {
                m_Locked = !m_Locked;
                ToggleInClassList(k_LockedStyleClass);
            }
        }

        public virtual void ForceExpand()
        {
        }

        public virtual void ForceClose()
        {
        }

        internal void UpdateStatsDisplay()
        {
            if (attachedComponent == null)
            {
                if(m_ProfilingGPUItemLeaves != null)
                    m_ProfilingGPUItemLeaves.Clear();
                else
                    m_ProfilingGPUItemLeaves = new List<VFXProfilingBoard.ProfilingItemLeaf>();
                return;
            }

            UpdateDynamicValues();
        }

        protected float NanoToMilli(long timeLong)
        {
            const float kNanoToMilli =  1e-6f;
            return timeLong * kNanoToMilli;
        }

        protected string ComputeHeatmapClass(float currentValue, float maxValue)
        {
            if(currentValue < maxValue * 0.50f)
                return k_ColdStyleClass;
            if(currentValue < maxValue * 1.0f)
                return k_MediumStyleClass;
            return k_HotStyleClass;
        }

        protected string ComputeEfficiencyColor(uint alive, uint capacity)
        {
            float efficiency = (float)alive / capacity;
            if (efficiency < 0.51f)
                return k_HotStyleClass;
            if (efficiency < 0.91f)
                return k_MediumStyleClass;
            return k_ColdStyleClass;
        }
        internal virtual void UpdateDynamicValues()
        {
            if (needSetupStatsLayout)
            {
                SetupStatsLayout();
                needSetupStatsLayout = false;
            }

            if (m_ProfilingGPUItemLeaves != null && m_SupportsGPURecorder)
            {
                foreach (var leaf in m_ProfilingGPUItemLeaves)
                {
                    var timingLabel = leaf.label;
                    if (timingLabel != null)
                    {
                        int sampleCount = leaf.recorder.gpuSampleBlockCount;

                        float timeMs = NanoToMilli(leaf.average);
                        timingLabel.text = $"{timeMs:F3} ms ({sampleCount.ToString()})";
                        timingLabel.ClearClassList();
                        timingLabel.AddToClassList(ComputeHeatmapClass(timeMs, m_HeatmapRefValue));
                    }

                    leaf.ResetTimingStats();
                }
            }

            if (m_ProfilingCPUItemLeaves != null)
            {
                foreach (var leaf in m_ProfilingCPUItemLeaves)
                {
                    var timingLabel = leaf.label;
                    if (timingLabel != null)
                        timingLabel.text = $"{NanoToMilli(leaf.average):F3} ms";

                    leaf.ResetTimingStats();
                }
            }
        }
        protected Label CreateBadge(string text, string tip)
        {
            return new Label { name = "Badge", text = text, tooltip = tip};
        }

        private bool needSetupStatsLayout = false;

        protected virtual void SetupStatsLayout()
        {
        }

        internal void CollectStats()
        {
            if (m_ProfilingGPUItemLeaves != null)
            {
                foreach (var leaf in m_ProfilingGPUItemLeaves)
                {
                    leaf.UpdateTimingStats(leaf.recorder.gpuElapsedNanoseconds);
                }
            }

            if (m_ProfilingCPUItemLeaves != null)
            {
                foreach (var leaf in m_ProfilingCPUItemLeaves)
                {
                    leaf.UpdateTimingStats(leaf.recorder.elapsedNanoseconds);
                }
            }
        }

        protected Label AddLabelToFoldout(Foldout foldout)
        {
            var label = new Label { name = "toggleInfo" };
            foldout.Q<Toggle>().Add(label);
            return label;
        }

        Controller IControlledElement.controller => controller;

        public VFXContextController controller { get; protected set; }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            // Needs to call SetupStatsLayout at the next frame
            needSetupStatsLayout = true;
        }
    }
}
