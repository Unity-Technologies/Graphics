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

        public override Vector3 GetGlobalCenter()
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            var center = m_ConnectorBox.position.position + new Vector2(presenter.direction == Direction.Input ? 1 : m_ConnectorBox.position.width -1, m_ConnectorBox.position.height * 0.5f -0.5f);
            center = m_ConnectorBox.transform.MultiplyPoint3x4(center);
            return this.LocalToGlobal(center);
        }

        void IEdgeConnectorListener.OnDropOutsideAnchor(EdgePresenter edge, Vector2 position)
        {
            VFXDataAnchorPresenter presenter = GetPresenter<VFXDataAnchorPresenter>();

            VFXSlot startSlot = presenter.model;


            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewPresenter viewPresenter = view.GetPresenter<VFXViewPresenter>();


            Node endNode = null;
            foreach( var node in view.GetAllNodes())
            {
                if( node.localBound.Contains(position))
                {
                    endNode = node;
                }
            }

            if (endNode != null)
            {
                VFXLinkablePresenter nodePresenter = endNode.GetPresenter<VFXLinkablePresenter>();

                if (nodePresenter != null)
                {
                    IVFXSlotContainer slotContainer = nodePresenter.slotContainer;
                    if (presenter.direction == Direction.Input)
                    {
                        foreach (var outputSlot in slotContainer.outputSlots)
                        {
                            if (startSlot.CanLink(outputSlot))
                            {
                                startSlot.Link(outputSlot);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var inputSlot in slotContainer.inputSlots)
                        {
                            if (inputSlot.CanLink(startSlot))
                            {
                                inputSlot.Link(startSlot);
                                break;
                            }

                        }
                    }
                }
            }
            else if (presenter.direction == Direction.Input)
            {

                VFXModelDescriptorParameters parameterDesc = VFXLibrary.GetParameters().FirstOrDefault(t => t.name == presenter.anchorType.Name);
                if (parameterDesc != null)
                {
                    VFXParameter parameter = viewPresenter.AddVFXParameter(position, parameterDesc);

                    startSlot.Link(parameter.outputSlots[0]);
                }
            }


        }

    }

    
}
