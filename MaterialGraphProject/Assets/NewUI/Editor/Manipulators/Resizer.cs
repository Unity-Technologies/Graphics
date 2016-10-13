using System;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums.Values;

namespace RMGUI.GraphView
{
	public class Resizer : Manipulator, IDecorator
	{
		public MouseButton activateButton { get; set; }

		// When applyInvTransform is true, Resizer will apply target's inverse transform before drawing/picking
		// (has the effect of being zoom factor independent)
		public bool applyInvTransform { get; set; }

		private Vector2 m_Start;
		private Rect m_StartPos;

		private string m_SizeStr;
		private Vector2 m_MinimumSize;

		// We need to delay style creation because we need to make sure we have a GUISkin loaded.
		private GUIStyle m_StyleWidget;
		private GUIStyle m_StyleLabel;

		private readonly Rect k_WidgetRect = new Rect(0, 0, 10, 6);
		private readonly Rect k_WidgetPickRect = new Rect(0, 0, 20, 20);
		private readonly Rect k_WidgetTextOffset = new Rect(0, 0, 5, 5);

		public Resizer()
		{
			m_MinimumSize = new Vector2(30.0f, 30.0f);
			m_SizeStr = "";
			activateButton = MouseButton.LeftMouse;
		}

		public Resizer(Vector2 minimumSize)
		{
			m_MinimumSize = minimumSize;
			m_SizeStr = "";
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			var ce = target as GraphElement;
			if (ce==null)
				return EventPropagation.Continue;

			var data = ce.dataProvider;
			if (data==null)
				return EventPropagation.Continue;

			if ( (data.capabilities & Capabilities.Resizable) != Capabilities.Resizable)
				return EventPropagation.Continue;

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (evt.button == (int) activateButton)
					{
						m_Start = evt.mousePosition;
						m_StartPos = target.position;

						var adjustedWidget = k_WidgetPickRect;
						if (applyInvTransform)
						{
							var inv = target.globalTransform.inverse;
							adjustedWidget.width *= inv.m00; // Apply scale
							adjustedWidget.height *= inv.m11;
						}

						var widget = m_StartPos;

						widget.x = widget.width - adjustedWidget.width;
						widget.y = widget.height - adjustedWidget.height;
						widget.width = adjustedWidget.width;
						widget.height = adjustedWidget.height;

						if (widget.Contains(m_Start))
						{
							// Warn user if target uses a relative CSS position type
							if (target.positionType != PositionType.Absolute)
							{
								Debug.LogWarning("Attempting to resize an object with a non absolute CSS position type");
							}
							this.TakeCapture();
							return EventPropagation.Stop;
						}
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture() && target.positionType == PositionType.Absolute)
					{
						var diff = evt.mousePosition - m_Start;
						var newSize = new Vector2(m_StartPos.width + diff.x, m_StartPos.height + diff.y);

						if (newSize.x < m_MinimumSize.x)
							newSize.x = m_MinimumSize.x;
						if (newSize.y < m_MinimumSize.y)
							newSize.y = m_MinimumSize.y;

						data.position = new Rect(data.position.x, data.position.y, newSize.x, newSize.y);

						m_SizeStr = String.Format("{0:0}", target.position.width) + "x" + String.Format("{0:0}", target.position.height);

						return EventPropagation.Stop;
					}
					return EventPropagation.Continue;

				case EventType.MouseUp:
					if (this.HasCapture() && evt.button == (int)activateButton)
					{
						this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return EventPropagation.Continue;
		}

		public void PrePaint(VisualElement t, PaintContext pc)
		{
		}

		public void PostPaint(VisualElement t, PaintContext pc)
		{
			// TODO: I would like to listen for skin change and create GUIStyle then and only then
			if (m_StyleWidget == null)
			{
				m_StyleWidget = new GUIStyle("WindowBottomResize") { fixedHeight = 0 };
			}

			if (m_StyleLabel == null)
			{
				m_StyleLabel = new GUIStyle("Label");
			}

			// Draw resize widget
			var widget = k_WidgetRect;
			if (applyInvTransform)
			{
				var inv = pc.worldXForm.inverse;
				widget.width *= inv.m00;
				widget.height *= inv.m11;
			}

			widget.position = new Vector2(target.position.max.x - widget.width, target.position.max.y - widget.height);

			// todo painter as drawimage
			m_StyleWidget.Draw(widget, GUIContent.none, 0);

			if (this.HasCapture())
			{
				// Get adjusted text offset
				var adjustedWidget = k_WidgetTextOffset;
				if (applyInvTransform)
				{
					var inv = pc.worldXForm.inverse;
					adjustedWidget.width *= inv.m00; // Apply scale
					adjustedWidget.height *= inv.m11;
				}

				// Now define widget to locate label
				widget = new Rect(target.position.max.x + adjustedWidget.width,
								  target.position.max.y + adjustedWidget.height,
								  200.0f, 20.0f);
				m_StyleLabel.Draw(widget, new GUIContent(m_SizeStr), false, false, false, false);
			}
		}
	}
}
