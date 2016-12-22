using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class ContentZoomer : Manipulator
	{
		public static readonly Vector3 DefaultMinScale = new Vector3(0.0f, 0.0f, 1.0f);
		public static readonly Vector3 DefaultMaxScale = new Vector3(3.0f, 3.0f, 1.0f);

		public float zoomStep { get; set; }

		public Vector3 minScale { get; set; }
		public Vector3 maxScale { get; set; }

		public ContentZoomer()
		{
			zoomStep = 0.01f;
			minScale = DefaultMinScale;
			maxScale = DefaultMaxScale;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			var graphView = target as GraphView;
			if (graphView == null)
			{
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}

			switch (evt.type)
			{
				case EventType.ScrollWheel:
				{
					Matrix4x4 transform = graphView.contentViewContainer.transform;

					// TODO: augment the data to have the position as well, so we don't have to read in data from the target.
					// 0-1 ranged center relative to size
					Vector2 zoomCenter = target.ChangeCoordinatesTo(graphView.contentViewContainer, evt.mousePosition);
					float x = zoomCenter.x + graphView.contentViewContainer.position.x;
					float y = zoomCenter.y + graphView.contentViewContainer.position.y;

					transform *= Matrix4x4.Translate(new Vector3(x, y, 0));
					Vector3 s = Vector3.one - Vector3.one*evt.delta.y*zoomStep;
					s.z = 1;
					transform *= Matrix4x4.TRS(Vector3.zero, Quaternion.identity, s);

					// Limit scale
					transform.m00 = Mathf.Max(Mathf.Min(maxScale.x, transform.m00), minScale.x);
					transform.m11 = Mathf.Max(Mathf.Min(maxScale.y, transform.m11), minScale.y);
					transform.m22 = Mathf.Max(Mathf.Min(maxScale.z, transform.m22), minScale.z);

					transform *= Matrix4x4.Translate(new Vector3(-x, -y, 0));

					graphView.contentViewContainer.transform = transform;

					return EventPropagation.Stop;
				}
			}
			return EventPropagation.Continue;
		}
	}
}
