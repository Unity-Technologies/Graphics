using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditorInternal; 
using Object = UnityEngine.Object;

#pragma warning disable 0414
#pragma warning disable 0219 

namespace UnityEditor
{
	namespace Experimental
	{
		public enum ManipulatorCapability
		{
			eMultiSelection = 0
		};

		public interface IManipulate
		{
			bool GetCaps(ManipulatorCapability cap);
			void AttachTo(CanvasElement e);
		}

		internal delegate bool ManipulateDelegate(Event e, Canvas2D parent, Object customData);

		internal class Draggable : IManipulate
		{
			private EventModifiers m_ActivatorModifiers;
			private int m_ActivatorButton = 0;

			public Draggable()
			{
				m_ActivatorButton = 0;
				m_ActivatorModifiers = EventModifiers.None;
			}

			public Draggable(int button, EventModifiers activator)
			{
				m_ActivatorButton = button;
				m_ActivatorModifiers = activator;
			}

			public bool GetCaps(ManipulatorCapability cap)
			{
				if (cap == ManipulatorCapability.eMultiSelection)
					return true;

				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.MouseDrag += MouseDrag;
				element.MouseUp += EndDrag;
				element.MouseDown += StartDrag;
			}

			private bool StartDrag(CanvasElement element, Event e, Canvas2D canvas)
			{
				if (e.type == EventType.Used)
					return false;

				if (e.button != m_ActivatorButton || m_ActivatorModifiers != e.modifiers)
				{
					return false;
				}

				canvas.StartCapture(this, element);

				e.Use();
				return true;
			}

			private bool EndDrag(CanvasElement element, Event e, Canvas2D canvas)
			{
				if (e.type == EventType.Used)
					return false;

				if (!canvas.IsCaptured(this))
				{
					return false;
				}

				canvas.EndCapture();

				if (canvas.Selection.Count == 0)
				{
					canvas.AddToSelection(element);
				}

				element.UpdateModel(UpdateType.eUpdate);
				e.Use();
				return true;
			}

			private bool MouseDrag(CanvasElement element, Event e, Canvas2D canvas)
			{
				if (e.type == EventType.Used)
					return false;

				if (!canvas.IsCaptured(this))
				{
					return false;
				}

				float scaleFactorX = element == canvas ? 1.0f : 1.0f/canvas.scale.x;
				float scaleFactorY = element == canvas ? 1.0f : 1.0f/canvas.scale.y;

				Vector3 tx = element.translation;
				tx.x += e.delta.x*scaleFactorX;
				tx.y += e.delta.y*scaleFactorY;
				element.translation = tx;
				element.UpdateModel(UpdateType.eCandidate);
				e.Use();

				return true;
			}
		};

		internal class Zoomable : IManipulate
		{
			public enum ZoomType
			{
				eAroundMouse = 0,
				eLastClick = 1
			};

			public Zoomable()
			{
				m_Type = ZoomType.eAroundMouse;
			}

			public Zoomable(ZoomType type)
			{
				m_Type = type;
			}

			private Vector2 m_ZoomLocation = Vector2.zero;
			private ZoomType m_Type = ZoomType.eAroundMouse;
			private float m_MinimumZoom = 0.08f;
			private float m_MaximumZoom = 1.0f;

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.ScrollWheel += OnZoom;
				element.KeyDown += OnKeyDown;

				if (m_Type == ZoomType.eLastClick)
				{
					element.MouseDown += OnMouseDown;
				}
			}

			private bool OnMouseDown(CanvasElement element, Event e, Canvas2D parent)
			{
				m_ZoomLocation = e.mousePosition;
				m_ZoomLocation.x -= element.translation.x;
				m_ZoomLocation.y -= element.translation.y;
				return true;
			}

			private bool OnKeyDown(CanvasElement element, Event e, Canvas2D canvas)
			{
				if (e.type == EventType.Used)
					return false;

				if (e.keyCode == KeyCode.R)
				{
					element.scale = Vector3.one;
					e.Use();
					return true;
				}
				return false;
			}

