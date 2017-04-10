using System;
using System.Collections.Generic;
using UnityEngine;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXBlockDataAnchorPresenter : VFXDataAnchorPresenter
    {
        public override bool expandable
        {
            get { return VFXBlockPresenter.IsTypeExpandable(anchorType); }
        }


        public void Init(VFXModel owner, VFXSlot model, VFXBlockPresenter nodePresenter)
        {
            base.Init(owner, model, nodePresenter);

            anchorType = model.property.type;
            name = model.property.name;
        }

        public void UpdateInfos()
        {
            anchorType = model.property.type;
        }


        public VFXBlockPresenter blockPresenter
        {
            get { return sourceNode as VFXBlockPresenter; }
        }


        public void SetPropertyValue(object value)
        {
            blockPresenter.PropertyValueChanged(this,value);
        }
    }

    class VFXBlockDataInputAnchorPresenter : VFXBlockDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXBlockDataOutputAnchorPresenter : VFXBlockDataAnchorPresenter
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
