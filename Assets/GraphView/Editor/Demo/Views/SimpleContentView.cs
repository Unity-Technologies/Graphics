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

			typeMapper[typeof(CirclePresenter)] = typeof(Circle);
			typeMapper[typeof(InvisibleBorderContainerPresenter)] = typeof(InvisibleBorderContainer);
			typeMapper[typeof(MiniMapPresenter)] = typeof(MiniMap);
			typeMapper[typeof(SimpleElementPresenter)] = typeof(SimpleElement);
			typeMapper[typeof(WWWImagePresenter)] = typeof(WWWImage);
			typeMapper[typeof(IMGUIPresenter)] = typeof(IMGUIElement);
			typeMapper[typeof(CommentPresenter)] = typeof(Comment);
		}
	}
}
