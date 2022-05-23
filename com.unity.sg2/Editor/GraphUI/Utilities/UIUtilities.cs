using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class UIUtilities
    {
        public static IEnumerable<Type> GetTypesOrNothing(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }
    }
}
