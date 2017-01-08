using System.Linq;
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
				s_ApplyWireMaterialMi = typeof(HandleUtility)
					.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
					.First(m => m.Name == "ApplyWireMaterial" && m.GetParameters().Length == 0);
			}

			if (s_ApplyWireMaterialMi != null)
			{
				s_ApplyWireMaterialMi.Invoke(null, null);
			}
		}
	}
}
