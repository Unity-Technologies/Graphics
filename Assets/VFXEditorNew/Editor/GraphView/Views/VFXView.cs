using System.Collections.Generic;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

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

            var bg = new GridBackground() { name = "vfx" };
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
        }

        void AddVFXContext(Vector2 pos,VFXContextDesc.Type contextType)
        {
            var context = new VFXContext(VFXContextDesc.CreateBasic(contextType));
            context.Position = pos;
            GetPresenter<VFXViewPresenter>().AddModel(context);
        } 
    }
}
