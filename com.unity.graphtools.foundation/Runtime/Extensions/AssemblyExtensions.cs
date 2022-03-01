using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    [Obsolete("0.10+ This class will be removed from GTF public API")]
    public static class AssemblyExtensions
    {
        [Obsolete("0.10+ his method will be removed from GTF public API")]
        public static IEnumerable<Type> GetTypesSafe(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Debug.LogWarning("Can't load assembly '" + assembly.GetName() + "'. Problematic types follow.");
                foreach (TypeLoadException tle in e.LoaderExceptions.Cast<TypeLoadException>())
                {
                    Debug.LogWarning("Can't load type '" + tle.TypeName + "': " + tle.Message);
                }

                return new Type[0];
            }
        }
    }
}
