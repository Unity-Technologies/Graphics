using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.VFX.UI
{
    //[StyleSheet("Assets/VFXEditorNew/Editor/GraphView/Views/")]
    class VFXView : GraphView
    {
		public VFXView()
        {
            forceNotififcationOnAdd = true;
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
                    menu.AddItem(new GUIContent(desc.name), false,
                                 contentView => AddVFXContext(tPos, desc),
                                 this);

                menu.ShowAsContext();
                return EventPropagation.Continue;
            },null));

            typeFactory[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
            typeFactory[typeof(VFXFlowEdgePresenter)] = typeof(VFXFlowEdge);
            typeFactory[typeof(VFXDataEdgePresenter)] = typeof(VFXDataEdge);
            typeFactory[typeof(VFXFlowInputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXFlowOutputAnchorPresenter)] = typeof(VFXFlowAnchor);
            typeFactory[typeof(VFXDataInputAnchorPresenter)] = typeof(VFXDataAnchor);
            typeFactory[typeof(VFXDataOutputAnchorPresenter)] = typeof(VFXDataAnchor);

            AddStyleSheetPath("VFXView");

            Dirty(ChangeType.Transform);
        }

        void AddVFXContext(Vector2 pos,VFXModelDescriptor<VFXContext> desc)
        {
            GetPresenter<VFXViewPresenter>().AddVFXContext(pos,desc.CreateInstance());
        }
    }
}
