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
					{Event.KeyboardEvent("delete"), DeleteSelection}
				}));

			AddDecorator(new GridBackground());

			dataMapper[typeof(CircleData)] = typeof(Circle);
			dataMapper[typeof(InvisibleBorderContainerData)] = typeof(InvisibleBorderContainer);
			dataMapper[typeof(MiniMapData)] = typeof(MiniMap);
			dataMapper[typeof(SimpleElementData)] = typeof(SimpleElement);
			dataMapper[typeof(WWWImageData)] = typeof(WWWImage);
			dataMapper[typeof(IMGUIData)] = typeof(IMGUIElement);
		}
	}
}