			private bool OnZoom(CanvasElement element, Event e, Canvas2D parent)
			{
				if (m_Type == ZoomType.eAroundMouse)
				{
					m_ZoomLocation = e.mousePosition;
					m_ZoomLocation.x -= element.translation.x;
					m_ZoomLocation.y -= element.translation.y;
				}

				float delta = 0;
				delta += Event.current.delta.y;
				delta += Event.current.delta.x;
				delta = -delta;

				Vector3 currentScale = element.scale;
				Vector3 currentTranslation = element.translation;

				// Scale multiplier. Don't allow scale of zero or below!
				float scale = Mathf.Max(0.01F, 1 + delta*0.01F);

				currentTranslation.x -= m_ZoomLocation.x*(scale - 1)*currentScale.x;
				currentScale.x *= scale;

				currentTranslation.y -= m_ZoomLocation.y*(scale - 1)*currentScale.y;
				currentScale.y *= scale;
				currentScale.z = 1.0f;

				bool outOfZoomBounds = false;
				if (((currentScale.x < m_MinimumZoom) || (currentScale.x > m_MaximumZoom)) ||
				    ((currentScale.y < m_MinimumZoom) || (currentScale.y > m_MaximumZoom)))
				{
					outOfZoomBounds = true;
				}

				currentScale.x = Mathf.Clamp(currentScale.x, m_MinimumZoom, m_MaximumZoom);
				currentScale.y = Mathf.Clamp(currentScale.y, m_MinimumZoom, m_MaximumZoom);

				element.scale = currentScale;
				if (!outOfZoomBounds)
				{
					element.translation = currentTranslation;
				}

				e.Use();
				return true;
			}
		};

		internal class Resizable : IManipulate
		{
			private bool m_Active = false;
			private Vector2 m_Start = new Vector2();

			public Resizable()
			{

			}

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.MouseDown += OnMouseDown;
				element.MouseDrag += OnMouseDrag;
				element.MouseUp += OnMouseUp;
				element.OnWidget += DrawResizeWidget;
			}

			private bool OnMouseDown(CanvasElement element, Event e, Canvas2D parent)
			{
				Rect r = element.boundingRect;
				Rect widget = r;
				widget.min = new Vector2(r.max.x - 30.0f, r.max.y - 30.0f);

				if (widget.Contains(parent.MouseToCanvas(e.mousePosition)))
				{
					parent.StartCapture(this, element);
					parent.ClearSelection();
					m_Active = true;
					m_Start = parent.MouseToCanvas(e.mousePosition);
					e.Use();
				}

				return true;
			}

			private bool OnMouseDrag(CanvasElement element, Event e, Canvas2D parent)
			{
				if (!m_Active || e.type != EventType.MouseDrag)
					return false;

				Vector2 newPosition = parent.MouseToCanvas(e.mousePosition);
				Vector2 diff = newPosition - m_Start;
				m_Start = newPosition;
				Vector3 newScale = element.scale;
				newScale.x = Mathf.Max(0.1f, newScale.x + diff.x);
				newScale.y = Mathf.Max(0.1f, newScale.y + diff.y);
				
				element.scale = newScale;
				
				element.DeepInvalidate();

				e.Use();
				return true;
			}

			private bool OnMouseUp(CanvasElement element, Event e, Canvas2D parent)
			{
				if (m_Active == true)
				{
					parent.EndCapture();
					parent.RebuildQuadTree();
				}
				m_Active = false;

				return true;
			}

