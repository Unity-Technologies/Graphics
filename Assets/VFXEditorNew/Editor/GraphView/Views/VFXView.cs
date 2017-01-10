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

            dataMapper[typeof(VFXContextPresenter)] = typeof(VFXContextUI);
        }
    }
}
