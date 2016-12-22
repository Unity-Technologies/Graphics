using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class WWWImage : SimpleElement
	{
		private Texture2D m_WwwTexture;
		WWW m_Www;
		bool m_IsScheduled;

		VisualElement m_ImageHolder;

		public WWWImage()
		{
			m_Www = new WWW("http://lorempixel.com/200/200");
			onEnter += SchedulePolling;
			onLeave += UnschedulePolling;
			pickingMode = PickingMode.Position;
			m_ImageHolder = new VisualElement {content = new GUIContent("Loading ...")};
			AddChild(m_ImageHolder);
		}

		private void SchedulePolling()
		{
			if (panel != null)
			{
				if (!m_IsScheduled)
				{
					this.Schedule(CheckForWWW).StartingIn(0).Every(1000);
					m_IsScheduled = true;
				}
			}
			else
			{
				m_IsScheduled = false;
			}
		}

		private void UnschedulePolling()
		{
			if (m_IsScheduled && panel != null)
			{
				this.Unschedule(CheckForWWW);
			}
			m_IsScheduled = false;
		}

		private void CheckForWWW(TimerState timerState)
		{
			if (!m_Www.isDone)
			{
				return;
			}

			if (m_WwwTexture == null)
			{
				m_WwwTexture = new Texture2D(4, 4, TextureFormat.DXT1, false);
				m_ImageHolder.content.text = string.Empty;
				m_ImageHolder.backgroundImage = m_WwwTexture;
			}
			m_Www.LoadImageIntoTexture(m_WwwTexture);
			m_Www = new WWW("http://lorempixel.com/200/200");

			this.Touch(ChangeType.Repaint);
		}
	}
}
