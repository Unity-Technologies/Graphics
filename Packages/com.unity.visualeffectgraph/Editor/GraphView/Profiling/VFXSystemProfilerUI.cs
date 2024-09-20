using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXSystemProfilerUI : VFXAnchoredProfilerUI
    {
        public VFXSystemProfilerUI(VFXContextUI initContextUI, List<VFXContextProfilerUI> systemContexts)
        {
            m_SystemContexts = new List<VFXContextProfilerUI>(systemContexts);

            controller = initContextUI.controller;
            controller.RegisterHandler(this);
            ScheduleRepositionInitializePanel();

            VFXSystemNames systemNames = controller.viewController.graph.systemNames;
            string systemName = systemNames.GetUniqueSystemName(controller.model.GetData());
            m_InitContextUI = initContextUI;
            m_SystemName = systemName;

            var contextModel = controller.model;
            var vfxData = contextModel.GetData();
            bool receivesGPUEvent = false;
            foreach (var inputCtx in contextModel.inputContexts)
            {
                if (inputCtx is VFXBasicGPUEvent)
                {
                    receivesGPUEvent = true;
                    break;
                }
            }

            m_CanReadbackAliveCount = !receivesGPUEvent || vfxData.IsAttributeStored(VFXAttribute.Alive) ;

            title = "Particle System Info";
            AddToClassList("VFXSystemProfiler");
            AnchorTo(initContextUI);

            RegisterCallback<FocusInEvent>(e => e.StopPropagation());
        }

        private VFXSystemBorder m_SystemBorder;
        private List<VFXContextProfilerUI> m_SystemContexts;
        private VFXContextUI m_InitContextUI;
        private bool m_CanReadbackAliveCount;

        //Status elements
        private Label m_PlayPauseInfo;
        private Label m_SleepStateInfo;
        private Label m_CullStateInfo;

        public override void Detach()
        {
            RemoveFromHierarchy();
        }

        public override void ForceExpand()
        {
            m_Foldout.value = true;
        }

        public override void ForceClose()
        {
            m_Foldout.value = false;
        }
        public string GetSystemName()
        {
            return m_SystemName;
        }

        void ScheduleRepositionInitializePanel()
        {
            schedule.Execute(() =>
            {
                m_SystemContexts[0].SetVerticalOffset(resolvedStyle.height);
                m_SystemContexts[0].RepositionPanel();
            });
        }
        protected override void OnCollapseChanged(ChangeEvent<bool> evt)
        {
            base.OnCollapseChanged(evt);
            ScheduleRepositionInitializePanel();
        }

        void AddStatusSection()
        {
            VisualElement statusContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 }
            };

            m_PlayPauseInfo = CreateBadge(null, "Indicates the playing status of the game object.");
            m_SleepStateInfo = CreateBadge(null, "Indicates if any of the particles are alive or if the system is sleeping");
            m_CullStateInfo = CreateBadge(null, "Indicates if the system is visible by any of the cameras or if it is culled");

            statusContainer.Add(m_PlayPauseInfo);
            statusContainer.Add(m_SleepStateInfo);
            statusContainer.Add(m_CullStateInfo);

            AddContent(statusContainer);
        }

        void AddSystemCpuSection(string systemName)
        {
            var markerName = m_AttachedComponent.GetCPUSystemMarkerName(systemName);
            if (string.IsNullOrEmpty(markerName))
                return;
            var recorder = Recorder.Get(markerName);
            VisualElement labelContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 }
            };

            Label mainLabel = new Label
            {
                text = "System CPU Update:",
                style = { flexGrow = 1, paddingBottom = 2, paddingTop = 2}
            };
            Label timingLabel = new Label
            {
                style = { flexGrow = 0, alignSelf = Align.FlexEnd, color = Color.white}
            };

            labelContainer.Add(mainLabel);
            labelContainer.Add(timingLabel);

            VFXProfilingBoard.ProfilingItemLeaf leaf = new VFXProfilingBoard.ProfilingItemLeaf(recorder, timingLabel);
            m_ProfilingCPUItemLeaves.Add(leaf);
            AddContent(labelContainer);
        }

        private Label m_AggregatedGPULabel;
        private long m_AggregatedGPUTime;
        void AddAggregatedGPUSection()
        {
            VisualElement labelContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 }
            };
            Label descLabel = new Label
            {
                style = { flexGrow = 1 },
                text = "GPU System time:",
            };
            m_AggregatedGPULabel = new Label
            {
                style = { flexGrow = 0, alignSelf = Align.FlexEnd}
            };
            labelContainer.Add(descLabel);
            labelContainer.Add(m_AggregatedGPULabel);
            if (!m_SupportsGPURecorder)
            {
                labelContainer.tooltip = kGpuRecorderNotSupportedMsg;
                m_AggregatedGPULabel.text = "-";
            }

            AddContent(labelContainer);
        }

        void AddGPUMemorySection()
        {
            long gpuMemoryInBytes = (controller.model.GetData() as VFXDataParticle).attributeBufferSize * 4;
            VisualElement labelContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 }
            };

            Foldout gpuMemFoldout = new Foldout()
            {
                style = { flexGrow = 1 },
                text = "GPU Memory:",
            };

            Label totalGpuMemLabel = AddLabelToFoldout(gpuMemFoldout);
            totalGpuMemLabel.text = "";

            Label descLabel = new Label
            {
                style = { flexGrow = 1 },
                text = "Particle Attributes Size:",
            };
            Label memValueLabel = new Label
            {
                style = { flexGrow = 0, alignSelf = Align.FlexEnd}
            };

            memValueLabel.text = EditorUtility.FormatBytes(gpuMemoryInBytes);
            labelContainer.Add(descLabel);
            labelContainer.Add(memValueLabel);
            gpuMemFoldout.Add(labelContainer);
            AddContent(gpuMemFoldout);
        }

        private Label m_AliveCountLabel;
        private Label m_CapacityLabel;
        void AddOccupancySection()
        {
            VisualElement labelContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2}
            };
            labelContainer.tooltip = "Current count of living particles over the capacity of the system.";
            Label descLabel = new Label
            {
                style = { flexGrow = 1 },
                text = "Alive/Capacity:"
            };
            m_AliveCountLabel =  new Label
            {
                style = { flexGrow = 0, alignSelf = Align.FlexEnd}
            };
            var particleSystemInfo = m_AttachedComponent.GetParticleSystemInfo(m_SystemName);
            m_CapacityLabel =  new Label
            {
                style = { flexGrow = 0, alignSelf = Align.FlexEnd},
                text = $"/ {particleSystemInfo.capacity.ToString()}",
            };
            labelContainer.Add(descLabel);
            labelContainer.Add(m_AliveCountLabel);
            labelContainer.Add(m_CapacityLabel);
            AddContent(labelContainer);
        }
        void ComputeAggregatedGPUFromContexts()
        {
            long aggregate = 0L;
            foreach (var contextPanel in m_SystemContexts)
            {
                aggregate += contextPanel.GetGPUTimingAggregate();
            }

            m_AggregatedGPUTime = aggregate;
        }

        internal IEnumerable<VFXSlot> GetTextureSlotsFromContexts()
        {
            foreach (var contextPanel in m_SystemContexts)
            {
                foreach (var slotLabel in contextPanel.GetTextureSlotLabels())
                {
                    yield return slotLabel.slot;
                }
            }
        }

        public long GetSystemAggregatedGPUTime()
        {
            return m_AggregatedGPUTime;

        }
        internal override void UpdateDynamicValues()
        {
            base.UpdateDynamicValues();
            if (m_AggregatedGPULabel != null && m_SupportsGPURecorder)
            {
                ComputeAggregatedGPUFromContexts();
                m_AggregatedGPULabel.text = $"{NanoToMilli(m_AggregatedGPUTime):F3} ms";
            }

            var particleSystemInfo = m_AttachedComponent.GetParticleSystemInfo(m_SystemName);

            if (m_PlayPauseInfo != null)
            {
                if (m_AttachedComponent.pause)
                {
                    m_PlayPauseInfo.RemoveFromClassList("play");
                    m_PlayPauseInfo.AddToClassList("pause");
                    m_PlayPauseInfo.text = "PAUSED";
                }
                else
                {
                    m_PlayPauseInfo.RemoveFromClassList("pause");
                    m_PlayPauseInfo.AddToClassList("play");
                    m_PlayPauseInfo.text = "PLAYING";

                }
                if (particleSystemInfo.sleeping)
                {
                    m_SleepStateInfo.RemoveFromClassList("awake");
                    m_SleepStateInfo.AddToClassList("sleep");
                    m_SleepStateInfo.text = "SLEEP";

                }
                else
                {
                    m_SleepStateInfo.RemoveFromClassList("sleep");
                    m_SleepStateInfo.AddToClassList("awake");
                    m_SleepStateInfo.text = "AWAKE";
                }
                if (m_AttachedComponent.culled)
                {
                    m_CullStateInfo.RemoveFromClassList("visible");
                    m_CullStateInfo.AddToClassList("culled");
                    m_CullStateInfo.text = "CULLED";
                }
                else
                {
                    m_CullStateInfo.RemoveFromClassList("culled");
                    m_CullStateInfo.AddToClassList("visible");
                    m_CullStateInfo.text = "VISIBLE";
                }
            }
            if (m_AliveCountLabel != null)
            {
                string aliveCountStr = m_CanReadbackAliveCount ? particleSystemInfo.aliveCount.ToString() :  "-";
                m_AliveCountLabel.text = $"{aliveCountStr}";
                m_AliveCountLabel.ClearClassList();
                m_AliveCountLabel.AddToClassList(ComputeEfficiencyColor(particleSystemInfo.aliveCount, particleSystemInfo.capacity));
            }
        }

        protected override void SetupStatsLayout()
        {
            if (attachedComponent == null)
                return;

            if (m_ProfilingCPUItemLeaves != null)
            {
                ClearContent();
                m_ProfilingCPUItemLeaves.Clear();
            }
            else
            {
                m_ProfilingCPUItemLeaves = new List<VFXProfilingBoard.ProfilingItemLeaf>();
            }

            AddStatusSection();
            AddOccupancySection();
            AddSystemCpuSection(m_SystemName);
            AddAggregatedGPUSection();
            AddGPUMemorySection();
        }
    }
}
