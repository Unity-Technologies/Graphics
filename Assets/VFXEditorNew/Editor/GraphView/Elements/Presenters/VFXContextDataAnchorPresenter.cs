using System;
using System.Collections.Generic;
using UnityEngine;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXContextDataAnchorPresenter : VFXDataAnchorPresenter
    {
        public override bool expandable
        {
            get { return VFXBlockPresenter.IsTypeExpandable(anchorType); }
        }


        public void Init(VFXModel owner, VFXSlot model, VFXSlotContainerPresenter nodePresenter)
        {
            base.Init(owner, model, nodePresenter);
        }

        public void UpdateInfos(bool expanded)
        {
            anchorType = model.property.type;
        }

        public VFXBlockPresenter blockPresenter
        {
            get { return sourceNode as VFXBlockPresenter; }
        }
    }

    class VFXContextDataInputAnchorPresenter : VFXContextDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXContextDataOutputAnchorPresenter : VFXContextDataAnchorPresenter
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
