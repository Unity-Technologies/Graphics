using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums.Values;

namespace RMGUI.GraphView.Demo
{
	public class IMGUIElement : SimpleElement
	{
		public IMGUIElement()
		{
			var imgui = new IMGUIContainer()
			{
				positionType = PositionType.Absolute,
				positionLeft = 0,
				positionTop = 20,
				positionRight = 0,
				positionBottom = 0,
				OnGUIHandler = OnGUIHandler
			};
			AddChild(imgui);
		}

		public virtual void OnGUIHandler()
		{
			// Hum... probably not ideal to have to cast and check all the time. Need to find something better.

			var imguiData = dataProvider as IMGUIData;
			if (imguiData != null)
			{
				imguiData.OnGUIHandler();
			}
		}
	}
}
