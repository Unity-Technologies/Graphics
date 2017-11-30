using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node
    {
        public VFXNodeUI()
        {
            AddToClassList("VFXNodeUI");
        }

        public override Port InstantiatePort(PortPresenter presenter)
        {
            if (presenter.direction == Direction.Input)
            {
                VFXDataAnchorPresenter anchorPresenter = presenter as VFXDataAnchorPresenter;
                VFXEditableDataAnchor anchor = VFXEditableDataAnchor.Create(anchorPresenter);
                anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

                return anchor;
            }
            else
            {
                return VFXOutputDataAnchor.Create(presenter as VFXDataAnchorPresenter);
            }
        }

        protected override void OnPortRemoved(Port anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXSlotContainerPresenter>().viewPresenter.onRecompileEvent -= (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        public IEnumerable<Port> GetPorts(bool input, bool output)
        {
            if (input)
            {
                foreach (var child in inputContainer)
                {
                    if (child is Port)
                        yield return child as Port;
                }
            }
            if (output)
            {
                foreach (var child in outputContainer)
                {
                    if (child is Port)
                        yield return child as Port;
                }
            }
        }
    }
}
