using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;

namespace RMGUI.GraphView
{
	public class IMGUIElement : SimpleElement
	{
		protected IMGUIContainer m_Container;
		public IMGUIElement()
		{
			m_Container = new IMGUIContainer
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
			var imguiPresenter = GetPresenter<IMGUIPresenter>();
			imguiPresenter.OnGUIHandler();
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			m_Container.executionContext = presenter.GetInstanceID();
		}
	}
}
