using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RMGUI.GraphView
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public class CustomDataView : Attribute
	{
		public CustomDataView(Type t)
		{
			if (t == null)
				Debug.LogError("Failed to load CustomDataView inspected type");
			dataType = t;
		}

		private Type dataType { get; set; }

		// map of [datType, viewType]
		private static Dictionary<Type, Type> s_TypeMap;

		public static GraphElement Create(GraphElementData data)
		{
			if (s_TypeMap == null)
			{
				s_TypeMap = new Dictionary<Type, Type>();

				// add extension methods
				AppDomain currentDomain = AppDomain.CurrentDomain;
				foreach (Assembly assembly in currentDomain.GetAssemblies())
				{
					foreach (Type type in assembly.GetTypes())
					{
						var attributes = type.GetCustomAttributes(typeof(CustomDataView), false);
						foreach (CustomDataView att in attributes)
						{
							s_TypeMap[type] = att.dataType;
						}
					}
				}
			}

			Type viewType;
			if (s_TypeMap.TryGetValue(data.GetType(), out viewType))
			{
				var dataContainer = (GraphElement)Activator.CreateInstance(viewType);
				dataContainer.dataProvider = data;
				return dataContainer;
			}
			throw new InvalidOperationException("No view in assembly for this data type" + data.GetType());
		}
	}
}
