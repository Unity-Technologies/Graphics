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
            capabilities |= Capabilities.Deletable | Capabilities.SendToFrontOnSelection;
        }

        protected void OnDisable()
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
            inputAnchors.Clear();
            outputAnchors.Clear();

            base.Init(model, viewPresenter);

            m_SlotPresenter.Init(model, this);

            if (context.inputType != VFXDataType.kNone)
            {
                for (int slot = 0; slot < context.inputFlowSlot.Length; ++slot)
                {
                    var inAnchor = CreateInstance<VFXFlowInputAnchorPresenter>();
                    inAnchor.Init(context, slot);
                    m_FlowInputAnchors.Add(inAnchor);
                    viewPresenter.RegisterFlowAnchorPresenter(inAnchor);
                }
            }

            if (context.outputType != VFXDataType.kNone)
            {
                for (int slot = 0; slot < context.outputFlowSlot.Length; ++slot)
                {
                    var outAnchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
                    outAnchor.Init(context, slot);
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

        internal void BlocksDropped(VFXBlockPresenter blockPresenter, bool after, IEnumerable<VFXBlockPresenter> draggedBlocks)
        {
            //Sort draggedBlock in the order we want them to appear and not the selected order ( blocks in the same context should appear in the same order as they where relative to each other).

            draggedBlocks = draggedBlocks.OrderBy(t => t.index).GroupBy(t => t.contextPresenter).SelectMany<IGrouping<VFXContextPresenter, VFXBlockPresenter>, VFXBlockPresenter>(t => t.Select(u => u));

            int insertIndex = blockPresenter.index;
            if (after) insertIndex++;

            foreach (VFXBlockPresenter draggedBlock in draggedBlocks)
            {
                this.ReorderBlock(insertIndex++, draggedBlock.block);
            }
        }

        public override IEnumerable<GraphElementPresenter> allElements
        {
            get
            {
                return Enumerable.Repeat((GraphElementPresenter)this, 1)
                    .Concat(inputAnchors.Cast<GraphElementPresenter>())
                    .Concat(outputAnchors.Cast<GraphElementPresenter>())
                    .Concat(flowInputAnchors.Cast<GraphElementPresenter>())
                    .Concat(flowOutputAnchors.Cast<GraphElementPresenter>())
                    .Concat(blockPresenters.Cast<GraphElementPresenter>());
            }
        }
    }
}
