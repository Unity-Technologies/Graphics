using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXBlockController : VFXNodeController
    {
        VFXContextController m_ContextController;
        public VFXBlockController(VFXBlock model, VFXContextController contextController) : base(model, contextController.viewController)
        {
            m_ContextController = contextController;
            if (model is VFXSubgraphBlock)
            {
                (model as VFXSubgraphBlock).RecreateCopy();
            }
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

        public VFXContextController contextController
        {
            get { return m_ContextController; }
        }

        public new VFXBlock model
        {
            get { return base.model as VFXBlock; }
        }

        public int index
        {
            get { return m_ContextController.FindBlockIndexOf(this); }
        }
    }
}
