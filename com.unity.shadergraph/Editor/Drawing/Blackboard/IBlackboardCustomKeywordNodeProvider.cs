using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Drawing
{
    [AttributeUsage(AttributeTargets.Method)]
    class CustomKeywordNodeProviderAttribute : Attribute
    {
        public int order { get; private set; }

        public CustomKeywordNodeProviderAttribute(int order = 0) => this.order = order;
    }

    static class BlackboardCustomKeywordNode
    {
        public static IEnumerable<ShaderKeyword> GetAllShaderKeyword()
            => TypeCache.GetMethodsWithAttribute<CustomKeywordNodeProviderAttribute>()
                .Where(method => method.IsStatic && method.ReturnType == typeof(IEnumerable<ShaderKeyword>))
                .OrderBy(method => method.GetCustomAttributes(typeof(CustomKeywordNodeProviderAttribute), false)
                    .Cast<CustomKeywordNodeProviderAttribute>().First().order)
                .SelectMany(method =>
                    (IEnumerable<ShaderKeyword>) method.Invoke(null, new object[0] { })
                );
    }
}
