using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // This is a transient Provider used to exercise the full 'Provider -> Producer -> Writer'
    // ProviderSystem loop by using it in CodeFunctionNode for its code generation.
    sealed class CodeFunctionProvider : IProvider<IShaderFunction>
    {
        readonly MethodInfo m_method;
        readonly string m_body;

        public string ProviderKey => $"{m_method.DeclaringType.FullName}.{m_method.Name}";
        public IShaderFunction Definition { get; private set; }

        internal CodeFunctionProvider(MethodInfo method, string body)
        {
            m_method = method ?? throw new ArgumentNullException(nameof(method));
            m_body = body;
            Reload();
        }

        public void Reload()
        {
            List<(int order, IShaderField field)> parameters = new();
            foreach (var param in m_method.GetParameters())
            {
                var slotId = CodeFunctionNode.GetSlotId(param)
                    ?? throw new InvalidOperationException($"'{param.Name}' of '{m_method.Name}' has no slot attribute.");
                parameters.Add((slotId, new ShaderField(param.Name, !param.IsOut, param.IsOut, new ShaderType(TypeName(param)), null)));
            }
            parameters.Sort((a, b) => a.order.CompareTo(b.order));

            List<IShaderField> fields = new();
            foreach (var (_, field) in parameters)
                fields.Add(field);

            Definition = new ShaderFunction(m_method.Name, null, fields, new ShaderType(ShaderObjectUtils.kVoidTypeName), StripBraces(m_body), null);
        }

        // Coerce the parameter's type into HLSL.
        static string TypeName(ParameterInfo param)
        {
            var concreteType = CodeFunctionNode.ConvertTypeToSlotValueType(param) switch
            {
                SlotValueType.Dynamic or SlotValueType.DynamicVector => ConcreteSlotValueType.Vector4,
                SlotValueType.DynamicMatrix => ConcreteSlotValueType.Matrix4,
                var slotValueType => slotValueType.ToConcreteSlotValueType(),
            };
            return concreteType.ToShaderString(PrecisionUtil.Token);
        }

        // CodeFunctionNode includes braces, but the Writer will do that later.
        static string StripBraces(string body)
        {
            body = (body ?? string.Empty).Trim('\r', '\n', '\t', ' ');
            if (body.EndsWith("};"))
                body = body.TrimEnd(';').TrimEnd();
            if (body.StartsWith("{") && body.EndsWith("}"))
                body = body.Substring(1, body.Length - 2).Trim('\r', '\n');
            return body;
        }

        public IProvider Clone() => new CodeFunctionProvider(m_method, m_body);
    }
}
