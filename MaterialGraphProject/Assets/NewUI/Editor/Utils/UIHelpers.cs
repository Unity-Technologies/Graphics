using System;
using System.Reflection;
using UnityEditor;

namespace RMGUI.GraphView
{
	internal class UIHelpers
	{
		static MethodInfo s_ApplyWireMaterialMi;

		public static void ApplyWireMaterial()
		{
			if (s_ApplyWireMaterialMi == null)
			{
				s_ApplyWireMaterialMi = typeof(HandleUtility).GetMethod(
                    "ApplyWireMaterial", 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                    null,
                    new Type[0],
                    null);
			}

			if (s_ApplyWireMaterialMi != null)
			{
				s_ApplyWireMaterialMi.Invoke(null, null);
			}
		}
	}
}
