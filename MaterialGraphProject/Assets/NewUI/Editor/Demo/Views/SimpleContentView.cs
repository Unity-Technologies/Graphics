using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[StyleSheet("Assets/Editor/Demo/Views/SimpleContentView.uss")]
	public class SimpleContentView : GraphView
	{
		public SimpleContentView()
		{
			AddManipulator(new ContentZoomer());
			AddManipulator(new ContentDragger());
			AddManipulator(new RectangleSelector());
			AddManipulator(new SelectionDragger());
			AddManipulator(new ClickSelector());
			AddManipulator(new ShortcutHandler(
				new Dictionary<Event, ShortcutDelegate>
				{
					{Event.KeyboardEvent("a"), FrameAll},
					{Event.KeyboardEvent("f"), FrameSelection},
					{Event.KeyboardEvent("o"), FrameOrigin}
				}));

			AddDecorator(new GridBackground());

			dataMapper[typeof(CircleData)] = typeof(Circle);
			dataMapper[typeof(InvisibleBorderContainerData)] = typeof(InvisibleBorderContainer);
			dataMapper[typeof(MiniMapData)] = typeof(MiniMap);
			dataMapper[typeof(SimpleElementData)] = typeof(SimpleElement);
			dataMapper[typeof(WWWImageData)] = typeof(WWWImage);
			dataMapper[typeof(IMGUIData)] = typeof(IMGUIElement);
		}

		bool m_FrameAnimate = false;

		public enum FrameType
		{
			All = 0,
			Selection = 1,
			Origin = 2
		}

		private Rect m_LastSelectionRect;

		public override void DoRepaint(PaintContext painter)
		{
			base.DoRepaint(painter);
			painter.DrawRectangleOutline(transform, m_LastSelectionRect, Color.red);
		}

		// TODO: Move elsewhere
		static Rect Encompass(Rect a, Rect b)
		{
			return new Rect
			{
				xMin = Math.Min(a.xMin, b.xMin),
				yMin = Math.Min(a.yMin, b.yMin),
				xMax = Math.Max(a.xMax, b.xMax),
				yMax = Math.Max(a.yMax, b.yMax)
			};
		}

		void CalculateFrameTransform(Rect rectToFit, out Vector3 frameTranslation, out Vector3 frameScaling)
		{
			// Give it full width/height
			Rect clientRect = position;

			// bring slightly smaller screen rect into GUI space
			var screenRect = new Rect
			{
				xMin = 30,
				xMax = clientRect.width - 30,
				yMin = 30,
				yMax = clientRect.height - 30
			};

			Matrix4x4 m = GUI.matrix;
			GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
			Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

			// measure zoom level necessary to fit the canvas rect into the screen rect
			float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

			// clamp
			zoomLevel = Mathf.Clamp(zoomLevel, 0.08f, 1.0f);

			var cachedScale = new Vector3(transform.GetColumn(0).magnitude,
										  transform.GetColumn(1).magnitude,
										  transform.GetColumn(2).magnitude);
			Vector4 cachedTranslation = transform.GetColumn(3);

			transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

			var edge = new Vector2(clientRect.width, clientRect.height);
			var origin = new Vector2(0, 0);

			var r = new Rect
			{
				min = origin,
				max = edge
			};

			var parentScale = new Vector3(transform.GetColumn(0).magnitude,
										  transform.GetColumn(1).magnitude,
										  transform.GetColumn(2).magnitude);
			Vector2 offset = r.center - (rectToFit.center * parentScale.x);

			// Update output values before leaving
			frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
			frameScaling = parentScale;

			transform = Matrix4x4.TRS(cachedTranslation, Quaternion.identity, cachedScale);

			GUI.matrix = m;
		}

		EventPropagation FrameAll()
		{
			return Frame(FrameType.All);
		}

		EventPropagation FrameSelection()
		{
			return Frame(FrameType.Selection);
		}

		EventPropagation FrameOrigin()
		{
			return Frame(FrameType.Origin);
		}

		EventPropagation Frame(FrameType frameType)
		{
			// Reset container translation, scale and position
			contentViewContainer.transform *= contentViewContainer.transform.inverse;
			Rect p = contentViewContainer.position;
			p.x = 0;
			p.y = 0;
			contentViewContainer.position = p;

			if (frameType == FrameType.Origin)
			{
				return EventPropagation.Stop;
			}

			Rect rectToFit = contentViewContainer.position;
			if (frameType == FrameType.Selection)
			{
				// Now calculate rectangle to fit all selected elements
				if (selection.Count == 0)
				{
					return EventPropagation.Continue;
				}

				var graphElement = selection[0] as GraphElement;
				if (graphElement != null)
				{
					rectToFit = graphElement.localBound;
				}

				rectToFit = selection.OfType<GraphElement>()
									 .Aggregate(rectToFit, (current, e) => Encompass(current, e.localBound));
			}
			else /*if (frameType == FrameType.All)*/
			{
				bool reachedFirstChild = false;
				foreach (VisualElement child in contentViewContainer.children)
				{
					var graphElement = child as GraphElement;
					if (graphElement == null ||
						(graphElement.dataProvider.capabilities & Capabilities.Floating) != 0 ||
						(graphElement.dataProvider is EdgeData))
					{
						continue;
					}

					if (!reachedFirstChild)
					{
						rectToFit = graphElement.localBound;
						reachedFirstChild = true;
					}
					else
					{
						rectToFit = Encompass(rectToFit, graphElement.localBound);
					}
				}
			}

			Vector3 frameTranslation;
			Vector3 frameScaling;

			m_LastSelectionRect = rectToFit;

			CalculateFrameTransform(rectToFit, out frameTranslation, out frameScaling);

			if (m_FrameAnimate)
			{
				// TODO
				// RMAnimation animation = new RMAnimation();
				// parent.Animate(parent)
				//       .Lerp(new string[] {"m_Scale", "m_Translation"},
				//             new object[] {parent.scale, parent.translation},
				//             new object[] {frameScaling, frameTranslation}, 0.08f);
			}
			else
			{
				Matrix4x4 t = Matrix4x4.identity;
				t *= Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
				contentViewContainer.transform = t;
			}

			contentViewContainer.Touch(ChangeType.Repaint);

			return EventPropagation.Stop;
		}
	}
}
