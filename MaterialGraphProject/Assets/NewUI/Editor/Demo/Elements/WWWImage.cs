using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[CustomDataView(typeof(WWWImageData))]
	public class WWWImage : SimpleElement
	{
		private Texture2D m_WwwTexture;
		WWW m_Www;
		bool m_IsScheduled;

		public WWWImage()
		{
			m_Www = new WWW("http://lorempixel.com/200/200");
			onEnter += SchedulePolling;
			onLeave += UnschedulePolling;
			pickingMode = PickingMode.Position;
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
				AddChild(new Image {image = m_WwwTexture});
			}

			m_Www.LoadImageIntoTexture(m_WwwTexture);

			m_Www = new WWW("http://lorempixel.com/200/200");

			this.Touch(ChangeType.Repaint);
		}
	}
}
