using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;

namespace RMGUI.GraphView
{
	public class Resizer : VisualElement
	{
		private Vector2 m_Start;
		private Rect m_StartPos;

		public MouseButton activateButton { get; set; }

		private Vector2 m_MinimumSize;

		// We need to delay style creation because we need to make sure we have a GUISkin loaded.
		private GUIStyle m_StyleWidget;
		private GUIStyle m_StyleLabel;
		private GUIContent m_LabelText = new GUIContent();

		private readonly Rect k_WidgetTextOffset = new Rect(0, 0, 5, 5);

		public Resizer()
		{
			m_MinimumSize = new Vector2(30.0f, 30.0f);
			activateButton = MouseButton.LeftMouse;

			positionType = PositionType.Absolute;
			positionTop = float.NaN;
			positionLeft = float.NaN;
			positionBottom = 0;
			positionRight = 0;

			// make clickable area bigger than render area
			paddingLeft = 10;
			paddingTop = 14;
			width = 20;
			height = 20;
		}

		public Resizer(Vector2 minimumSize)
		{
			m_MinimumSize = minimumSize;
			positionType = PositionType.Absolute;
			positionTop = float.NaN;
			positionLeft = float.NaN;
			positionBottom = 0;
			positionRight = 0;
			// make clickable area bigger than render area
			paddingLeft = 10;
			paddingTop = 14;
			width = 20;
			height = 20;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			var ce = parent as GraphElement;
			if (ce == null)
				return EventPropagation.Continue;

			GraphElementPresenter presenter = ce.presenter;
			if (presenter ==  null)
				return EventPropagation.Continue;

			if ((presenter.capabilities & Capabilities.Resizable) != Capabilities.Resizable)
				return EventPropagation.Continue;

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (evt.button == (int)activateButton)
					{
						m_Start = this.ChangeCoordinatesTo(parent,evt.mousePosition);
						m_StartPos = parent.position;
						// Warn user if target uses a relative CSS position type
						if (parent.positionType != PositionType.Absolute)
						{
							Debug.LogWarning("Attempting to resize an object with a non absolute CSS position type");
						}
						this.TakeCapture();
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture() && parent.positionType == PositionType.Absolute)
					{
						Vector2 diff = this.ChangeCoordinatesTo(parent,evt.mousePosition) - m_Start;
						var newSize = new Vector2(m_StartPos.width + diff.x, m_StartPos.height + diff.y);

						if (newSize.x < m_MinimumSize.x)
							newSize.x = m_MinimumSize.x;
						if (newSize.y < m_MinimumSize.y)
							newSize.y = m_MinimumSize.y;

						presenter.position = new Rect(presenter.position.x, presenter.position.y, newSize.x, newSize.y);

						m_LabelText.text = String.Format("{0:0}", parent.position.width) + "x" + String.Format("{0:0}", parent.position.height);

						return EventPropagation.Stop;
					}
					return EventPropagation.Continue;

				case EventType.MouseUp:
					if (evt.button == (int)activateButton && this.HasCapture())
					{
						this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return EventPropagation.Continue;
		}

		public override void DoRepaint(IStylePainter painter)
		{
			// TODO: I would like to listen for skin change and create GUIStyle then and only then
			if (m_StyleWidget == null)
			{
				m_StyleWidget = new GUIStyle("WindowBottomResize") { fixedHeight = 0 };
				content = new GUIContent(m_StyleWidget.normal.background);
			}

			base.DoRepaint(painter);

			if (m_StyleLabel == null)
			{
				m_StyleLabel = new GUIStyle("Label");
			}

			if (this.HasCapture())
			{
				// Get adjusted text offset
				Rect adjustedWidget = k_WidgetTextOffset;

				// Now define widget to locate label
				var widget = new Rect(position.max.x + adjustedWidget.width,
								  position.max.y + adjustedWidget.height,
								  200.0f, 20.0f);

				m_StyleLabel.Draw(widget, m_LabelText, false, false, false, false);
			}
		}
	}
}
