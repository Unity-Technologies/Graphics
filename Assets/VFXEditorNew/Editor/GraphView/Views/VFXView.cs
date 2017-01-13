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

                foreach(var desc in VFXLibrary.GetContexts())
                    menu.AddItem(new GUIContent(desc.Name), false,
                                 contentView => AddVFXContext(tPos, desc),
                                 this);

                menu.ShowAsContext();
                return EventPropagation.Continue;
            }));

            dataMapper[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
            dataMapper[typeof(VFXFlowEdgePresenter)] = typeof(VFXFlowEdge);
            dataMapper[typeof(VFXFlowInputAnchorPresenter)] = typeof(VFXFlowAnchor);
            dataMapper[typeof(VFXFlowOutputAnchorPresenter)] = typeof(VFXFlowAnchor);
        }

        void AddVFXContext(Vector2 pos,VFXContextDesc desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXContext(pos,desc);
        }
           
    }
}
