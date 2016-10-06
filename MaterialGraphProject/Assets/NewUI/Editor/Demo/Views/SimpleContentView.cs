using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	public class SimpleContentView : GraphView
	{
		public SimpleContentView()
		{
			AddManipulator(new ContentZoomer());
			AddManipulator(new ContentDragger());
			AddManipulator(new RectangleSelector());
			AddManipulator(new SelectionDragger());
			AddManipulator(new ClickSelector());

			AddDecorator(new GridBackground(contentViewContainer));
		}
	}
}
