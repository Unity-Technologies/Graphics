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
        private List<VFXNodeBlockPresenter> m_NodeBlockPresenters;
		public List<VFXNodeBlockPresenter> nodeBlockPresenters
 		{
 			get { return m_NodeBlockPresenters ?? (m_NodeBlockPresenters = new List<VFXNodeBlockPresenter>()); }
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
            if (Model.InputType != VFXDataType.kNone)
            {
                var inAnchor = CreateInstance<VFXFlowInputAnchorPresenter>();
                inAnchor.Init(Model);
                inputAnchors.Add(inAnchor);
                ViewPresenter.RegisterFlowAnchorPresenter(inAnchor);
            }

            if (Model.OutputType != VFXDataType.kNone)
            {
                var outAnchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
                outAnchor.Init(Model);
                outputAnchors.Add(outAnchor);
                ViewPresenter.RegisterFlowAnchorPresenter(outAnchor);
            }

            // Recreate presenters from model
            nodeBlockPresenters.Clear();
            foreach (var block in Model.GetChildren())
                AddPresentersFromModel((VFXBlock)block);
        }

        public void AddNodeBlock(int index,VFXBlockDesc desc)
        {
            var block = new VFXBlock(desc);
            Model.AddChild(block, index);
            AddPresentersFromModel(block);
        }

        private void AddPresentersFromModel(VFXBlock block)
        {
            var presenter = CreateInstance<VFXNodeBlockPresenter>();
            presenter.Init(block, this);
            m_NodeBlockPresenters.Add(presenter);
        }
    }
}
