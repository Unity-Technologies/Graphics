using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[StyleSheet("Assets/Editor/Demo/Views/SimpleContentView.uss")]
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
