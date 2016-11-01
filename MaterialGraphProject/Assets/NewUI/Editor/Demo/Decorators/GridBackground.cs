using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace RMGUI.GraphView.Demo
{
	public class GridBackground : IDecorator, IStyleTarget
	{
		const string SpacingProperty = "spacing";
		const string ThickLinesProperty = "thick-lines";
		const string LineColorProperty = "line-color";
		const string ThickLineColorProperty = "thick-line-color";
		const string GridBackgroundColorProperty = "grid-background-color";

		StyleProperty<float> m_Spacing;
		float spacing
		{
			get
			{
				return m_Spacing.GetOrDefault(50.0f);
			}
		}

		StyleProperty<int> m_ThickLines;
		int thickLines
		{
			get
			{
				return m_ThickLines.GetOrDefault(10);
			}
		}

		StyleProperty<Color> m_LineColor;
		Color lineColor
		{
			get
			{
				return m_LineColor.GetOrDefault(new Color(0f, 0f, 0f, 0.18f));
			}
		}

		StyleProperty<Color> m_ThickLineColor;
		Color thickLineColor
		{
			get
			{
				return m_ThickLineColor.GetOrDefault(new Color(0f, 0f, 0f, 0.38f));
			}
		}

		StyleProperty<Color> m_BackgroundColor;
		Color backgroundColor
		{
			get
			{
				return m_BackgroundColor.GetOrDefault(new Color(0.17f, 0.17f, 0.17f, 1.0f));
			}
		}

		private VisualContainer m_Container;

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

		// Notes: styles are read from this manipulator's target,
		// note its m_Container member
		public void OnStylesResolved(VisualElementStyles styles)
		{
			styles.ApplyCustomProperty(SpacingProperty, ref m_Spacing);
			styles.ApplyCustomProperty(ThickLinesProperty, ref m_ThickLines);
			styles.ApplyCustomProperty(ThickLineColorProperty, ref m_ThickLineColor);
			styles.ApplyCustomProperty(LineColorProperty, ref m_LineColor);
			styles.ApplyCustomProperty(GridBackgroundColorProperty, ref m_BackgroundColor);
		}

		public void PrePaint(VisualElement target, IStylePainter pc)
		{
			var graphView = target as GraphView;
			if (graphView == null)
			{
				throw new InvalidOperationException("GridBackground decorator can only be added to a GraphView");
			}
			m_Container = graphView.contentViewContainer;
			Rect clientRect = graphView.position;

			var containerScale = new Vector3(m_Container.transform.GetColumn(0).magnitude,
				m_Container.transform.GetColumn(1).magnitude,
				m_Container.transform.GetColumn(2).magnitude);
			var containerTranslation = m_Container.transform.GetColumn(3);
			var containerPosition = m_Container.position;

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

			var tx = Matrix4x4.TRS(containerTranslation, Quaternion.identity, Vector3.one);

			from = tx.MultiplyPoint(from);
			to = tx.MultiplyPoint(to);

			from.x += (containerPosition.x * containerScale.x);
			from.y += (containerPosition.y * containerScale.y);
			to.x += (containerPosition.x * containerScale.x);
			to.y += (containerPosition.y * containerScale.y);

			Handles.DrawWireDisc(from, new Vector3(0.0f, 0.0f, -1.0f), 6f);

			float thickGridLineX = from.x;
			float thickGridLineY = from.y;

			// Update from/to to start at beginning of clientRect
			from.x = (from.x % (spacing * (containerScale.x)) - (spacing * (containerScale.x)));
			to.x = from.x;

			from.y = clientRect.y;
			to.y = clientRect.y + clientRect.height;

			while (from.x < clientRect.width)
			{
				from.x += spacing * containerScale.x;
				to.x += spacing * containerScale.x;

				GL.Begin(GL.LINES);
				GL.Color(lineColor);
				GL.Vertex(Clip(clientRect, from));
				GL.Vertex(Clip(clientRect, to));
				GL.End();
			}

			float thickLineSpacing = (spacing * thickLines);
			from.x = to.x = (thickGridLineX % (thickLineSpacing * (containerScale.x)) - (thickLineSpacing * (containerScale.x)));

			while (from.x < clientRect.width + thickLineSpacing)
			{
				GL.Begin(GL.LINES);
				GL.Color(thickLineColor);
				GL.Vertex(Clip(clientRect, from));
				GL.Vertex(Clip(clientRect, to));
				GL.End();

				from.x += (spacing * containerScale.x * thickLines);
				to.x += (spacing * containerScale.x * thickLines);
			}

			// horizontal lines
			from = new Vector3(clientRect.x, clientRect.y, 0.0f);
			to = new Vector3(clientRect.x + clientRect.width, clientRect.y, 0.0f);

			from.x += (containerPosition.x * containerScale.x);
			from.y += (containerPosition.y * containerScale.y);
			to.x += (containerPosition.x * containerScale.x);
			to.y += (containerPosition.y * containerScale.y);

			from = tx.MultiplyPoint(from);
			to = tx.MultiplyPoint(to);

			from.y = to.y = (from.y % (spacing * (containerScale.y)) - (spacing * (containerScale.y)));
			from.x = clientRect.x;
			to.x = clientRect.width;

			while (from.y < clientRect.height)
			{
				from.y += spacing * containerScale.y;
				to.y += spacing * containerScale.y;

				GL.Begin(GL.LINES);
				GL.Color(lineColor);
				GL.Vertex(Clip(clientRect, from));
				GL.Vertex(Clip(clientRect, to));
				GL.End();
			}

			thickLineSpacing = spacing * thickLines;
			from.y = to.y = (thickGridLineY % (thickLineSpacing * (containerScale.y)) - (thickLineSpacing * (containerScale.y)));

			while (from.y < clientRect.height + thickLineSpacing)
			{
				GL.Begin(GL.LINES);
				GL.Color(thickLineColor);
				GL.Vertex(Clip(clientRect, from));
				GL.Vertex(Clip(clientRect, to));
				GL.End();

				from.y += spacing * containerScale.y * thickLines;
				to.y += spacing * containerScale.y * thickLines;
			}
		}

		public void PostPaint(VisualElement element, IStylePainter pc)
		{
		}
	}
}
