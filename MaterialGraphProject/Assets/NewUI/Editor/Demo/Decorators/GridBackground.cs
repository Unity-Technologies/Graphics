using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView.Demo
{
	public class GridBackground : IDecorator
	{
		const string SpacingProperty = "spacing";
		const string ThickLinesProperty = "thick-lines";
		const string LineColorProperty = "line-color";
		const string ThickLineColorProperty = "thick-line-color";
		// Most likely will be a built-in style property soon
		const string BackgroundColorProperty = "background-color";

		public float spacing
		{
			get
			{
				return m_Container.GetStyleFloat(SpacingProperty, 50.0f);
			}
			set
			{
				m_Container.SetStyleFloat(SpacingProperty, value);
			}
		}

		public int thickLines
		{
			get
			{
				return m_Container.GetStyleInt(ThickLinesProperty, 10);
			}
			set
			{
				m_Container.SetStyleInt(ThickLinesProperty, value);
			}
		}

		public Color lineColor
		{
			get
			{
				return m_Container.GetStyleColor(LineColorProperty, new Color(0f, 0f, 0f, 0.18f));
			}
			set
			{
				m_Container.SetStyleColor(LineColorProperty, value);
			}
		}

		public Color thickLineColor
		{
			get
			{
				return m_Container.GetStyleColor(ThickLineColorProperty, new Color(0f, 0f, 0f, 0.38f));
			}
			set
			{
				m_Container.SetStyleColor(ThickLineColorProperty, value);
			}
		}

		public Color backgroundColor
		{
			get
			{
				return m_Container.GetStyleColor(BackgroundColorProperty, new Color(0.17f, 0.17f, 0.17f, 1.0f));
			}
			set
			{
				m_Container.SetStyleColor(BackgroundColorProperty, value);
			}
		}

		private readonly VisualContainer m_Container;

		public GridBackground()
		{ }

		public GridBackground(VisualContainer container)
		{
			m_Container = container;
		}

		private Vector3 Clip(Rect clipRect, Vector3 _in)
		{
			if (_in.x < clipRect.xMin)
				_in.x = clipRect.xMin;
			if (_in.x > clipRect.xMax)
				_in.x = clipRect.xMax;

			if (_in.y < clipRect.yMin)
				_in.y = clipRect.yMin;
			if (_in.y > clipRect.yMax)
				_in.y = clipRect.yMax;

			return _in;
		}

		public void PrePaint(VisualElement target, PaintContext pc)
		{
			Rect clientRect = target.position;

			VisualElement e = m_Container ?? target;
			var targetScale = new Vector3(e.transform.GetColumn(0).magnitude,
										  e.transform.GetColumn(1).magnitude,
										  e.transform.GetColumn(2).magnitude);
			var targetTranslation = e.transform.GetColumn(3);
			var targetPosition = e.position;

			// background
			UIHelpers.ApplyWireMaterial();

			GL.Begin(GL.QUADS);
				GL.Color(backgroundColor);
				GL.Vertex(new Vector3(clientRect.x, clientRect.y));
				GL.Vertex(new Vector3(clientRect.xMax, clientRect.y));
				GL.Vertex(new Vector3(clientRect.xMax, clientRect.yMax));
				GL.Vertex(new Vector3(clientRect.x, clientRect.yMax));
			GL.End();

			// vertical lines
			Vector3 from = new Vector3(clientRect.x, clientRect.y, 0.0f);
			Vector3 to = new Vector3(clientRect.x, clientRect.height, 0.0f);

			var tx = Matrix4x4.TRS(targetTranslation, Quaternion.identity, Vector3.one);

			from = tx.MultiplyPoint(from);
			to = tx.MultiplyPoint(to);

			from.x += (targetPosition.x * targetScale.x);
			from.y += (targetPosition.y * targetScale.y);
			to.x += (targetPosition.x * targetScale.x);
			to.y += (targetPosition.y * targetScale.y);

			Handles.DrawWireDisc(from, new Vector3(0.0f, 0.0f, -1.0f), 6f);

			float thickGridLineX = from.x;
			float thickGridLineY = from.y;

			// Update from/to to start at beginning of clientRect
			from.x = (from.x % (spacing * (targetScale.x)) - (spacing * (targetScale.x)));
			to.x = from.x;

			from.y = clientRect.y;
			to.y = clientRect.y + clientRect.height;

			while (from.x < clientRect.width)
			{
				from.x += spacing * targetScale.x;
				to.x += spacing * targetScale.x;

				GL.Begin(GL.LINES);
					GL.Color(lineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
				GL.End();
			}

			float thickLineSpacing = (spacing * thickLines);
			from.x = to.x = (thickGridLineX % (thickLineSpacing * (targetScale.x)) - (thickLineSpacing * (targetScale.x)));

			while (from.x < clientRect.width + thickLineSpacing)
			{
				GL.Begin(GL.LINES);
					GL.Color(thickLineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
				GL.End();

				from.x += (spacing * targetScale.x * thickLines);
				to.x += (spacing * targetScale.x * thickLines);
			}

			// horizontal lines
			from = new Vector3(clientRect.x, clientRect.y, 0.0f);
			to = new Vector3(clientRect.x + clientRect.width, clientRect.y, 0.0f);

			from.x += (targetPosition.x * targetScale.x);
			from.y += (targetPosition.y * targetScale.y);
			to.x += (targetPosition.x * targetScale.x);
			to.y += (targetPosition.y * targetScale.y);

			from = tx.MultiplyPoint(from);
			to = tx.MultiplyPoint(to);

			from.y = to.y = (from.y % (spacing * (targetScale.y)) - (spacing * (targetScale.y)));
			from.x = clientRect.x;
			to.x = clientRect.width;

			while (from.y < clientRect.height)
			{
				from.y += spacing * targetScale.y;
				to.y += spacing * targetScale.y;

				GL.Begin(GL.LINES);
					GL.Color(lineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
				GL.End();
			}

			thickLineSpacing = spacing * thickLines;
			from.y = to.y = (thickGridLineY % (thickLineSpacing * (targetScale.y)) - (thickLineSpacing * (targetScale.y)));

			while (from.y < clientRect.height + thickLineSpacing)
			{
				GL.Begin(GL.LINES);
					GL.Color(thickLineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
				GL.End();

				from.y += spacing * targetScale.y * thickLines;
				to.y += spacing * targetScale.y * thickLines;
			}
		}

		public void PostPaint(VisualElement target, PaintContext pc)
		{
		}
	}
}
