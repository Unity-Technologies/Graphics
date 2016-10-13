using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public abstract class GraphElement : DataWatchContainer, ISelectable
	{
		GraphElementData m_DataProvider;

		public T GetData<T>() where T : GraphElementData
		{
			return dataProvider as T;
		}

		public GraphElementData dataProvider
		{
			get { return m_DataProvider; }
			set
			{
				if (m_DataProvider == value)
					return;
				RemoveWatch();
				m_DataProvider = value;
				OnDataChanged();
				AddWatch();
			}
		}

		protected override object toWatch
		{
			get { return dataProvider; }
		}

		public override void OnDataChanged()
		{
			var data = dataProvider;
			if (data == null)
			{
				return;
			}

			// propagate selection but why?
			foreach (VisualElement visualElement in children)
			{
				var graphElement = visualElement as GraphElement;
				if (graphElement != null)
				{
					GraphElementData childData = graphElement.dataProvider;
					if (childData != null)
					{
						childData.selected = data.selected;
					}
				}
			}

			SetPosition(data.position);
		}

		public virtual bool IsSelectable()
		{
			GraphElementData data = dataProvider;
			if (data != null)
			{
				return (data.capabilities & Capabilities.Selectable) == Capabilities.Selectable;
			}
			return false;
		}

		public virtual Vector3 GetGlobalCenter()
		{
			var center = position.center;
			var globalCenter = new Vector3(center.x + parent.position.x, center.y + parent.position.y);
			return parent.globalTransform.MultiplyPoint3x4(globalCenter);
		}

		public virtual void SetPosition(Rect newPos)
		{
			// set absolute position from data
			position = newPos;
		}
	}
}
