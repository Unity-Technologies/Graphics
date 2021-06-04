using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Rendering.Utilities
{
    /// <summary>
    /// Set of utility functions for types
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Obtains a method with a set of types in it's declaration, useful when having same method name and multiple overrides
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="methodName">The method name</param>
        /// <param name="bindingAttr">The <see cref="BindingFlags"/> that define the method</param>
        /// <param name="types">The parameters of the method to find</param>
        /// <returns></returns>
        public static MethodInfo GetMethod(this Type type, string methodName, BindingFlags bindingAttr, params Type[] types)
        {
            if (types?.Any() ?? false)
            {
                return type
                    .GetMethods(bindingAttr)
                    .FirstOrDefault(
                        m =>
                        {
                            if (!m.Name.Equals(methodName)) return false;

                            var parameters = m.GetParameters();
                            if (parameters.Length != types.Length)
                                return false;

                            for (int i = 0; i < types.Length; ++i)
                            {
                                if (parameters[i].ParameterType != types[i])
                                    return false;
                            }

                            return true;
                        });
            }

            return type.GetMethod(methodName, bindingAttr);
        }
    }
}
