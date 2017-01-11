using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXContextUI : GraphElement
    {
        const string RectColorProperty = "rect-color";

        VisualContainer m_FlowInputConnectorContainer;
        VisualContainer m_FlowOutputConnectorContainer;
        VisualContainer m_NodeContainer;
        VisualContainer m_NodeBlockContainer;

        public VFXContextUI()
        {
            pickingMode = PickingMode.Ignore;

            m_FlowInputConnectorContainer = new VisualContainer()
            {
                name = "FlowInputs",
                pickingMode = PickingMode.Ignore,
                classList = new ClassList("FlowContainer")
            };

            m_FlowOutputConnectorContainer = new VisualContainer()
            {
                name = "FlowOutputs",
                pickingMode = PickingMode.Ignore,
                classList = new ClassList("FlowContainer")
            };

            m_NodeContainer = new VisualContainer()
            {
                name = "NodeContents"
            };

            AddChild(m_FlowInputConnectorContainer);
            AddChild(m_NodeContainer);
            AddChild(m_FlowOutputConnectorContainer);
            m_NodeContainer.AddChild(new Label(new GUIContent("Title")));

            m_NodeBlockContainer = new VisualContainer()
            {
                name = "NodeBlockContainer",
                pickingMode = PickingMode.Ignore
            };

            m_NodeBlockContainer.AddManipulator(new ClickSelector());

            m_NodeContainer.AddChild(m_NodeBlockContainer);

 			m_NodeContainer.AddManipulator(new ContextualMenu((evt, customData) =>
 			{
 				var menu = new GenericMenu();
 				menu.AddItem(new GUIContent("Add NodeBlock"), false,
 							 contentView => AddNodeBlock(),
 							 this);
 				menu.ShowAsContext();
 				return EventPropagation.Continue;
 			}));

            classList = new ClassList("VFXContext");
        }

		public override void SetPosition(Rect newPos)
		{
			if (classList.Contains("vertical"))
			{
				base.SetPosition(newPos);
			}
			else
			{
				positionType = PositionType.Absolute;
				positionLeft = newPos.x;
				positionTop = newPos.y;
			}
		}

        private void AddNodeBlock()
        {
            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
            presenter.AddNodeBlock();
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            
            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
            VFXContextDesc.Type contextType = presenter.Model.ContextType;

            RemoveFromClassList("init", "update", "output");

            switch (contextType)
            {
                case VFXContextDesc.Type.kTypeInit: AddToClassList("init"); break;
                case VFXContextDesc.Type.kTypeUpdate: AddToClassList("update"); break;
                case VFXContextDesc.Type.kTypeOutput: AddToClassList("output"); break;
                default: throw new Exception();
            }

            if (presenter.Model.Position != presenter.position.position)
            {
                presenter.Model.Position = presenter.position.position;  
                // Needs to make the model dirty
            }

            m_FlowInputConnectorContainer.ClearChildren();
            m_FlowOutputConnectorContainer.ClearChildren();

            foreach(var inanchorpresenter in presenter.inputAnchors)
            {
                var edge = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(inanchorpresenter);
                m_FlowInputConnectorContainer.AddChild(edge);
                presenter.m_view.RegisterFlowAnchorPresenter(inanchorpresenter);
            }

            foreach(var outanchorpresenter in presenter.outputAnchors)
            {
                var edge = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(outanchorpresenter);
                m_FlowOutputConnectorContainer.AddChild(edge);
                presenter.m_view.RegisterFlowAnchorPresenter(outanchorpresenter);
            }

            m_NodeBlockContainer.ClearChildren();
            foreach(var nodeblock in presenter.nodeBlockPresenters)
            {
                var blockUI = VFXNodeBlockPresenter.Create(nodeblock);
                m_NodeBlockContainer.AddChild(blockUI);
            }

        }

        public override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint(painter);
           // painter.DrawRect(position, m_RectColor);
        }


        // Declare new USS rect-color and use it
        public override void OnStylesResolved(VisualElementStyles elementStyles)
		{
			base.OnStylesResolved(elementStyles);
			elementStyles.ApplyCustomProperty(RectColorProperty, ref m_RectColor); 
		}

        StyleProperty<Color> m_RectColor;
        Color rectColor { get { return m_RectColor.GetOrDefault(Color.magenta); } }

        //private Color m_Color;
    }
}
