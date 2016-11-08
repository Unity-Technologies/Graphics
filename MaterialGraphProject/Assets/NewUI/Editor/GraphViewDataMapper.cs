using System;
using System.Collections.Generic;

namespace RMGUI.GraphView
{
	public class GraphViewDataMapper
	{
		private readonly Dictionary<Type, Type> m_DataToViewDict = new Dictionary<Type, Type>();

		public Type this[Type t]
		{
			get
			{
				return m_DataToViewDict[t];
			}
			set
			{
				if (!t.IsSubclassOf(typeof(GraphElementData)))
				{
					throw new ArgumentException("The type passed as key does not derive from UnityEngine.Object.");
				}

				if (!value.IsSubclassOf(typeof(GraphElement)))
				{
					throw new ArgumentException("The type passed as value does not derive from DataContainer.");
				}

				m_DataToViewDict[t] = value;
			}
		}

		public GraphElement Create(GraphElementData data)
		{
			Type viewType = null;
			Type dataType = data.GetType();

			while (viewType == null && dataType != typeof(GraphElementData))
			{
				if (!m_DataToViewDict.TryGetValue(dataType, out viewType))
				{
					dataType = dataType.BaseType;
				}
			}

			if (viewType == null)
			{
				viewType = typeof(FallbackGraphElement);
			}

			var dataContainer = (GraphElement)Activator.CreateInstance(viewType);
			dataContainer.dataProvider = data;
			return dataContainer;
		}
	}
}
