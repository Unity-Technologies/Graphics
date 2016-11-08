using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;

namespace RMGUI.GraphView.Demo
{
	public class IMGUIElement : SimpleElement
	{
		private IMGUIContainer m_Container;
		public IMGUIElement()
		{
			m_Container = new IMGUIContainer()
			{
				positionType = PositionType.Absolute,
				positionLeft = 0,
				positionTop = 20,
				positionRight = 0,
				positionBottom = 0,
				OnGUIHandler = OnGUIHandler
			};
			AddChild(m_Container);
		}

		public virtual void OnGUIHandler()
		{
			var imguiData = GetData<IMGUIData>();
			if (imguiData != null)
			{
				imguiData.OnGUIHandler();
			}
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			m_Container.executionContext = dataProvider.GetInstanceID();
		}
	}
}
