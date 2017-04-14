using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXDataAnchor : NodeAnchor, IEdgeConnectorListener
    {


        protected VFXDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            AddToClassList("VFXDataAnchor");
        }

        public static VFXDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXDataAnchor(presenter);
            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
            anchor.presenter = presenter;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            m_ConnectorText.text = "";

            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            // reverse because we want the flex to choose the position of the connector
            presenter.position = position;

            if (presenter.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");




            // update the css type of the class
            foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
                m_ConnectorBox.RemoveFromClassList(cls);

            m_ConnectorBox.AddToClassList(VFXTypeDefinition.GetTypeCSSClass(presenter.anchorType));

            AddToClassList("EdgeConnector");
            switch (presenter.direction)
            {
                case Direction.Input:
                    AddToClassList("Input");
                    break;
                case Direction.Output:
                    AddToClassList("Output");
                    break;
            }

            if(presenter.hidden)
            {
                AddToClassList("invisible");
            }
            else
            {
                RemoveFromClassList("invisible");
            }
            if( presenter.direction == Direction.Output )
                m_ConnectorText.text = presenter.name;

            clipChildren = false;
        }

        void IEdgeConnectorListener.OnDropOutsideAnchor(EdgePresenter edge, Vector2 position)
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();
            if( presenter.direction == Direction.Input)
            {
                VFXSlot inputSlot = presenter.model;


                VFXView view = this.GetFirstAncestorOfType<VFXView>();
                VFXViewPresenter viewPresenter = view.GetPresenter<VFXViewPresenter>();

                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == presenter.anchorType.Name);
                if (parameterDesc != null)
                {
                    VFXParameter parameter = viewPresenter.AddVFXParameter(position, parameterDesc);

                    inputSlot.Link(parameter.outputSlots[0]);
                }
            }
        }

    }

    
}
