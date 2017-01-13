using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    class VFXContextPresenter : GraphElementPresenter
    {
        private VFXViewPresenter m_viewPresenter;
        public VFXViewPresenter ViewPresenter { get { return m_viewPresenter; } }
        
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

        public void Init(VFXViewPresenter viewPresenter,VFXContext model)
        {
            m_viewPresenter = viewPresenter;
            m_Model = model;

            inputAnchors.Clear();
            outputAnchors.Clear();

            // TODO : ACCESS INPUTS AND OUTPUTS
            // WIP STUFF
            if (model.ContextType != VFXContextDesc.Type.kTypeInit)
            {
                var in_anchor = CreateInstance<VFXFlowInputAnchorPresenter>();
                in_anchor.anchorType = typeof(int);
                inputAnchors.Add(in_anchor);
            }

            if (model.ContextType != VFXContextDesc.Type.kTypeOutput)
            {
                var out_anchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
                out_anchor.anchorType = typeof(int);
                outputAnchors.Add(out_anchor);
            }

            // Recreate presenters from model
            nodeBlockPresenters.Clear();
            foreach (var block in model.GetChildren())
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
            presenter.Model = block;
            m_NodeBlockPresenters.Add(presenter);
        }
    }
}
