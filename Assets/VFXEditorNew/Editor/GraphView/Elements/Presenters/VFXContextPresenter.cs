using System;
using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    class VFXContextPresenter : GraphElementPresenter
    {
        public VFXView m_view;

        protected new void OnEnable()
        {
            base.OnEnable();
            capabilities |= Capabilities.Deletable;
        }

        public VFXContext Model
        {
            get { return m_Model; }
        }

        private VFXContext m_Model;

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

        public void InitModel(VFXContext model)
        {
            m_Model = model;

            inputAnchors.Clear();
            outputAnchors.Clear();

            // TODO : ACCESS INPUTS AND OUTPUTS
            // WIP STUFF
            var in_anchor = CreateInstance<VFXFlowInputAnchorPresenter>();
            in_anchor.anchorType = typeof(int);
            var in_anchor2 = CreateInstance<VFXFlowInputAnchorPresenter>();
            in_anchor2.anchorType = typeof(int);
            var out_anchor = CreateInstance<VFXFlowOutputAnchorPresenter>();
            out_anchor.anchorType = typeof(int);
            inputAnchors.Add(in_anchor);
            inputAnchors.Add(in_anchor2);
            outputAnchors.Add(out_anchor);
        }

        public void AddNodeBlock()
        {
            // need to replug to model
            var block = CreateInstance<VFXNodeBlockPresenter>();
            m_NodeBlockPresenters.Add(block);
        }

    }
}
