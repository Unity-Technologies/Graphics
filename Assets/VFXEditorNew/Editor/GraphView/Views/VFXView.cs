using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    [StyleSheet("Assets/VFXEditorNew/Editor/GraphView/Views/VFXView.uss")]
    class VFXView : GraphView
    {
        public List<NodeAnchorPresenter> m_FlowAnchorPresenters;

        public VFXView()
		{
            AddManipulator(new ContentZoomer());
			AddManipulator(new ContentDragger());
			AddManipulator(new RectangleSelector());
			AddManipulator(new SelectionDragger());
			AddManipulator(new ClickSelector());
			AddManipulator(new ShortcutHandler(
				new Dictionary<Event, ShortcutDelegate>
				{
					{Event.KeyboardEvent("a"), FrameAll},
					{Event.KeyboardEvent("f"), FrameSelection},
					{Event.KeyboardEvent("o"), FrameOrigin},
					{Event.KeyboardEvent("delete"), DeleteSelection},
					{Event.KeyboardEvent("#tab"), FramePrev},
					{Event.KeyboardEvent("tab"), FrameNext}
				}));

            var bg = new GridBackground() { name = "VFXBackgroundGrid" };
            InsertChild(0, bg);

            AddManipulator(new ContextualMenu((evt, customData) =>
            {
                var menu = new GenericMenu();
                Vector2 tPos = this.ChangeCoordinatesTo(contentViewContainer, evt.mousePosition);

                menu.AddItem(new GUIContent("Add Init"), false,
                             contentView => AddVFXContext(tPos, VFXContextDesc.Type.kTypeInit),
                             this);

                menu.AddItem(new GUIContent("Add Update"), false,
                             contentView => AddVFXContext(tPos, VFXContextDesc.Type.kTypeUpdate),
                             this);

                menu.AddItem(new GUIContent("Add Output"), false,
                             contentView => AddVFXContext(tPos, VFXContextDesc.Type.kTypeOutput),
                             this);

                menu.ShowAsContext();
                return EventPropagation.Continue;
            }));

            dataMapper[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
            dataMapper[typeof(VFXFlowEdgePresenter)] = typeof(VFXFlowEdge);
            dataMapper[typeof(VFXFlowInputAnchorPresenter)] = typeof(VFXFlowAnchor);
            dataMapper[typeof(VFXFlowOutputAnchorPresenter)] = typeof(VFXFlowAnchor);

            m_FlowAnchorPresenters = new List<NodeAnchorPresenter>();

        }

        void AddVFXContext(Vector2 pos,VFXContextDesc.Type contextType)
        {
            var context = new VFXContext(VFXContextDesc.CreateBasic(contextType));
            context.Position = pos;
            GetPresenter<VFXViewPresenter>().AddModel(context,this);
        }
            
        public void RegisterFlowAnchorPresenter(NodeAnchorPresenter presenter)
        {
            if(!m_FlowAnchorPresenters.Contains(presenter))
            {
                m_FlowAnchorPresenters.Add(presenter);
            }
        }

        public override List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchor, NodeAdapter nodeAdapter)
        {
            return m_FlowAnchorPresenters
			.Where(nap => nap.IsConnectable() &&
							nap.direction != startAnchor.direction &&
							nodeAdapter.GetAdapter(nap.source, startAnchor.source) != null)
			.ToList();
        }
    }
}
