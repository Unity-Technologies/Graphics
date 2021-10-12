using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UnityEngine.Rendering
{
    class SupportedOnGenerator : ISourceGenerator
    {
        class SupportedOnVisitor : SymbolVisitor
        {
            public Dictionary<ITypeSymbol, List<ITypeSymbol>> SupportedOn = new Dictionary<ITypeSymbol, List<ITypeSymbol>>();

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.GetAttributes().Any(att => att.AttributeClass?.Name == "SupportedOnAttribute"))
                {
                    var targets = symbol
                        .GetAttributes()
                        .Where(att => att.AttributeClass?.Name == "SupportedOnAttribute"
                            && att.ConstructorArguments.Length == 1
                            && att.ConstructorArguments[0].Type?.Name == "Type")
                        .Select(att => att.ConstructorArguments[0].Value as ITypeSymbol)
                        .ToList();
                    if (targets.Count > 0)
                        SupportedOn.Add(symbol, targets);
                }

                foreach (var childType in symbol.GetTypeMembers())
                    childType.Accept(this);
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                foreach (var module in symbol.Modules)
                    module.Accept(this);
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
                symbol.GlobalNamespace.Accept(this);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var childSymbol in symbol.GetMembers())
                    childSymbol.Accept(this);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var visitor = new SupportedOnVisitor();
            visitor.Visit(context.Compilation.Assembly);

            var initializationBody = visitor.SupportedOn
                .SelectMany(subject => subject.Value.Select(target => (subject.Key, target)))
                .Aggregate(new StringBuilder(), (sb, pair) =>
                {
                    sb.AppendLine($"UnityEngine.Rendering.IsSupportedOn.RegisterStaticRelation(typeof({pair.Key}), typeof({pair.target}));");
                    sb.AppendLine($"UnityEngine.Rendering.IsSupportedOn.RegisterDynamicRelation(typeof({pair.Key}), typeof({pair.target}));");
                    return sb;
                });
            if (initializationBody.Length > 0)
            {
                var source = $@"
namespace {context.Compilation.Assembly.Identity.Name}
{{
    static class RegisterSupportedOn
    {{
        static bool s_Initialized = false;

        [UnityEngine.RuntimeInitializeOnLoadMethod]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Initialize()
        {{
            if (s_Initialized)
                return;
            {initializationBody}
            s_Initialized = true;
        }}
    }}
}}
";
                context.AddSource("SupportedOnRegistration.cs", source);
            }
        }
    }
}
