using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.ObjectModel;
using System.Reflection;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXContextController : VFXNodeController
    {
        public VFXContext context       { get { return model as VFXContext; } }

        private List<VFXBlockController> m_BlockControllers = new List<VFXBlockController>();
        public ReadOnlyCollection<VFXBlockController> blockControllers
        {
            get { return m_BlockControllers.AsReadOnly(); }
        }

        protected List<VFXFlowAnchorController> m_FlowInputAnchors = new List<VFXFlowAnchorController>();
        public ReadOnlyCollection<VFXFlowAnchorController> flowInputAnchors
        {
            get { return m_FlowInputAnchors.AsReadOnly(); }
        }

        protected List<VFXFlowAnchorController> m_FlowOutputAnchors = new List<VFXFlowAnchorController>();
        public ReadOnlyCollection<VFXFlowAnchorController> flowOutputAnchors
        {
            get { return m_FlowOutputAnchors.AsReadOnly(); }
        }

        IDataWatchHandle m_DataHandle;

        public override void OnDisable()
        {
            if (viewController != null)
            {
                UnregisterAnchors();
            }
            if (!object.ReferenceEquals(m_Data, null))
            {
                viewController.UnRegisterNotification(m_Data, DataChanged);
                m_Data = null;
            }

            base.OnDisable();
        }

        private void UnregisterAnchors()
        {
            foreach (var anchor in flowInputAnchors)
                viewController.UnregisterFlowAnchorController(anchor);
            foreach (var anchor in flowOutputAnchors)
                viewController.UnregisterFlowAnchorController(anchor);
        }

        protected void DataChanged()
        {
            NotifyChange(AnyThing);
        }

        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            if (input)
            {
                VFXContextDataInputAnchorController anchorController = new VFXContextDataInputAnchorController(slot, this, hidden);

                return anchorController;
            }
            else
            {
                VFXContextDataOutputAnchorController anchorController = new VFXContextDataOutputAnchorController(slot, this, hidden);

                return anchorController;
            }
        }

        VFXData m_Data = null;

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            SyncControllers();
            // make sure we listen to the right data

            if (!object.ReferenceEquals(m_Data, null) && context.GetData() != m_Data)
            {
                viewController.UnRegisterNotification(m_Data, DataChanged);
                m_Data = null;
            }
            if (m_Data == null && context.GetData() != null)
            {
                m_Data = context.GetData();

                viewController.RegisterNotification(m_Data, DataChanged);
            }

            viewController.FlowEdgesMightHaveChanged();

            base.ModelChanged(obj);
        }

        public VFXContextController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
            UnregisterAnchors();

            if (context.inputType != VFXDataType.kNone)
            {
                for (int slot = 0; slot < context.inputFlowSlot.Length; ++slot)
                {
                    var inAnchor = new VFXFlowInputAnchorController();
                    inAnchor.Init(this, slot);
                    m_FlowInputAnchors.Add(inAnchor);
                    viewController.RegisterFlowAnchorController(inAnchor);
                }
            }

            if (context.outputType != VFXDataType.kNone)
            {
                for (int slot = 0; slot < context.outputFlowSlot.Length; ++slot)
                {
                    var outAnchor = new VFXFlowOutputAnchorController();
                    outAnchor.Init(this, slot);
                    m_FlowOutputAnchors.Add(outAnchor);
                    viewController.RegisterFlowAnchorController(outAnchor);
                }
            }

            SyncControllers();
        }

        public void AddBlock(int index, VFXBlock block)
        {
            context.AddChild(block, index);
        }

        public void ReorderBlock(int index, VFXBlock block)
        {
            if (block.GetParent() == model && context.GetIndex(block) < index)
            {
                --index;
            }

            context.AddChild(block, index);
        }

        public void RemoveBlock(VFXBlock block)
        {
            context.RemoveChild(block);

            VFXSlot slotToClean = null;
            do
            {
                slotToClean = block.inputSlots.Concat(block.outputSlots).FirstOrDefault(o => o.HasLink(true));
                if (slotToClean)
                {
                    slotToClean.UnlinkAll(true, true);
                }
            }
            while (slotToClean != null);
        }

        public int FindBlockIndexOf(VFXBlockController controller)
        {
            return m_BlockControllers.IndexOf(controller);
        }

        private void SyncControllers()
        {
            var m_NewControllers = new List<VFXBlockController>();
            foreach (var block in context.children)
            {
                var newController = m_BlockControllers.Find(p => p.model == block);
                if (newController == null) // If the controller does not exist for this model, create it
                {
                    newController = new VFXBlockController(block, this);
                    newController.ForceUpdate();
                }
                m_NewControllers.Add(newController);
            }

            foreach (var deletedController in m_BlockControllers.Except(m_NewControllers))
            {
                deletedController.OnDisable();
            }
            m_BlockControllers = m_NewControllers;
        }

        static VFXBlock DuplicateBlock(VFXBlock block)
        {
            var dependencies = new HashSet<ScriptableObject>();
            dependencies.Add(block);
            block.CollectDependencies(dependencies);

            var duplicated = VFXMemorySerializer.DuplicateObjects(dependencies.ToArray());

            VFXBlock result = duplicated.OfType<VFXBlock>().First();

            foreach (var slot in result.inputSlots)
            {
                slot.UnlinkAll(true, false);
            }
            foreach (var slot in result.outputSlots)
            {
                slot.UnlinkAll(true, false);
            }

            return result;
        }

        internal void BlocksDropped(int blockIndex, IEnumerable<VFXBlockController> draggedBlocks, bool copy)
        {
            //Sort draggedBlock in the order we want them to appear and not the selected order ( blocks in the same context should appear in the same order as they where relative to each other).

            draggedBlocks = draggedBlocks.OrderBy(t => t.index).GroupBy(t => t.contextController).SelectMany<IGrouping<VFXContextController, VFXBlockController>, VFXBlockController>(t => t.Select(u => u));


            foreach (VFXBlockController draggedBlock in draggedBlocks)
            {
                if (copy)
                {
                    this.AddBlock(blockIndex++, DuplicateBlock(draggedBlock.block));
                }
                else
                {
                    this.ReorderBlock(blockIndex++, draggedBlock.block);
                }
            }
        }

        public override IEnumerable<Controller> allChildren
        {
            get { return m_BlockControllers.Cast<Controller>(); }
        }

        public static bool IsTypeExpandable(System.Type type)
        {
            return !type.IsPrimitive && !typeof(UnityObject).IsAssignableFrom(type) && type != typeof(AnimationCurve) && !type.IsEnum && type != typeof(Gradient);
        }
    }
}
