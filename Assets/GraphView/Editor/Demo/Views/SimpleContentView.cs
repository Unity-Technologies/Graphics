using System.Collections.Generic;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[StyleSheet("Assets/GraphView/Editor/Demo/Views/SimpleContentView.uss")]
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

			typeFactory[typeof(CirclePresenter)] = typeof(Circle);
			typeFactory[typeof(InvisibleBorderContainerPresenter)] = typeof(InvisibleBorderContainer);
			typeFactory[typeof(MiniMapPresenter)] = typeof(MiniMap);
			typeFactory[typeof(SimpleElementPresenter)] = typeof(SimpleElement);
			typeFactory[typeof(WWWImagePresenter)] = typeof(WWWImage);
			typeFactory[typeof(IMGUIPresenter)] = typeof(IMGUIElement);
			typeFactory[typeof(CommentPresenter)] = typeof(Comment);
		}
	}
}
