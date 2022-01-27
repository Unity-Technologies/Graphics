using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.UIGen
{
    public static class DebugMenuUIGenerator
    {
        const string Identifier = "DebugMenu";

        public struct DebugMenu { }

        // TODO: [Fred] This is where we can look for type added by an extension mecanism
        //    Here, only default types are added
        static UIPropertySetGenerator k_DebugMenuUIPropertyGenerator = UIPropertySetGenerator.Empty();

#if UNITY_EDITOR
        [MustUseReturnValue]
        static bool FindPropertyGeneratorsFor<TView>(
            [NotNullWhen(true)] out List<Type> generators,
            [NotNullWhen(false)] out Exception error
        )
        {
            var customGeneratorTypes = TypeCache.GetTypesWithAttribute<UIPropertyGeneratorSupportsAttribute>();

            // TODO: [Fred] Must be pooled
            generators = new();
            foreach (var customGeneratorType in customGeneratorTypes)
            {
                //sanity check
                if (!typeof(UIPropertyGenerator).IsAssignableFrom(customGeneratorType))
                {
                    Debug.LogError($"{nameof(UIPropertyGeneratorSupportsAttribute)} should only be used on class implementing {nameof(UIPropertyGenerator)}. Found on {customGeneratorType.FullName}. Skipping.");
                    continue;
                }

                var attributes = customGeneratorType.GetCustomAttributes(typeof(UIPropertyGeneratorSupportsAttribute), false);
                foreach (UIPropertyGeneratorSupportsAttribute attribute in attributes)
                {
                    if (attribute.uiType != typeof(TView))
                        continue;

                    generators.Add(customGeneratorType);
                }
            }

            error = default;
            return true;
        }

        [InitializeOnLoadMethod]
        static void FindCustomGenerator()
        {
            if (!FindPropertyGeneratorsFor<DebugMenu>(out var generators, out var error))
            {
                Debug.LogException(error);
                return;
            }

            if (UIPropertySetGenerator.TryFromGeneratorTypes(generators, out k_DebugMenuUIPropertyGenerator, out error))
            {
                Debug.LogException(error);
                return;
            }
        }
#endif

        public struct Parameters
        {
            public string uiViewTypeName;
            public string uiViewContextTypeName;

            public static Parameters Default() => new ()
            {
                uiViewTypeName = "DebugMenu",
                uiViewContextTypeName = "DebugMenuContext",
            };
        }

        [MustUseReturnValue]
        public static bool GenerateDebugMenuBindableView(
            [DisallowNull] UIDefinition uiDefinition,
            [DisallowNull] UIContextDefinition contextDefinition,
            Parameters parameters,
            [NotNullWhen(true)] out UIImplementationDocuments result,
            [NotNullWhen(false)] out Exception error
        )
        {
            result = default;

            if (!parameters.uiViewTypeName.FailIfNullOrEmpty(nameof(parameters.uiViewTypeName), out error)
                || !parameters.uiViewContextTypeName.FailIfNullOrEmpty(nameof(parameters.uiViewContextTypeName), out error))
                return false;

            // TODO multithreading:
            //   - Map
            //   - Property generation C# + UXML (multithreading inside)
            if (!UIDefinitionPropertyCategoryIndex.FromDefinition(uiDefinition, out var index, out error))
                return false;

            if (!GenerateBindableViewIntermediateDocumentFromProperties(
                    uiDefinition,
                    out var intermediateDocuments,
                    out error))
                return false;

            using (index)
                return GenerateDocumentFromIntermediate(parameters,
                    index,
                    intermediateDocuments,
                    contextDefinition,
                    out result,
                    out error);
        }

        [MustUseReturnValue]
        static bool GenerateBindableViewIntermediateDocumentFromProperties(
            [DisallowNull] UIDefinition definition,
            // TODO: [Fred] should be pooled
            out Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> result,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (!k_DebugMenuUIPropertyGenerator.GenerateIntermediateDocumentsFor(definition, out result, out error))
                return false;

            return true;
        }


        [MustUseReturnValue]
        static bool GenerateDocumentFromIntermediate(Parameters parameters,
            [DisallowNull] UIDefinitionPropertyCategoryIndex index,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [DisallowNull] UIContextDefinition uiContextDefinition,
            [NotNullWhen(true)] out UIImplementationDocuments result,
            [NotNullWhen(false)] out Exception error)
        {
            result = default;

            if (!GenerateRuntimeCode(parameters,
                    intermediateDocuments,
                    uiContextDefinition,
                    out var runtimeCode,
                    out error))
                return false;

            if (!GenerateVisualTreeAsset(parameters, index, intermediateDocuments, out var visualTreeAsset, out error))
                return false;

            if (!UIImplementationDocuments.From(Identifier, visualTreeAsset, runtimeCode, out result, out error))
                return false;

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateVisualTreeAsset(
            Parameters parameters,
            [DisallowNull] UIDefinitionPropertyCategoryIndex index,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [NotNullWhen(true)] out XmlDocument visualTreeAsset,
            [NotNullWhen(false)] out Exception error
        )
        {
            visualTreeAsset = new XmlDocument();
            error = default;
            return true;
        }

        [MustUseReturnValue]
        static bool GenerateRuntimeCode(Parameters parameters,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [DisallowNull] UIContextDefinition uiContextDefinition,
            [NotNullWhen(true)] out CSharpSyntaxTree runtimeCode,
            [NotNullWhen(false)] out Exception error)
        {
            error = default;
            runtimeCode = default;

            using (HashSetPool<string>.Get(out var usings))
            using (ListPool<MemberDeclarationSyntax>.Get(out var types))
            {
                if (!GenerateUIViewDeclaration(parameters, intermediateDocuments, usings, out var uiViewDeclaration, out error))
                    return false;

                if (!GenerateContextDeclarations(parameters,
                        intermediateDocuments,
                        uiContextDefinition,
                        usings,
                        out var interfaceDeclaration,
                        out var implementationDeclaration,
                        out error))
                    return false;

                types.Add(uiViewDeclaration);
                types.Add(interfaceDeclaration);
                types.Add(implementationDeclaration);

                var usingDirectives = SyntaxFactory.List<UsingDirectiveSyntax>(
                    usings.Select(u => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(u))));

                runtimeCode = (CSharpSyntaxTree) SyntaxFactory.SyntaxTree(
                    SyntaxFactory.CompilationUnit(
                        SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                        usingDirectives,
                        SyntaxFactory.List<AttributeListSyntax>(),
                        SyntaxFactory.List(types)
                    )
                );
            }

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateUIViewDeclaration(
            Parameters parameters,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [DisallowNull] HashSet<string> usings,
            [NotNullWhen(true)] out TypeDeclarationSyntax declaration,
            [NotNullWhen(false)] out Exception error
        )
        {
            error = default;

            var bindContextBodies = intermediateDocuments.Select(p => p.Value.bindContextBody)
                .Aggregate(new StringBuilder(), (acc, s) =>
                {
                    acc.Append(s);
                    return acc;
                }).ToString();
            var unbindContextBodies = intermediateDocuments.Select(p => p.Value.unbindContextBody)
                .Aggregate(new StringBuilder(), (acc, s) =>
                {
                    acc.Append(s);
                    return acc;
                }).ToString();

            usings.Add("System");
            usings.Add("System.Diagnostics.CodeAnalysis");
            usings.Add("UnityEngine.UIElements");
            usings.Add("UnityEngine.Rendering.UIGen");

            var code = $@"public sealed class {parameters.uiViewTypeName} : UIView<{parameters.uiViewTypeName}, I{parameters.uiViewContextTypeName}>
{{
    static DebugMenu()
    {{
        UIViewDefaults<{parameters.uiViewTypeName}>.DefaultTemplateAssetPath = ""Assets/DebugMenu.uxml"";
    }}

    protected override bool BindContext(
        [DisallowNull] TContext context,
        [DisallowNull] TemplateContainer container,
        [NotNullWhen(false)] out Exception error
    )
    {{
        {bindContextBodies}
    }}

    protected override bool UnbindContext(
        [DisallowNull] TContext context,
        [DisallowNull] TemplateContainer container,
        [NotNullWhen(false)] out Exception error
    )
    {{
        {unbindContextBodies}
    }}
}}";
            declaration = (TypeDeclarationSyntax) SyntaxFactory.ParseSyntaxTree(code).GetRoot();
            return true;
        }

        [MustUseReturnValue]
        static bool GenerateContextDeclarations(Parameters parameters,
            [DisallowNull] Dictionary<UIDefinition.Property, UIImplementationIntermediateDocuments> intermediateDocuments,
            [DisallowNull] UIContextDefinition uiContextDefinition,
            [DisallowNull] HashSet<string> usings,
            [NotNullWhen(true)] out TypeDeclarationSyntax interfaceDeclaration,
            [NotNullWhen(true)] out TypeDeclarationSyntax implementationDeclaration,
            [NotNullWhen(false)] out Exception error)
        {
            error = default;

            interfaceDeclaration = (TypeDeclarationSyntax) SyntaxFactory.ParseSyntaxTree($@"public interface I{parameters.uiViewContextTypeName}
{{
    {uiContextDefinition.members
        .Select(member => $"{member.type} {member.name} {{get;}}")
        .AggregateStrings("\r\n")}
}}
").GetRoot();

            implementationDeclaration = (TypeDeclarationSyntax) SyntaxFactory.ParseSyntaxTree($@"public class {parameters.uiViewContextTypeName} : I{parameters.uiViewContextTypeName}
{{
    {uiContextDefinition.members
        .Select(member => $"public {member.type} {member.name} => {member.type}.instance;")
        .AggregateStrings("\r\n")}
}}
").GetRoot();

            return true;
        }
    }
}