			private bool DrawResizeWidget(CanvasElement element, Event e, Canvas2D parent)
			{
				GUIStyle style = new GUIStyle("WindowBottomResize");

				Rect r = element.boundingRect;
				Rect widget = r;
				widget.min = new Vector2(r.max.x - 10.0f, r.max.y - 7.0f);
				GUI.Label(widget, GUIContent.none, style);
				return true;
			}
		};

		internal class RectangleSelect : IManipulate
		{
			private Vector2 m_Start = Vector2.zero;
			private Vector2 m_End = Vector2.zero;
			private bool m_SelectionActive = false;

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.MouseDown += MouseDown;
				element.MouseUp += MouseUp;
				element.MouseDrag += MouseDrag;
			}

			private bool MouseDown(CanvasElement element, Event e, Canvas2D parent)
			{
				if (e.type == EventType.Used)
					return false;

				parent.ClearSelection();
				if (e.button == 0)
				{
					element.OnWidget += DrawSelection;
					m_Start = parent.MouseToCanvas(e.mousePosition);
					m_End = m_Start;
					m_SelectionActive = true;
					e.Use();
					return true;
				}
				return false;
			}

			private bool MouseUp(CanvasElement element, Event e, Canvas2D parent)
			{
				if (e.type == EventType.Used)
					return false;

				bool handled = false;

				if (m_SelectionActive)
				{
					element.OnWidget -= DrawSelection;
					m_End = parent.MouseToCanvas(e.mousePosition);

					Rect selection = new Rect();
					selection.min = new Vector2(Math.Min(m_Start.x, m_End.x), Math.Min(m_Start.y, m_End.y));
					selection.max = new Vector2(Math.Max(m_Start.x, m_End.x), Math.Max(m_Start.y, m_End.y));

					selection.width = Mathf.Max(selection.width, 5.0f);
					selection.height = Mathf.Max(selection.height, 5.0f);

					foreach (CanvasElement child in parent.Elements)
					{
						if (child.Intersects(selection))
						{
							parent.AddToSelection(child);
						}
					}

					handled = true;
					e.Use();
				}
				m_SelectionActive = false;

				return handled;
			}

			private bool MouseDrag(CanvasElement element, Event e, Canvas2D parent)
			{
				if (e.button == 0)
				{
					m_End = parent.MouseToCanvas(e.mousePosition);
					e.Use();
					return true;
				}

				return false;
			}

			private bool DrawSelection(CanvasElement element, Event e, Canvas2D parent)
			{
				if (!m_SelectionActive)
					return false;

				Rect r = new Rect();
				r.min = new Vector2(Math.Min(m_Start.x, m_End.x), Math.Min(m_Start.y, m_End.y));
				r.max = new Vector2(Math.Max(m_Start.x, m_End.x), Math.Max(m_Start.y, m_End.y));

				Color lineColor = new Color(1.0f, 0.6f, 0.0f, 1.0f);
				float segmentSize = 5f;

				Vector3[] points =
				{
					new Vector3(r.xMin, r.yMin, 0.0f),
					new Vector3(r.xMax, r.yMin, 0.0f),
					new Vector3(r.xMax, r.yMax, 0.0f),
					new Vector3(r.xMin, r.yMax, 0.0f)
				};

				DrawDottedLine(points[0], points[1], segmentSize, lineColor);
				DrawDottedLine(points[1], points[2], segmentSize, lineColor);
				DrawDottedLine(points[2], points[3], segmentSize, lineColor);
				DrawDottedLine(points[3], points[0], segmentSize, lineColor);

				return true;
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

		internal class Frame : IManipulate
		{
			public enum FrameType
			{
				eAll = 0,
				eSelection = 1
			};

			private FrameType m_Type = FrameType.eAll;

			public Frame(FrameType type)
			{
				m_Type = type;
			}

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.KeyDown += KeyDown;
			}

			private bool KeyDown(CanvasElement element, Event e, Canvas2D parent)
			{
				if (e.type == EventType.Used)
					return false;

				if ((m_Type == FrameType.eAll && e.keyCode == KeyCode.A) ||
				    (m_Type == FrameType.eSelection && e.keyCode == KeyCode.F))
				{
					Rect rectToFit = parent.CanvasRect;
					if (m_Type == FrameType.eSelection)
					{
						List<CanvasElement> s = parent.Selection;
						if (s.Count == 0)
							return false;
						rectToFit = s[0].boundingRect;
						foreach (CanvasElement c in s)
						{
							rectToFit = RectUtils.Encompass(rectToFit, c.boundingRect);
						}
					}

					// bring slightly smaller screen rect into GUI space
					Rect screenRect = new Rect();
					screenRect.xMin = 50;
					screenRect.xMax = Screen.width - 50;
					screenRect.yMin = 50;
					screenRect.yMax = Screen.height - 50;

					Matrix4x4 m = GUI.matrix;
					GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
					Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

					// measure zoom level necessary to fit the canvas rect into the screen rect
					float zoomLevel = Math.Min(identity.width/rectToFit.width, identity.height/rectToFit.height);

					// clamp
					zoomLevel = Mathf.Clamp(zoomLevel, 0.08f, 1.0f);

					parent.scale = new Vector3(zoomLevel, zoomLevel, 1.0f);
					parent.translation = Vector3.zero;


					// make a rect of the screen in GUI space and measure the distance between that rect
					// and the canvas rect. Multiply this by the scale level to get the offset to center the view
					Vector2 edge = parent.MouseToCanvas(new Vector2(Screen.width, Screen.height));
					Vector2 origin = parent.MouseToCanvas(new Vector2(0.0f, 0.0f));

					Rect r = new Rect();
					r.min = origin;
					r.max = edge;

					Vector2 offset = (r.center - rectToFit.center)*parent.scale.x;
					parent.translation = new Vector3(offset.x, offset.y, 0.0f);

					GUI.matrix = m;

					e.Use();

					return true;
				}

				return false;
			}
		};

		internal class ContextualMenu : IManipulate
		{
			private ManipulateDelegate m_Callback = null;
			private Object m_CustomData = null;

			public ContextualMenu(ManipulateDelegate callback)
			{
				m_Callback = callback;
				m_CustomData = null;
			}

			public ContextualMenu(ManipulateDelegate callback, Object customData)
			{
				m_Callback = callback;
				m_CustomData = customData;
			}

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.ContextClick += OnContextMenu;
			}

			private bool OnContextMenu(CanvasElement element, Event e, Canvas2D parent)
			{
				if (e.type == EventType.Used)
					return false;

				e.Use();
				return m_Callback(e, parent, m_CustomData);
			}
		};

		internal class DragDrop : IManipulate
		{
			private ManipulateDelegate m_Callback = null;
			private Object m_CustomData = null;

			public DragDrop(ManipulateDelegate callback)
			{
				m_Callback = callback;
				m_CustomData = null;
			}

			public DragDrop(ManipulateDelegate callback, Object customData)
			{
				m_Callback = callback;
				m_CustomData = customData;
			}

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.DragPerform += OnDragAndDropEvent;
				element.DragUpdated += OnDragAndDropEvent;
				element.DragExited += OnDragAndDropEvent;
			}

			private bool OnDragAndDropEvent(CanvasElement element, Event e, Canvas2D parent)
			{
				if (e.type == EventType.Used)
					return false;

				return m_Callback(e, parent, m_CustomData);
			}
		};

		internal class ScreenSpaceGrid : IManipulate
		{
			private float m_Spacing = 50f;
			private int m_ThickLines = 10;
			private Color m_LineColor = new Color(0f, 0f, 0f, 0.18f);
			private Color m_ThickLineColor = new Color(0f, 0f, 0f, 0.38f);
			private Color m_Background = new Color(0.17f, 0.17f, 0.17f, 1.0f);
			//private Color m_Background = new Color(1.0f, 1.0f, 1.0f, 1.0f);

			public ScreenSpaceGrid()
			{

			}

			public ScreenSpaceGrid(float spacing, int thickLineFrequency, Color lineColor, Color thickLineColor, Color background)
			{
				m_Spacing = spacing;
				m_ThickLines = thickLineFrequency;
				m_LineColor = lineColor;
				m_ThickLineColor = thickLineColor;
				m_Background = background;
			}

			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				if (element is Canvas2D)
				{
					(element as Canvas2D).OnBackground += DrawGrid;
				}
			}

			public static bool nearlyEqual(float a, float b, float epsilon)
			{
				if ((Math.Abs(a) - Math.Abs(b)) > epsilon)
					return false;
				return true;
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

			private bool DrawGrid(CanvasElement element, Event e, Canvas2D canvas)
			{
				Rect clientRect = new Rect(0, canvas.clientRect.y, Screen.width, Screen.height);

				// background
				UIHelpers.ApplyWireMaterial();
				
				GL.Begin(GL.QUADS);
				GL.Color(m_Background);
				GL.Vertex(Clip(clientRect, new Vector3(clientRect.x, clientRect.y + canvas.viewOffset.y, 0.0f)));
				GL.Vertex(Clip(clientRect, new Vector3(clientRect.x + clientRect.width, clientRect.y + canvas.viewOffset.y, 0.0f)));
				GL.Vertex(Clip(clientRect, new Vector3(clientRect.x + clientRect.width, clientRect.y + clientRect.height, 0.0f)));
				GL.Vertex(Clip(clientRect, new Vector3(clientRect.x, clientRect.y + clientRect.height, 0.0f)));
				GL.End();

				Vector3 from = new Vector3(0.0f, 0.0f, 0.0f);
				Vector3 to = new Vector3(0.0f, clientRect.height, 0.0f);

				Matrix4x4 tx = Matrix4x4.TRS(canvas.translation, Quaternion.identity, Vector3.one);

				// vertical lines
				from = tx.MultiplyPoint(from);
				to = tx.MultiplyPoint(to);

				float thickGridLineX = from.x;
				float thickGridLineY = from.y;

				from.x = (from.x%(m_Spacing*(canvas.scale.x)) - (m_Spacing*(canvas.scale.x)));
				to.x = from.x;

				from.y = 0.0f;
				to.y = clientRect.y + clientRect.height;

				while (from.x < clientRect.width)
				{
					from.x += m_Spacing*(canvas.scale.x);
					to.x += m_Spacing*(canvas.scale.x);
					GL.Begin(GL.LINES);
					GL.Color(m_LineColor);
					GL.Vertex(Clip(clientRect,from));
					GL.Vertex(Clip(clientRect, to));
					GL.End();
				}

				float thickLineSpacing = (m_Spacing*m_ThickLines);
				from.x = to.x = (thickGridLineX%(thickLineSpacing*(canvas.scale.x)) - (thickLineSpacing*(canvas.scale.x)));
				while (from.x < clientRect.width)
				{
					GL.Begin(GL.LINES);
					GL.Color(m_ThickLineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
					GL.End();
					from.x += (m_Spacing*(canvas.scale.x)*m_ThickLines);
					to.x += (m_Spacing*(canvas.scale.x)*m_ThickLines);
				}

				// horizontal lines
				from = new Vector3(0.0f, 0.0f, 0.0f);
				to = new Vector3(clientRect.width, 0.0f, 0.0f);

				from = tx.MultiplyPoint(from);
				to = tx.MultiplyPoint(to);

				from.y = (from.y%(m_Spacing*(canvas.scale.y)) - (m_Spacing*(canvas.scale.y)));
				to.y = from.y;
				from.x = 0.0f;
				to.x = clientRect.width;

				while (from.y < clientRect.height)
				{
					from.y += m_Spacing*(canvas.scale.y);
					to.y += m_Spacing*(canvas.scale.y);
					GL.Begin(GL.LINES);
					GL.Color(m_LineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
					GL.End();
				}

				thickLineSpacing = (m_Spacing*m_ThickLines);
				from.y = to.y = (thickGridLineY%(thickLineSpacing*(canvas.scale.y)) - (thickLineSpacing*(canvas.scale.y)));
				while (from.y < clientRect.height)
				{
					GL.Begin(GL.LINES);
					GL.Color(m_ThickLineColor);
					GL.Vertex(Clip(clientRect, from));
					GL.Vertex(Clip(clientRect, to));
					GL.End();
					from.y += (m_Spacing*(canvas.scale.y)*m_ThickLines);
					to.y += (m_Spacing*(canvas.scale.y)*m_ThickLines);
				}
				return true;
			}

		};

		internal class IMGUIContainer : IManipulate
		{
			public bool GetCaps(ManipulatorCapability cap)
			{
				return false;
			}

			public void AttachTo(CanvasElement element)
			{
				element.AllEvents += (target, evt, canvas) =>
				{
					Vector2 canvasPos = canvas.MouseToCanvas(evt.mousePosition);
					Rect rect = canvas.CanvasToScreen(element.boundingRect);
					GUI.BeginGroup(rect);
					element.Render(canvas.boundingRect, canvas);
					GUI.EndGroup();

					canvas.Repaint();
					element.Invalidate();

					return false;
				};
			}
		}
	}
}
