using System.Reflection;
using UnityEditor;
using System.Linq;

namespace RMGUI.GraphView
{
	internal class UIHelpers
	{
		static MethodInfo s_ApplyWireMaterialMi;

		public static void ApplyWireMaterial()
		{
			if (s_ApplyWireMaterialMi == null)
			{
				var methods = typeof(HandleUtility).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
				s_ApplyWireMaterialMi = methods.First(o => o.Name == "ApplyWireMaterial" && o.GetGenericArguments().Count() == 0);
			}

			if (s_ApplyWireMaterialMi != null)
			{
				s_ApplyWireMaterialMi.Invoke(null, null);
			}
		}
	}
}
