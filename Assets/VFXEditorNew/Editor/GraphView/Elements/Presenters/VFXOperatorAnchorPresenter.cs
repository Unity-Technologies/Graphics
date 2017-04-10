using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXOperatorAnchorPresenter : VFXDataAnchorPresenter
    {
        public void Init(VFXModel owner, VFXSlot model, VFXNodePresenter source)
        {
            base.Init(owner, model,source);
        }

        //Ignore m_Connection, this information is store in model
        public override void Connect(EdgePresenter edgePresenter) {}
        public override void Disconnect(EdgePresenter edgePresenter) {}


    }


    class VFXInputOperatorAnchorPresenter : VFXOperatorAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXOutputOperatorAnchorPresenter : VFXOperatorAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}