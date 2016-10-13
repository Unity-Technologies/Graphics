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
			var imguiData = GetData<IMGUIData>();
			if (imguiData != null)
			{
				imguiData.OnGUIHandler();
			}
		}
	}
}
