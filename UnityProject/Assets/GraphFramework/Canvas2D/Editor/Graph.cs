using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditorInternal; 
using Object = UnityEngine.Object;

//#pragma warning disable 0414
//#pragma warning disable 0219 

namespace UnityEditor
{
	namespace Experimental
	{
		namespace Graph
		{
			class TypeAdapter : Attribute
			{
				
			}

			public enum Direction
			{
				eInput = 0,
				eOutput = 1,
				eBidirectional = 2
			};

			public interface IConnect
			{
				Direction GetDirection();
				void Highlight(bool highlighted);
				void RenderOverlay(Canvas2D canvas);
				object Source();
				Vector3 ConnectPosition();
				void OnConnect(IConnect other);
			};

			public class NodeAdapter
			{
				private static List<MethodInfo> m_TypeAdapters = null;
				private static Dictionary<int, System.Reflection.MethodInfo> m_NodeAdapterDictionary;

				public bool CanAdapt(object a, object b)
				{
					if (a == b)
						return false;   // self connections are not permitted

					if (a == null || b == null)
						return false;

					MethodInfo mi = GetAdapter(a, b);
					if (mi == null)
					{
						Debug.Log("adapter node not found for: " + a.GetType().ToString() + " -> " + b.GetType().ToString());
					}
					return mi != null ? true : false;
				}

				public bool Connect(object a, object b)
				{
					MethodInfo mi = GetAdapter(a, b);
					if (mi == null)
					{
						Debug.LogError("Attempt to connect 2 unadaptable types: " + a.GetType().ToString() + " -> " + b.GetType().ToString());
						return false;
					}
					object retVal = mi.Invoke(this, new object[] { this, a, b });
					return (bool)retVal;
				}

				IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType)
				{
					var query = from type in assembly.GetTypes()
								where type.IsSealed && !type.IsGenericType && !type.IsNested
								from method in type.GetMethods(BindingFlags.Static
									| BindingFlags.Public | BindingFlags.NonPublic)
								where method.IsDefined(typeof(ExtensionAttribute), false)
								where method.GetParameters()[0].ParameterType == extendedType
								select method;
					return query;
				}

				public MethodInfo GetAdapter(object a, object b)
				{
					if (a == null || b == null)
						return null;

					if (m_NodeAdapterDictionary == null)
					{
						m_NodeAdapterDictionary = new Dictionary<int, System.Reflection.MethodInfo>();

						// add extension methods
						AppDomain currentDomain = AppDomain.CurrentDomain;
						foreach (System.Reflection.Assembly assembly in currentDomain.GetAssemblies())
						{
							foreach (MethodInfo method in GetExtensionMethods(assembly, typeof(NodeAdapter)))
							{
								System.Reflection.ParameterInfo[] methodParams = method.GetParameters();
								if (methodParams.Count() == 3)
								{
									string pa = methodParams[1].ParameterType.ToString() + methodParams[2].ParameterType.ToString();
									m_NodeAdapterDictionary.Add(pa.GetHashCode(), method);
								}

							}
						}
					}

					string s = a.GetType().ToString() + b.GetType().ToString();

					try
					{
						return m_NodeAdapterDictionary[s.GetHashCode()];
					}
					catch (Exception)
					{
					}

					return null;
				}

				public MethodInfo GetTypeAdapter(Type from, Type to)
				{
					if (m_TypeAdapters == null)
					{
						m_TypeAdapters = new List<MethodInfo>();
						AppDomain currentDomain = AppDomain.CurrentDomain;
						foreach (System.Reflection.Assembly assembly in currentDomain.GetAssemblies())
						{
							try
							{
								foreach (Type temptype in assembly.GetTypes())
								{
									MethodInfo[] methodInfos = temptype.GetMethods(BindingFlags.Public | BindingFlags.Static);
									foreach (MethodInfo i in methodInfos)
									{
										object[] allAttrs = i.GetCustomAttributes(typeof(TypeAdapter), false);
										if (allAttrs.Count() > 0)
										{
											m_TypeAdapters.Add(i);
										}
									}
								}
							}
							catch (Exception ex)
							{
								Debug.Log(ex);
							}
						}
					}

					foreach (MethodInfo i in m_TypeAdapters)
					{
						if (i.ReturnType == to)
						{
							ParameterInfo[] allParams = i.GetParameters();
							if (allParams.Count() == 1)
							{
								if (allParams[0].ParameterType == from)
									return i;
							}
						}
					}
					return null;
				}
			};

			internal class EdgeConnector<T> : IManipulate where T : IConnect
			{
				private static Color s_EdgeColor = new Color(1.0f, 1.0f, 1.0f, 0.8f);
				private static Color s_ActiveEdgeColor = new Color(0.2f, 0.4f, 1.0f, 0.8f);

				private Vector2 m_Start = Vector2.zero;
				private Vector2 m_End = Vector2.zero;
				private Color m_Color = s_EdgeColor;
				private IConnect m_SnappedTarget = null;
				private IConnect m_SnappedSource = null;

				List<IConnect> m_CompatibleAnchors = new List<IConnect>();

				public EdgeConnector()
				{
				}

