using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXContextPresenter : GraphElementPresenter, IVFXPresenter
    {
        private VFXViewPresenter m_ViewPresenter;
        public VFXViewPresenter ViewPresenter { get { return m_ViewPresenter; } }

        [SerializeField]
        private VFXContext m_Model;
        public VFXContext context       { get { return m_Model; } }
        public virtual VFXModel model   { get { return m_Model; } }

		[SerializeField]
        private List<VFXBlockPresenter> m_BlockPresenters;
		public List<VFXBlockPresenter> blockPresenters
 		{
 			get { return m_BlockPresenters ?? (m_BlockPresenters = new List<VFXBlockPresenter>()); }
 		}

		[SerializeField]
		protected List<VFXFlowAnchorPresenter> m_InputAnchors;
		public List<VFXFlowAnchorPresenter> inputAnchors
 		{
 			get { return m_InputAnchors ?? (m_InputAnchors = new List<VFXFlowAnchorPresenter>()); }
 		}

		[SerializeField]
		protected List<VFXFlowAnchorPresenter> m_OutputAnchors;
		public List<VFXFlowAnchorPresenter> outputAnchors
		{
			get { return m_OutputAnchors ?? (m_OutputAnchors = new List<VFXFlowAnchorPresenter>()); }
		}

        protected new void OnEnable()
        {
            base.OnEnable();
            capabilities |= Capabilities.Deletable;
        }

        protected void OnDisable()
        {
            UnregisterAnchors();
        }

        private void UnregisterAnchors()
        {
            foreach (var anchor in inputAnchors)
                ViewPresenter.UnregisterFlowAnchorPresenter(anchor);
            foreach (var anchor in outputAnchors)
                ViewPresenter.UnregisterFlowAnchorPresenter(anchor);
        }

        public virtual void Init(VFXModel model,VFXViewPresenter viewPresenter)
        {
            m_ViewPresenter = viewPresenter;
            m_Model = (VFXContext)model;

            position = new Rect(model.position.x, model.position.y, position.width, position.height);

            UnregisterAnchors();

            inputAnchors.Clear();
            outputAnchors.Clear();

            // TODO : ACCESS INPUTS AND OUTPUTS
            // WIP STUFF
            if (context.inputType != VFXDataType.kNone)
            {
                var inAnchor = CreateInstance<VFXFlowInputAnchorPresenter>();
                inAnchor.Init(context);
                inputAnchors.Add(inAnchor);
                ViewPresenter.RegisterFlowAnchorPresenter(inAnchor);
            }

            if (context.outputType != VFXDataType.kNone)
            {
                var outAnchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
                outAnchor.Init(context);
                outputAnchors.Add(outAnchor);
                ViewPresenter.RegisterFlowAnchorPresenter(outAnchor);
            }

            SyncPresenters();
        }

        public void AddBlock(int index,VFXBlock block)
        {
            context.AddChild(block, index);
            SyncPresenters();
        }

        public void RemoveBlock(VFXBlock block)
        {
            context.RemoveChild(block);
            SyncPresenters();
        }


        static int s_Counter = 1;

        private void SyncPresenters()
        {
            var m_NewPresenters = new List<VFXBlockPresenter>();
            foreach (var block in context.GetChildren())
            {
                var presenter = blockPresenters.Find(p => p.Model == block);
                if (presenter == null) // If the presenter does not exist for this model, create it
                {
                    presenter = CreateInstance<VFXBlockPresenter>();
                    presenter.Init(block, this);
                    presenter.expanded = !block.collapsed;
                    presenter.title = string.Format("{0} ({1})", block.name, s_Counter++);
                }

                m_NewPresenters.Add(presenter);
            }

            m_BlockPresenters = m_NewPresenters;
        }

        public override IEnumerable<GraphElementPresenter> allChildren
        {
            get {
                foreach( var presenter in m_BlockPresenters)
                {
                    yield return presenter;
                }
            }
        }

        internal void BlocksDropped(VFXBlockPresenter blockPresenter, bool after, IEnumerable<VFXBlockPresenter> draggedBlocks)
        {
            //Sort draggedBlock in the order we want them to appear and not the selected order ( blocks in the same context should appear in the same order as they where relative to each other).
            
            draggedBlocks = draggedBlocks.OrderBy(t=>t.index).GroupBy(t => t.ContextPresenter).SelectMany<IGrouping<VFXContextPresenter,VFXBlockPresenter>,VFXBlockPresenter>(t=>t.Select(u=>u));

            foreach(VFXBlockPresenter draggedBlock in draggedBlocks)
            {
                draggedBlock.ContextPresenter.RemoveBlock(draggedBlock.Model);
            }

            int insertIndex = blockPresenter.index;
            if (after) insertIndex++;

            foreach (VFXBlockPresenter draggedBlock in draggedBlocks)
            {
                this.AddBlock(insertIndex++,draggedBlock.Model);
            }
        }
    }
}
