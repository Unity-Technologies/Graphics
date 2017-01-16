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

                // Needs to have the model here to filter compatible node blocks
                var contextType = GetPresenter<VFXContextPresenter>().Model.ContextType;
                foreach (var desc in VFXLibrary.GetBlocks())
                    if ((desc.CompatibleContexts & contextType) != 0)
                        menu.AddItem(new GUIContent(desc.Name), false,
                                     contentView => AddNodeBlock(-1, desc),
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

        private void AddNodeBlock(int index,VFXBlockDesc desc)
        {
            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
            presenter.AddNodeBlock(index, desc);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            
            VFXContextPresenter presenter = GetPresenter<VFXContextPresenter>();
            VFXContextType contextType = presenter.Model.ContextType;

            RemoveFromClassList("init", "update", "output");

            switch (contextType)
            {
                case VFXContextType.kInit:      AddToClassList("init"); break;
                case VFXContextType.kUpdate:    AddToClassList("update"); break;
                case VFXContextType.kOutput:    AddToClassList("output"); break;
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
            }

            foreach(var outanchorpresenter in presenter.outputAnchors)
            {
                var edge = VFXFlowAnchor.Create<VFXFlowEdgePresenter>(outanchorpresenter);
                m_FlowOutputConnectorContainer.AddChild(edge);         
            }

            m_NodeBlockContainer.ClearChildren();
            foreach(var nodeblock in presenter.nodeBlockPresenters)
            {
                var blockUI = VFXNodeBlockPresenter.Create(nodeblock);
                m_NodeBlockContainer.AddChild(blockUI);
            }

            // Recreate label with good name // Dirty
            m_NodeContainer.RemoveChildAt(0);
            m_NodeContainer.InsertChild(0, new Label(new GUIContent(presenter.Model.Desc.Name)));

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