				public bool GetCaps(ManipulatorCapability cap)
				{
					return false;
				}

				public void AttachTo(CanvasElement element)
				{
					element.MouseUp += EndDrag;
					element.MouseDown += StartDrag;
					element.MouseDrag += MouseDrag;
				}

				private bool StartDrag(CanvasElement element, Event e, Canvas2D canvas)
				{
					if (e.type == EventType.Used)
					{
						return false;
					}

					if (e.button != 0)
					{
						return false;
					}

					element.OnWidget += DrawEdge;

					IConnect cnx = element as IConnect;

					if (element.collapsed)
						return false;

					canvas.StartCapture(this, element);
					m_Start = m_End = element.canvasBoundingRect.center;

					e.Use();
					

					if (cnx != null)
					{
						cnx.Highlight(true);
					}
					EndSnap();

					// find compatible anchors
					m_CompatibleAnchors.Clear();

					Rect screenRect = new Rect();
					screenRect.min = canvas.MouseToCanvas(new Vector2(0.0f, 0.0f));
					screenRect.max = canvas.MouseToCanvas(new Vector2(Screen.width, Screen.height));

					CanvasElement[] visibleAnchors = canvas.Pick<T>(screenRect);
					NodeAdapter nodeAdapter = new NodeAdapter();
					foreach (CanvasElement anchor in visibleAnchors)
					{
						IConnect toCnx = anchor as IConnect;
						if (toCnx == null)
							continue;

						bool isBidirectional = ((cnx.GetDirection() == Direction.eBidirectional) ||
						                        (toCnx.GetDirection() == Direction.eBidirectional));

						if (cnx.GetDirection() != toCnx.GetDirection() || isBidirectional)
						{
							if (nodeAdapter.GetAdapter(cnx.Source(), toCnx.Source()) != null)
							{
								m_CompatibleAnchors.Add(toCnx);
							}
						}
					}

					canvas.OnOverlay += HighlightCompatibleAnchors;

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

					element.OnWidget -= DrawEdge;

					canvas.EndCapture();
					IConnect cnx = element as IConnect;
					if (cnx != null)
					{
						cnx.Highlight(false);
					}
					
					if (m_SnappedSource == null && m_SnappedTarget == null)
					{
						cnx.OnConnect(null);
					}
					else if (m_SnappedSource != null && m_SnappedTarget != null)
					{
						NodeAdapter nodeAdapter = new NodeAdapter();
						if (nodeAdapter.CanAdapt(m_SnappedSource.Source(), m_SnappedTarget.Source()))
						{
							nodeAdapter.Connect(m_SnappedSource.Source(), m_SnappedTarget.Source());
							cnx.OnConnect(m_SnappedTarget);
						}
					}

					EndSnap();
					e.Use();
					canvas.OnOverlay -= HighlightCompatibleAnchors;
					return true;
				}

				private bool MouseDrag(CanvasElement element, Event e, Canvas2D canvas)
				{
					if (e.type == EventType.Used)
					{
						return false;
					}

					if (!canvas.IsCaptured(this))
					{
						return false;
					}

					m_End = canvas.MouseToCanvas(e.mousePosition);
					e.Use();

					m_Color = s_EdgeColor;

					IConnect thisCnx = (element as IConnect);
					// find target anchor under us
					CanvasElement elementUnderMouse = canvas.PickSingle<T>(e.mousePosition);
					if (elementUnderMouse != null)
					{
						IConnect cnx = elementUnderMouse as IConnect;
						if (cnx == null)
						{
							Debug.LogError("PickSingle returned an incompatible element: does not support IConnect interface");
							return true;
						}

						if (m_CompatibleAnchors.Exists(ic => ic == cnx))
						{
							StartSnap(thisCnx, cnx);
							m_Color = s_ActiveEdgeColor;
						}
					}
					else
					{
						EndSnap();
					}

					return true;
				}

				private void StartSnap(IConnect from, IConnect to)
				{
					EndSnap();
					m_SnappedTarget = to;
					m_SnappedSource = from;
					m_SnappedTarget.Highlight(true);
				}

				private void EndSnap()
				{
					if (m_SnappedTarget != null)
					{
						m_SnappedTarget.Highlight(false);
						m_SnappedTarget = null;
					}
				}

				private bool DrawEdge(CanvasElement element, Event e, Canvas2D canvas)
				{
					if (!canvas.IsCaptured(this))
					{
						return false;
					}

					bool invert = false;
					if (m_End.x < m_Start.x)
						invert = true;
					Vector3[] points, tangents;
					GetTangents(invert ? m_End : m_Start, invert ? m_Start : m_End, out points, out tangents);
					Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], m_Color, null, 5f);

