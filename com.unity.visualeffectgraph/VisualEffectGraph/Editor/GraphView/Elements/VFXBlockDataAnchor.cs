using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXBlockDataAnchor : VFXEditableDataAnchor
    {
        protected VFXBlockDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
        }

        public static new VFXBlockDataAnchor Create(VFXDataAnchorPresenter presenter)
        {
            var anchor = new VFXBlockDataAnchor(presenter.orientation, presenter.direction, presenter.portType);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = presenter;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }
    }
}
