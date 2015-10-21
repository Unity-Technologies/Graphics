using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEditorInternal; 
using System.Reflection;
using Object = UnityEngine.Object;

namespace UnityEditor
{
	namespace Experimental
	{
		/// <summary>
		/// CanvasLayout : the base class for vertical and horizontal layouts
		/// WARNING: these layout classes have pretty limited usage.
		/// </summary>
		/// <remarks>
		/// Do not use this class directly. Use on of the specializations or derive your own
		/// </remarks>
		internal class CanvasLayout
		{
			protected CanvasElement m_Owner = null;
			protected CanvasLayout m_Parent = null;
			protected Vector4 m_Padding = Vector4.zero;
			protected float m_Left = 0;
			protected float m_Height = 0;
			protected float m_Width = 0;
			protected List<CanvasLayout> m_Children = new List<CanvasLayout>();
			protected List<CanvasElement> m_Elements = new List<CanvasElement>();

			public float Height
			{
				get
				{
					float maxHeight = m_Height;
					foreach (CanvasLayout l in m_Children)
					{
						maxHeight = Mathf.Max(maxHeight, l.Height);
					}
					return maxHeight + PaddingTop + PaddingBottom;
				}
			}

			public float Width
			{
				get
				{
					float maxWidth = m_Width;
					foreach (CanvasLayout l in m_Children)
					{
						maxWidth = Mathf.Max(maxWidth, l.Width);
					}
					return maxWidth + PaddingLeft + PaddingRight;
				}
			}

			public CanvasLayout(CanvasElement e)
			{
				m_Owner = e;
			}

			public CanvasLayout(CanvasLayout p)
			{
				m_Parent = p;
				m_Owner = p.Owner();
			}

			public CanvasElement Owner()
			{
				if (m_Owner != null)
					return m_Owner;

				if (m_Parent != null)
					return m_Parent.Owner();

				return null;
			}

			public float Left
			{
				get { return m_Left; }
				set { m_Left = value; }
			}

			public float PaddingLeft
			{
				get { return m_Padding.w; }
				set { m_Padding.w = value; }
			}

			public float PaddingRight
			{
				get { return m_Padding.y; }
				set { m_Padding.y = value; }
			}

			public float PaddingTop
			{
				get { return m_Padding.x; }
				set { m_Padding.x = value; }
			}

			public float PaddingBottom
			{
				get { return m_Padding.z; }
				set { m_Padding.z = value; }
			}

			public virtual void LayoutElement(CanvasElement c)
			{
				m_Elements.Add(c);
			}
			
			public virtual void LayoutElements(CanvasElement[] arrayOfElements)
			{
				for (int a = 0; a < arrayOfElements.Length; a++)
				{
					m_Elements.Add(arrayOfElements[a]);
				}
			}

			public void AddSpacer(int pixels)
			{
				float collapsedFactor = m_Owner.IsCollapsed() ? 0.0f : 1.0f;
				m_Height += pixels*collapsedFactor;
			}

			public virtual void DebugDraw()
			{

			}
		};

		/// <summary>
		/// CanvasVerticalLayout : Helps layouting a group of canvas elements vertically
		/// </summary>
		internal class CanvasVerticalLayout : CanvasLayout
		{
			public CanvasVerticalLayout(CanvasElement e)
				: base(e)
			{

			}

			public override void LayoutElements(CanvasElement[] arrayOfElements)
			{
				for (int a = 0; a < arrayOfElements.Length; a++)
				{
					LayoutElement(arrayOfElements[a]);
				}
				base.LayoutElements(arrayOfElements);
			}

			public override void LayoutElement(CanvasElement c)
			{
				float collapsedFactor = m_Owner.IsCollapsed() ? 0.0f : 1.0f;
				if ((c.Caps & CanvasElement.Capabilities.DoesNotCollapse) == CanvasElement.Capabilities.DoesNotCollapse)
				{
					collapsedFactor = 1.0f;
				}

				m_Height += m_Padding.x*collapsedFactor;
				//c.translation = new Vector3(m_Padding.y + c.translation.x, Height*collapsedFactor, 0.0f);
				c.translation = new Vector3(c.translation.x, Height * collapsedFactor, 0.0f);
				c.scale = new Vector3(c.scale.x, c.scale.y*collapsedFactor, 1.0f);
				m_Height += (c.scale.y + m_Padding.z)*collapsedFactor;
				m_Width = Mathf.Max(m_Width, c.scale.x);
				Owner().AddChild(c);
				base.LayoutElement(c);
			}

			public override void DebugDraw()
			{
				if (m_Elements.Count() == 0)
					return;

				Rect encompassingRect = m_Elements[0].canvasBoundingRect;
				List<Rect> elementRects = new List<Rect>();
				foreach (CanvasElement e in m_Elements)
				{
					elementRects.Add(e.canvasBoundingRect);
					encompassingRect = RectUtils.Encompass(encompassingRect, e.canvasBoundingRect);
				}

				Vector3[] points =
				{
					new Vector3(encompassingRect.xMin, encompassingRect.yMin, 0.0f),
					new Vector3(encompassingRect.xMax, encompassingRect.yMin, 0.0f),
					new Vector3(encompassingRect.xMax, encompassingRect.yMax, 0.0f),
					new Vector3(encompassingRect.xMin, encompassingRect.yMax, 0.0f)
				};

				Color prevColor = GUI.color;
				GUI.color = new Color(1.0f, 0.6f, 0.0f, 1.0f);
				Handles.DrawDottedLine(points[0], points[1], 5.0f);
				Handles.DrawDottedLine(points[1], points[2], 5.0f);
				Handles.DrawDottedLine(points[2], points[3], 5.0f);
				Handles.DrawDottedLine(points[3], points[0], 5.0f);
				GUI.color = prevColor;

				foreach (Rect r in elementRects)
				{
					Vector2 from = new Vector2(r.xMin, r.yMax);
					Vector2 to = new Vector2(encompassingRect.xMax, r.yMax);

					DrawDottedLine(from, to, 5.0f, new Color(1.0f, 0.6f, 0.0f, 1.0f));
				}
			}

			private void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
			{
				UIHelpers.ApplyWireMaterial();

				GL.Begin(GL.LINES);
				GL.Color(col);

				float length = Vector3.Distance(p1, p2); // ignore z component
				int count = Mathf.CeilToInt(length/segmentsLength);
				for (int i = 0; i < count; i += 2)
				{
					GL.Vertex((Vector3.Lerp(p1, p2, i*segmentsLength/length)));
					GL.Vertex((Vector3.Lerp(p1, p2, (i + 1)*segmentsLength/length)));
				}

				GL.End();
			}

		};


		internal class UIHelpers
		{
			static MethodInfo ApplyWireMaterialMI = null;

			public static void ApplyWireMaterial()
			{
				if (ApplyWireMaterialMI == null)
				{
					ApplyWireMaterialMI = typeof(HandleUtility).GetMethod("ApplyWireMaterial", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				}

				if (ApplyWireMaterialMI != null)
				{
					ApplyWireMaterialMI.Invoke(null, null);
				}
			}
		}
	}
}