					// little widget on the middle of the edge
					Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);
					Color oldColor = Handles.color;
					Handles.color = m_Color;
					Handles.DrawSolidDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 6f);
					Handles.color = oldColor;
					return true;
				}

				private bool HighlightCompatibleAnchors(CanvasElement element, Event e, Canvas2D canvas)
				{
					foreach (IConnect visible in m_CompatibleAnchors)
					{
						visible.RenderOverlay(canvas);
					}
					return false;
				}

				public static void GetTangents(Vector2 start, Vector2 end, out Vector3[] points, out Vector3[] tangents)
				{
					points = new Vector3[] { start, end };
					tangents = new Vector3[2];

					const float minTangent = 30;

					float weight = (start.y < end.y) ? .3f : .7f;
					weight = .5f;
					float weight2 = 1 - weight;
					float y = 0;

					if (start.x > end.x)
					{
						weight2 = weight = -.25f;
						float aspect = (start.x - end.x) / (start.y - end.y);
						if (Mathf.Abs(aspect) > .5f)
						{
							float asp = (Mathf.Abs(aspect) - .5f) / 8;
							asp = Mathf.Sqrt(asp);
							y = Mathf.Min(asp * 80, 80);
							if (start.y > end.y)
								y = -y;
						}
					}
					float cleverness = Mathf.Clamp01(((start - end).magnitude - 10) / 50);

					tangents[0] = start + new Vector2((end.x - start.x) * weight + minTangent, y) * cleverness;
					tangents[1] = end + new Vector2((end.x - start.x) * -weight2 - minTangent, -y) * cleverness;
				}
			};


			internal class Edge<T> : CanvasElement where T : CanvasElement, IConnect
			{
				private T m_Left = null;
				private T m_Right = null;
				private ICanvasDataSource m_Data;
				public Edge(ICanvasDataSource data, T left, T right)
				{
					m_Data = data;
					zIndex = 9999;
					m_SupportsRenderToTexture = false;
					left.AddDependency(this);
					right.AddDependency(this);
					m_Left = left;
					m_Right = right;

					UpdateModel(UpdateType.eUpdate);

					KeyDown += OnDeleteEdge;
				}

			    public T Left
			    {
			        get { return m_Left; }
			    }

			    public T Right
			    {
			        get { return m_Right; }
			    }

			    private bool OnDeleteEdge(CanvasElement element, Event e, Canvas2D canvas)
				{
					if (e.type == EventType.Used)
						return false;

					if (e.keyCode == KeyCode.Delete)
					{
						m_Data.DeleteElement(this);
						return true;
					}
					return false;
				}

				public override bool Intersects(Rect rect)
				{
					// first check coarse bounding box
					if (!base.Intersects(rect))
						return false;

					// bounding box check succeeded, do more fine grained check by checking intersection between the rectangles' diagonal
					// and the line segments

					Vector3 from = m_Left.ConnectPosition();
					Vector3 to = m_Right.ConnectPosition();

					if (to.x < from.x)
					{
						Vector3 t = from;
						from = to;
						to = t;
					}

					Vector3[] points, tangents;
					EdgeConnector<T>.GetTangents(from, to, out points, out tangents);
					Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

					for (int a = 0; a < allPoints.Length; a++)
					{
						if (a >= allPoints.Length - 1)
						{
							break;
						}

						Vector2 segmentA = new Vector2(allPoints[a].x, allPoints[a].y);
						Vector2 segmentB = new Vector2(allPoints[a + 1].x, allPoints[a + 1].y);

						if (RectUtils.IntersectsSegment(rect, segmentA, segmentB))
							return true;
					}

					return false;
				}

				public override bool Contains(Vector2 canvasPosition)
				{
					// first check coarse bounding box
					if (!base.Contains(canvasPosition))
						return false;

					// bounding box check succeeded, do more fine grained check by measuring distance to bezier points

					Vector3 from = m_Left.ConnectPosition();
					Vector3 to = m_Right.ConnectPosition();

					if (to.x < from.x)
					{
						Vector3 t = from;
						from = to;
						to = t;
					}

					Vector3[] points, tangents;
					EdgeConnector<T>.GetTangents(from, to, out points, out tangents);
					Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

					float minDistance = Mathf.Infinity;
					foreach (Vector3 currentPoint in allPoints)
					{
						float distance = Vector3.Distance(currentPoint, canvasPosition);
						minDistance = Mathf.Min(minDistance, distance);
						if (minDistance < 15.0f)
						{
							return true;
						}
					}

					return false;
				}

				public override void Render(Rect parentRect, Canvas2D canvas)
				{
					Color edgeColor = selected ? Color.yellow : Color.white;

					Vector3 from = m_Left.ConnectPosition();
					Vector3 to = m_Right.ConnectPosition();

					if (to.x < from.x)
					{
						Vector3 t = from;
						from = to;
						to = t;
					}

					Vector3[] points, tangents;
					EdgeConnector<T>.GetTangents(from, to, out points, out tangents);
					Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 5f);
				}

				public override void UpdateModel(UpdateType t)
				{
					Vector3 from = m_Left.ConnectPosition();
					Vector3 to = m_Right.ConnectPosition();

					Rect r = new Rect();
					r.min = new Vector2(Math.Min(from.x, to.x), Math.Min(from.y, to.y));
					r.max = new Vector2(Math.Max(from.x, to.x), Math.Max(from.y, to.y));

					translation = r.min;
					scale = new Vector3(r.width, r.height, 1.0f);

					base.UpdateModel(t);
				}
			}
		}
	}
}
