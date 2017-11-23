using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXContextPresenter : VFXNodePresenter
    {
        public VFXContext context       { get { return model as VFXContext; } }

        [SerializeField]
        private List<VFXBlockPresenter> m_BlockPresenters = new List<VFXBlockPresenter>();
        public IEnumerable<VFXBlockPresenter> blockPresenters
        {
            get { return m_BlockPresenters; }
        }

        [SerializeField]
        protected List<VFXFlowAnchorPresenter> m_FlowInputAnchors = new List<VFXFlowAnchorPresenter>();
        public IEnumerable<VFXFlowAnchorPresenter> flowInputAnchors
        {
            get { return m_FlowInputAnchors; }
        }

        [SerializeField]
        protected List<VFXFlowAnchorPresenter> m_FlowOutputAnchors = new List<VFXFlowAnchorPresenter>();
        public IEnumerable<VFXFlowAnchorPresenter> flowOutputAnchors
        {
            get { return m_FlowOutputAnchors; }
        }

        VFXContextSlotContainerPresenter m_SlotPresenter;
        public VFXContextSlotContainerPresenter slotPresenter
        {
            get { return m_SlotPresenter; }
        }


        protected new void OnEnable()
        {
            base.OnEnable();
            capabilities |= Capabilities.Deletable | Capabilities.Ascendable;
        }

        public override void OnRemoveFromGraph()
        {
            if (viewPresenter != null)
            {
                UnregisterAnchors();
                viewPresenter.RemoveInvalidateDelegate(model, OnModelInvalidate);
            }
        }

        private void UnregisterAnchors()
        {
            foreach (var anchor in flowInputAnchors)
                viewPresenter.UnregisterFlowAnchorPresenter(anchor);
            foreach (var anchor in flowOutputAnchors)
                viewPresenter.UnregisterFlowAnchorPresenter(anchor);
        }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            UnregisterAnchors();

            m_SlotPresenter = CreateInstance<VFXContextSlotContainerPresenter>();
            inputPorts.Clear();
            outputPorts.Clear();

            base.Init(model, viewPresenter);

            m_SlotPresenter.Init(model, this);

            if (context.inputType != VFXDataType.kNone)
            {
                for (int slot = 0; slot < context.inputFlowSlot.Length; ++slot)
                {
                    var inAnchor = CreateInstance<VFXFlowInputAnchorPresenter>();
                    inAnchor.Init(this, slot);
                    m_FlowInputAnchors.Add(inAnchor);
                    viewPresenter.RegisterFlowAnchorPresenter(inAnchor);
                }
            }

            if (context.outputType != VFXDataType.kNone)
            {
                for (int slot = 0; slot < context.outputFlowSlot.Length; ++slot)
                {
                    var outAnchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
                    outAnchor.Init(this, slot);
                    m_FlowOutputAnchors.Add(outAnchor);
                    viewPresenter.RegisterFlowAnchorPresenter(outAnchor);
                }
            }

            viewPresenter.AddInvalidateDelegate(model, OnModelInvalidate);
            SyncPresenters();
        }

        private void OnModelInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model == this.model && cause == VFXModel.InvalidationCause.kStructureChanged)
                SyncPresenters();
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

        public int FindBlockIndexOf(VFXBlockPresenter presenter)
        {
            return m_BlockPresenters.IndexOf(presenter);
        }

        private void SyncPresenters()
        {
            var m_NewPresenters = new List<VFXBlockPresenter>();
            foreach (var block in context.children)
            {
                var presenter = m_BlockPresenters.Find(p => p.model == block);
                if (presenter == null) // If the presenter does not exist for this model, create it
                {
                    presenter = CreateInstance<VFXBlockPresenter>();
                    presenter.Init(block, this);
                    presenter.expanded = !block.collapsed;
                    presenter.title = block.name;
                }
                m_NewPresenters.Add(presenter);
            }
            m_BlockPresenters = m_NewPresenters;
        }

        public override IEnumerable<GraphElementPresenter> allChildren
        {
            get
            {
                foreach (var presenter in m_BlockPresenters)
                {
                    yield return presenter;
                }
            }
        }

        internal void BlocksDropped(VFXBlockPresenter blockPresenter, bool after, IEnumerable<VFXBlockPresenter> draggedBlocks, bool copy)
        {
            //Sort draggedBlock in the order we want them to appear and not the selected order ( blocks in the same context should appear in the same order as they where relative to each other).

            draggedBlocks = draggedBlocks.OrderBy(t => t.index).GroupBy(t => t.contextPresenter).SelectMany<IGrouping<VFXContextPresenter, VFXBlockPresenter>, VFXBlockPresenter>(t => t.Select(u => u));

            int insertIndex;

            if (blockPresenter != null)
            {
                insertIndex = blockPresenter.index;
                if (after) insertIndex++;
            }
            else if (after)
            {
                insertIndex = blockPresenters.Count();
            }
            else
            {
                insertIndex = 0;
            }

            foreach (VFXBlockPresenter draggedBlock in draggedBlocks)
            {
                if (copy)
                {
                    this.AddBlock(insertIndex++, draggedBlock.block.Clone<VFXBlock>());
                }
                else
                {
                    this.ReorderBlock(insertIndex++, draggedBlock.block);
                }
            }
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, model, context.GetData() };
        }

        public override IEnumerable<GraphElementPresenter> allElements
        {
            get
            {
                return Enumerable.Repeat((GraphElementPresenter)this, 1)
                    .Concat(inputPorts.Cast<GraphElementPresenter>())
                    .Concat(outputPorts.Cast<GraphElementPresenter>())
                    .Concat(flowInputAnchors.Cast<GraphElementPresenter>())
                    .Concat(flowOutputAnchors.Cast<GraphElementPresenter>())
                    .Concat(blockPresenters.Cast<GraphElementPresenter>());
            }
        }
    }
}
