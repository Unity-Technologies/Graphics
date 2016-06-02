using System.Reflection;

namespace UnityEditor.Experimental
{
    internal class UIHelpers
    {
        static MethodInfo s_ApplyWireMaterialMi;

        public static void ApplyWireMaterial()
        {
            if (s_ApplyWireMaterialMi == null)
            {
                s_ApplyWireMaterialMi = typeof(HandleUtility).GetMethod("ApplyWireMaterial", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            }

            if (s_ApplyWireMaterialMi != null)
            {
                s_ApplyWireMaterialMi.Invoke(null, null);
            }
        }
    }
}
