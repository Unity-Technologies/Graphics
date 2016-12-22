using System.Collections.Generic;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[StyleSheet("Assets/NewUI/Editor/Demo/Views/SimpleContentView.uss")]
	public class SimpleContentView : GraphView
	{
		public SimpleContentView()
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

			InsertChild(0, new GridBackground());

			dataMapper[typeof(CirclePresenter)] = typeof(Circle);
			dataMapper[typeof(InvisibleBorderContainerPresenter)] = typeof(InvisibleBorderContainer);
			dataMapper[typeof(MiniMapPresenter)] = typeof(MiniMap);
			dataMapper[typeof(SimpleElementPresenter)] = typeof(SimpleElement);
			dataMapper[typeof(WWWImagePresenter)] = typeof(WWWImage);
			dataMapper[typeof(IMGUIPresenter)] = typeof(IMGUIElement);
		}
	}
}
