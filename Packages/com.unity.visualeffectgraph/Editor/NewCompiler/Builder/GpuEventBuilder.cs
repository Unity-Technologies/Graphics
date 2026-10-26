using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX
{
    class GpuEventBuilder : SystemBuilder
    {
        readonly List<VFXBasicGPUEvent> m_GpuEvents = new();

        public GpuEventBuilder()
        {
        }

        public override bool ProcessContext(VFXContext context)
        {
            bool processed = base.ProcessContext(context);
            if (context is VFXBasicGPUEvent gpuEvent)
            {
                m_GpuEvents.Add(gpuEvent);
                processed = true;
            }
            return processed;
        }

        public override void Build()
        {
            foreach (var gpuEvent in m_GpuEvents)
            {
                ConnectGpuEvent(gpuEvent);
            }
        }

        void ConnectGpuEvent(VFXBasicGPUEvent gpuEvent)
        {
            TriggerEvent triggerBlock = null;
            VFXContext fromContext = null;
            foreach (var slot in gpuEvent.allLinkedInputSlot)
            {
                Debug.Assert(fromContext == null, "GPU events should receive events from a single trigger block");
                triggerBlock = slot.owner as TriggerEvent;
                fromContext = triggerBlock.GetParent();
            }

            VFXContext toContext = null;
            foreach (var context in gpuEvent.outputContexts)
            {
                Debug.Assert(toContext == null, "GPU events should be connected to a single context");
                toContext = context;
            }

            if (fromContext != null && toContext != null)
            {
                var toData = toContext.GetData();
                var toSystemName = SystemNames.GetUniqueSystemName(toData);
                uint capacity = (uint)toData.GetSettingValue("capacity");

                var eventListDataDescription = new EventListData($"{toSystemName}_GPUEvents", isCpu: false, capacity);
                var eventListDataViewId = TaskGraphBuilder.AddData(eventListDataDescription.Name, eventListDataDescription);

                // Register event data so other builders can retrieve it
                RegisterDataView(gpuEvent, eventListDataViewId);
            }
        }

        public override void EndBuilding()
        {
            m_GpuEvents.Clear();
            base.EndBuilding();
        }
    }
}
