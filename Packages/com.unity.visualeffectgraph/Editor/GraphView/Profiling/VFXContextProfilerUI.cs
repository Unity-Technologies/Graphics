using System.Collections.Generic;
using System.IO;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXContextProfilerUI : VFXAnchoredProfilerUI
    {
        public VFXContextProfilerUI(VFXContextUI contextUI)
        {
            m_ContextUI = contextUI;
            controller = contextUI.controller;
            controller.RegisterHandler(this);

            if(!m_ContextUI.selected)
                AddToClassList(k_HiddenStyleClass);

            title = controller.model.name.Split(" ")[0];
            AddToClassList("VFXContextProfiler");

            AnchorTo(m_ContextUI);

            m_TextureInfosLabels = new List<SlotLabel>();
            m_TextureHashSet = new HashSet<Texture>();
        }

        private VFXContextUI m_ContextUI;
        internal VFXContextUI contextUI => m_ContextUI;
        private Label m_ContextAggregateGPUTiming;
        public struct SlotLabel
        {
            public SlotLabel(VFXSlot slot, string text)
            {
                this.slot = slot;
                this.label = new Label(text) { name = "texture-slot" };
            }
            public VFXSlot slot;
            public Label label;
        }

        private Foldout m_TextureUsageFoldout;
        private List<SlotLabel> m_TextureInfosLabels;
        private HashSet<Texture> m_TextureHashSet;
        private Label m_InfoBadge;

        private VisualElement m_PositionBadge;
        private VisualElement m_RotationBadge;
        private VisualElement m_AgeBadge;

        public override bool isCollapsed => ClassListContains(k_HiddenStyleClass);

        void CollectAllTextureSlotsRecursive(IVFXSlotContainer slotContainer, HashSet<VFXSlot> allLinkedSlots)
        {
            foreach (var inputSlot in slotContainer.inputSlots)
            {
                if(inputSlot is VFXSlotTexture2D or VFXSlotTexture3D or VFXSlotTextureCube or VFXSlotTexture2DArray or VFXSlotTextureCubeArray )
                    allLinkedSlots.Add(inputSlot);
                foreach (var linkedSlot in inputSlot.LinkedSlots)
                {
                    CollectAllTextureSlotsRecursive(linkedSlot.owner, allLinkedSlots);
                }
            }
        }

        internal static string GetTextureInformationString(Texture texture)
        {
            string memorySize = EditorUtility.FormatBytes(TextureUtil.GetStorageMemorySizeLong(texture));
            string textureInfo;
            if (texture is Texture3D texture3D)
            {
                textureInfo = $"{texture3D.height}x{texture3D.width}x{texture3D.depth} - {memorySize}";
            }
            else
            {
                textureInfo = $"{texture.height}x{texture.width} - {memorySize}";
            }

            const int kMaxTotalLength = 35;
            int lengthLeftForName = kMaxTotalLength - textureInfo.Length;
            string textureName;
            if (lengthLeftForName > texture.name.Length)
                textureName = texture.name;
            else
            {
                int halfLength = lengthLeftForName / 2;
                textureName = $"{texture.name.Substring(0, halfLength)}...{texture.name.Substring(texture.name.Length - halfLength, halfLength)}";
            }

            return $"{textureName} - {textureInfo}";
        }

        internal List<SlotLabel> GetTextureSlotLabels()
        {
            return m_TextureInfosLabels;
        }
        private void RegisterTextureLabel(VFXSlot slot)
        {
            Texture texture = slot.value as Texture;
            m_TextureInfosLabels.Add( new SlotLabel(slot, texture != null ? GetTextureInformationString(texture) : ""));
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            if(!m_Locked)
                AddToClassList(k_HiddenStyleClass);
        }

        public override void AnchorTo(VFXContextUI target)
        {
            base.AnchorTo(target);
            target.onSelectionDelegate += OnContextSelection;
        }
        public override void Detach()
        {
            base.Detach();
            m_ContextUI.onSelectionDelegate -= OnContextSelection;
        }

        internal void RepositionPanel()
        {
            RepositionPanel(m_ContextUI);
        }

        public override void ForceExpand()
        {
            RemoveFromClassList(k_HiddenStyleClass);
            m_Foldout.value = true;
        }

        public override void ForceClose()
        {
            AddToClassList(k_HiddenStyleClass);
            m_Foldout.value = false;
        }

        void OnContextSelection(bool isSelected)
        {
            if (isSelected)
            {
                RemoveFromClassList(k_HiddenStyleClass);
                m_Foldout.value = true;
            }
            else
            {
                if(!m_Locked)
                    AddToClassList(k_HiddenStyleClass);
            }
        }

        protected override void SetupStatsLayout()
        {
            if (attachedComponent == null)
                return;

            if (m_ProfilingGPUItemLeaves != null)
            {
                ClearContent();
                m_ProfilingGPUItemLeaves.Clear();
            }
            else
            {
                m_ProfilingGPUItemLeaves = new List<VFXProfilingBoard.ProfilingItemLeaf>();
            }

            if(controller.model is VFXBasicSpawner)
                SetupEventBadges();
            if(controller.model is VFXBasicUpdate)
                SetupImplicitUpdateBadges();

            VFXSystemNames systemNames = controller.viewController.graph.systemNames;
            string systemName = systemNames.GetUniqueSystemName(controller.model.GetData());
            m_SystemName = systemName;
            Foldout gpuTimeFoldout = new Foldout
            {
                name = "TimingGPU",
                text = "Execution time (GPU)",
                tooltip = "Execution time of the context on the GPU, in milliseconds. Number of dispatches/draw calls are in parenthesis."
            };
            gpuTimeFoldout.value = false;

            m_ContextAggregateGPUTiming = AddLabelToFoldout(gpuTimeFoldout);
            if (!m_SupportsGPURecorder)
            {
                m_ContextAggregateGPUTiming.text = "-";
                gpuTimeFoldout.tooltip = kGpuRecorderNotSupportedMsg;
            }

            foreach (var taskId in controller.model.GetContextTaskIndices())
            {
                string markerName = attachedComponent.GetGPUTaskMarkerName(systemName, taskId.taskIndex);
                if (string.IsNullOrEmpty(markerName))
                    continue;
                VisualElement labelContainer = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 }
                };
                Label taskLabel = new Label
                {
                    name = markerName,
                    text = $"{taskId.taskName} ",
                    style = { flexGrow = 1 }
                };

                Label taskTimingLabel = new Label()
                {
                    name = markerName,
                    style = { flexGrow = 0, alignSelf = Align.FlexEnd}
                };

                if (!m_SupportsGPURecorder)
                {
                    taskTimingLabel.text = "-";
                    taskTimingLabel.tooltip = kGpuRecorderNotSupportedMsg;
                }
                labelContainer.Add(taskLabel);
                labelContainer.Add(taskTimingLabel);
                gpuTimeFoldout.Add(labelContainer);

                var recorder = Recorder.Get(markerName);
                VFXProfilingBoard.ProfilingItemLeaf leaf = new VFXProfilingBoard.ProfilingItemLeaf(recorder, taskTimingLabel);
                m_ProfilingGPUItemLeaves.Add(leaf);
            }
            if(gpuTimeFoldout.childCount > 0)
                AddContent(gpuTimeFoldout);

            SetupTextureSection();
        }

        private void SetupTextureSection()
        {
            m_TextureInfosLabels.Clear();
            HashSet<VFXSlot> textureSlots = new HashSet<VFXSlot>();
            CollectAllTextureSlotsRecursive(controller.model, textureSlots);
            foreach (var block in controller.model.children)
            {
                CollectAllTextureSlotsRecursive(block, textureSlots);
            }

            foreach (var slot in textureSlots)
            {
                RegisterTextureLabel(slot);
            }

            if (m_TextureInfosLabels.Count > 0)
            {
                m_TextureUsageFoldout = new Foldout
                {
                    name = "TextureUsage",
                    text = "Texture Usage"
                };

                m_TextureUsageFoldout.value = false;
                foreach (var textureInfoLabel in m_TextureInfosLabels)
                {
                    m_TextureUsageFoldout.Add(textureInfoLabel.label);
                }
                AddContent(m_TextureUsageFoldout);
            }
        }

        void SetupEventBadges()
        {
            VisualElement statusContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 }
            };

            m_InfoBadge = CreateBadge(null, "Indicates whether the last received event was a Start or a Stop event");
            statusContainer.Add(m_InfoBadge);
            AddContent(statusContainer);
        }

        void SetupImplicitUpdateBadges()
        {
            bool hasPosition = false;
            bool hasRotation = false;
            bool hasAge = false;

            if (controller.model is VFXBasicUpdate basicUpdate)
            {
                foreach (var block in basicUpdate.activeChildrenWithImplicit)
                {
                    if (block is EulerIntegration)
                        hasPosition = true;
                    if (block is AngularEulerIntegration)
                        hasRotation = true;
                    if (block is Age)
                        hasAge = true;
                }
            }

            if (hasPosition || hasRotation || hasAge)
            {
                VisualElement statusContainer = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, paddingTop = 2, paddingBottom = 2 },
                    tooltip = "Attributes implicitly updated during the update context. They can be disabled in update context’s inspector."
                };

                if (hasPosition)
                {
                    m_PositionBadge = CreateBadge("POSITION", "Particles positions are automatically updated based on their velocity attribute.");
                    m_PositionBadge.AddToClassList("position");
                    statusContainer.Add(m_PositionBadge);
                }

                if (hasRotation)
                {
                    m_RotationBadge = CreateBadge("ROTATION", "Particles rotations are automatically updated based on their angular velocity attribute.");
                    m_RotationBadge.AddToClassList("rotation");
                    statusContainer.Add(m_RotationBadge);
                }

                if (hasAge)
                {
                    m_AgeBadge = CreateBadge("AGE", "Particles ages are automatically incremented with deltaTime.");
                    m_AgeBadge.AddToClassList("age");
                    statusContainer.Add(m_AgeBadge);
                }
                AddContent(statusContainer);
            }
        }

        internal override void UpdateDynamicValues()
        {
            base.UpdateDynamicValues();
            if (m_ContextAggregateGPUTiming != null && m_SupportsGPURecorder)
            {
                float timeMs = NanoToMilli(GetGPUTimingAggregate());
                m_ContextAggregateGPUTiming.text = $"{timeMs:F3} ms";
                m_ContextAggregateGPUTiming.ClearClassList();
                m_ContextAggregateGPUTiming.AddToClassList(ComputeHeatmapClass(timeMs, m_HeatmapRefValue));
            }

            if (m_TextureInfosLabels != null && m_TextureInfosLabels.Count > 0)
            {
                m_TextureHashSet.Clear();
                foreach (var slotLabel in m_TextureInfosLabels)
                {
                    Texture texture = slotLabel.slot.value as Texture;
                    if (m_TextureHashSet.Contains(texture) || texture == null)
                    {
                        slotLabel.label.style.display =  DisplayStyle.None;
                        continue;
                    }

                    slotLabel.label.style.display = DisplayStyle.Flex;
                    string textureLabelText = GetTextureInformationString(texture);
                    m_TextureHashSet.Add(texture);
                    if (slotLabel.label.text != textureLabelText)
                    {
                        slotLabel.label.text = textureLabelText;
                    }
                }

                m_TextureUsageFoldout.style.display = m_TextureHashSet.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            }

            if (m_InfoBadge != null)
            {
                var spawnerState = m_AttachedComponent.GetSpawnSystemInfo(m_SystemName);
                if (spawnerState.playing)
                {
                    m_InfoBadge.text = "PLAYING";
                    m_InfoBadge.AddToClassList("play");
                    m_InfoBadge.RemoveFromClassList("stopped");
                }
                else
                {
                    m_InfoBadge.text = "STOPPED";
                    m_InfoBadge.AddToClassList("stopped");
                    m_InfoBadge.RemoveFromClassList("play");
                }
            }
        }
        public long GetGPUTimingAggregate()
        {
            long aggregate = 0L;
            foreach (var leaf in m_ProfilingGPUItemLeaves)
            {
                aggregate += leaf.average;
            }
            return aggregate;
        }
    }
}
