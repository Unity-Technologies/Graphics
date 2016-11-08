using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	static class PainterExtensions
	{
		// todo arguably the transform should be on context already
		public static void DrawRectangleOutline(this IStylePainter painter, Matrix4x4 transform, Rect rectangle, Color outlineColor)
		{
			Vector3[] verts =
				{
					new Vector3(rectangle.xMin, rectangle.yMin, 0.0f),
					new Vector3(rectangle.xMax, rectangle.yMin, 0.0f),
					new Vector3(rectangle.xMax, rectangle.yMax, 0.0f),
					new Vector3(rectangle.xMin, rectangle.yMax, 0.0f)
				};

			UIHelpers.ApplyWireMaterial();

			GL.PushMatrix();
			GL.MultMatrix(transform);

			if (outlineColor.a > 0)
			{
				Color col = outlineColor;
				GL.Begin(GL.LINES);
				GL.Color(col);
				for (int i = 0; i < 4; i++)
				{
					GL.Vertex(verts[i]);
					GL.Vertex(verts[(i + 1) % 4]);
				}
				GL.End();
			}

			GL.PopMatrix();
		}
	}
}
