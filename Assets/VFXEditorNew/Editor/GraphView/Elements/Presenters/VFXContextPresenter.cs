using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXContextPresenter : GraphElementPresenter
    {
        private VFXViewPresenter m_ViewPresenter;
        public VFXViewPresenter ViewPresenter { get { return m_ViewPresenter; } }

        [SerializeField]
        private VFXContext m_Model;
        public VFXContext Model { get { return m_Model; } }

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

        public void Init(VFXViewPresenter viewPresenter, VFXContext model)
        {
            m_ViewPresenter = viewPresenter;
            m_Model = model;

            UnregisterAnchors();

            inputAnchors.Clear();
            outputAnchors.Clear();

            // TODO : ACCESS INPUTS AND OUTPUTS
            // WIP STUFF
            if (Model.inputType != VFXDataType.kNone)
            {
                var inAnchor = CreateInstance<VFXFlowInputAnchorPresenter>();
                inAnchor.Init(Model);
                inputAnchors.Add(inAnchor);
                ViewPresenter.RegisterFlowAnchorPresenter(inAnchor);
            }

            if (Model.outputType != VFXDataType.kNone)
            {
                var outAnchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
                outAnchor.Init(Model);
                outputAnchors.Add(outAnchor);
                ViewPresenter.RegisterFlowAnchorPresenter(outAnchor);
            }

            SyncPresenters();
        }

        public void AddBlock(int index,VFXBlock block)
        {
            Model.AddChild(block, index);
            SyncPresenters();
        }

        public void RemoveBlock(VFXBlock block)
        {
            Model.RemoveChild(block);
            SyncPresenters();
        }


        static int s_Counter = 1;

        private void SyncPresenters()
        {
            var m_NewPresenters = new List<VFXBlockPresenter>();

            foreach (var block in Model.GetChildren())
            {
                var presenter = blockPresenters.Find(p => p.Model == block);
                if (presenter == null) // If the presenter does not exist for this model, create it
                {
                    presenter = CreateInstance<VFXBlockPresenter>();
                    presenter.Init(block, this);
                    presenter.title = string.Format("{0} ({1})", block.name, s_Counter++);
                }

                m_NewPresenters.Add(presenter);
            }

            m_BlockPresenters = m_NewPresenters;
        }
    }
}
