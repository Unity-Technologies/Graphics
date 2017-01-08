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
				if (!t.IsSubclassOf(typeof(GraphElementPresenter)))
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

		public GraphElement Create(GraphElementPresenter presenter)
		{
			Type viewType = null;
			Type dataType = presenter.GetType();

			while (viewType == null && dataType != typeof(GraphElementPresenter))
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
			dataContainer.presenter = presenter;
			return dataContainer;
		}
	}
}
